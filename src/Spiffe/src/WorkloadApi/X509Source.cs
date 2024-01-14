using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a source of X.509 SVIDs and X.509 bundles maintained via the Workload API.
/// <br/>
/// It handles a <see cref="X509Svid"/> and a <see cref="X509BundleSet"/> that are updated automatically
/// whenever there is an update from the Workload API.
/// <br/>
/// Implements the <see cref="IDisposable"/> interface to close the source,
/// dropping the connection to the Workload API. Other source methods will return an error
/// after close has been called.
/// </summary>
public sealed class X509Source : IX509Source
{
    private readonly Func<List<X509Svid>, X509Svid?> _picker;

    private readonly ReaderWriterLock _lock;

    private readonly TimeSpan _lockTimeout;

    private X509Svid? _svid;

    private X509BundleSet? _bundles;

    /// <summary>
    /// Constructs X509 source.
    /// </summary>
    internal X509Source(Func<List<X509Svid>, X509Svid?> picker)
    {
        _picker = picker;
        _lock = new ReaderWriterLock();
        _lockTimeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Blocks to get an initial X509 context and then listens to updates.
    /// </summary>
    public static Task StartAsync(IWorkloadApiClient client,
                                  Func<List<X509Svid>, X509Svid?>? picker = null,
                                  CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        picker ??= svids => svids.FirstOrDefault();

        CountdownEvent countdown = new(1);
        X509Source source = new(picker);
        Task watchTask = Task.Run(
            async () =>
            {
                bool stop = false;
                while (!cancellationToken.IsCancellationRequested && !stop)
                {
                    try
                    {
                        await client.WatchX509ContextAsync(
                            (x509Context, _) =>
                            {
                                source.UpdateX509Context(x509Context);
                                countdown.Signal();
                            },
                            cancellationToken);
                    }
                    catch
                    {
                        stop = true;
                    }
                }
            },
            cancellationToken);

        countdown.Wait(cancellationToken);

        return watchTask;
    }

    /// <inheritdoc/>
    public X509Svid? GetX509Svid()
    {
        // check if closed
        _lock.AcquireReaderLock(_lockTimeout);
        try
        {
            return _svid;
        }
        finally
        {
            _lock.ReleaseReaderLock();
        }
    }

    /// <inheritdoc/>
    public X509Bundle? GetX509Bundle(TrustDomain trustDomain)
    {
        // check if closed
        _lock.AcquireReaderLock(_lockTimeout);
        try
        {
            if (_bundles == null)
            {
                return null;
            }

            bool found = _bundles.Bundles.TryGetValue(trustDomain, out X509Bundle? bundle);
            return found ? bundle : null;
        }
        finally
        {
            _lock.ReleaseReaderLock();
        }
    }

    private void UpdateX509Context(X509Context x509Context)
    {
        // check if closed
        _lock.AcquireWriterLock(_lockTimeout);
        try
        {
            _svid = _picker(x509Context.X509Svids);
            _bundles = x509Context.X509Bundles;
        }
        finally
        {
            _lock.ReleaseWriterLock();
        }
    }
}
