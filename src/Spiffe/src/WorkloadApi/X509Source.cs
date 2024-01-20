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
public sealed class X509Source : IDisposable
{
    private readonly Func<List<X509Svid>, X509Svid> _picker;

    private readonly ReaderWriterLockSlim _lock;

    private X509Svid? _svid;

    private X509BundleSet? _bundles;

    private volatile int _initialized;

    private volatile int _disposed;

    /// <summary>
    /// Constructs X509 source.
    /// </summary>
    internal X509Source(Func<List<X509Svid>, X509Svid> picker)
    {
        _picker = picker;
        _lock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Creates a new X509Source. It blocks until the initial update
    /// has been received from the Workload API. The source should be closed when
    /// no longer in use to free underlying resources.
    /// </summary>
    public static async Task<X509Source> New(IWorkloadApiClient client,
                                             Func<List<X509Svid>, X509Svid>? picker = null,
                                             CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        picker ??= svids =>
        {
            if (svids.Count == 0)
            {
                throw new ArgumentException("SVIDs must be non-empty");
            }

            return svids[0];
        };

        X509Source source = new(picker);
        X509ContextWatcher watcher = new(source.SetX509Context);
        _ = Task.Run(
            () => client.WatchX509ContextAsync(watcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(cancellationToken);

        return source;
    }

    /// <summary>
    /// Gets a default SVID.
    /// </summary>
    public X509Svid GetX509Svid()
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            // This is a defensive check and should be unreachable since the source
            // waits for the initial Workload API update before returning from
            // New().
            if (_svid == null)
            {
                throw new InvalidOperationException("Missing X509-SVID");
            }

            return _svid!;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets a trust bundle associated with trust domain.
    /// </summary>
    public X509Bundle GetX509Bundle(TrustDomain trustDomain)
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            bool found = _bundles!.Bundles.TryGetValue(trustDomain, out X509Bundle? bundle);
            if (!found || bundle == null)
            {
                throw new KeyNotFoundException($"No X.509 bundle for trust domain '{trustDomain}'");
            }

            return bundle!;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Disposes client
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _lock.Dispose();
        }
    }

    /// <summary>
    /// Waits until the source is updated or the <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    private async Task WaitUntilUpdated(CancellationToken cancellationToken = default)
    {
        while (_initialized == 0 &&
               _disposed != 1 &&
               !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(500, cancellationToken);
        }
    }

    private void SetX509Context(X509Context x509Context)
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            _svid = _picker(x509Context.X509Svids);
            _bundles = x509Context.X509Bundles;
            _initialized = 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }
}
