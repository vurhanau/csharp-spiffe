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

    private readonly IWorkloadApiClient _workloadApiClient;

    private readonly ReaderWriterLock _lock;

    private readonly TimeSpan _lockTimeout;

    private X509Svid? _svid;

    private X509BundleSet? _bundles;

    /// <summary>
    /// Constructs X509 source.
    /// </summary>
    internal X509Source(IWorkloadApiClient workloadApiClient, Func<List<X509Svid>, X509Svid?>? picker = default)
    {
        _workloadApiClient = workloadApiClient;
        _picker = picker ?? PickDefaultSvid;
        _lock = new ReaderWriterLock();
        _lockTimeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Subscribes to updates and gets an initial X509 context.
    /// </summary>
    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        // check if closed
        var watcherTask = Task.Run(
            () => _workloadApiClient.WatchX509ContextAsync(UpdateX509Context, cancellationToken),
            cancellationToken);

        X509Context x509Context = await _workloadApiClient.FetchX509ContextAsync(cancellationToken);
        UpdateX509Context(x509Context, cancellationToken);

        await watcherTask;
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

    private void UpdateX509Context(X509Context x509Context, CancellationToken cancellationToken)
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

    private X509Svid? PickDefaultSvid(List<X509Svid> svids) => svids.FirstOrDefault();
}
