using DotNext.Threading;
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
    private IX509ContextWatcher _watcher;

    private readonly Func<List<X509Svid>, X509Svid?> _picker;

    private readonly AsyncReaderWriterLock _lock;

    private readonly TimeSpan _lockTimeout;

    private X509Svid? _svid;

    private X509BundleSet? _bundles;

    /// <summary>
    /// Constructs X509 source.
    /// </summary>
    internal X509Source(Func<List<X509Svid>, X509Svid?> picker)
    {
        _watcher = new Watcher();
        _picker = picker;
        _lock = new AsyncReaderWriterLock();
        _lockTimeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Creates a new X509Source. It blocks until the initial update
    /// has been received from the Workload API. The source should be closed when
    /// no longer in use to free underlying resources.
    /// </summary>
    public static async Task<X509Source> New(IWorkloadApiClient client,
                                             Func<List<X509Svid>, X509Svid?>? picker = null,
                                             CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        picker ??= svids => svids.FirstOrDefault();

        X509Source source = new(picker);
        source._watcher = new Watcher();
        return source;
    }

    /// <inheritdoc/>
    public async Task<X509Svid?> GetX509Svid()
    {
        // check if closed
        using (await _lock.AcquireReadLockAsync(_lockTimeout))
        {
            return _svid;
        }
    }

    /// <inheritdoc/>
    public async Task<X509Bundle?> GetX509Bundle(TrustDomain trustDomain)
    {
        // check if closed
        using (await _lock.AcquireReadLockAsync(_lockTimeout))
        {
            if (_bundles == null)
            {
                return null;
            }

            bool found = _bundles.Bundles.TryGetValue(trustDomain, out X509Bundle? bundle);
            return found ? bundle : null;
        }
    }

    /// <summary>
    /// Disposes client
    /// </summary>
    public async ValueTask DisposeAsync() => await _lock.DisposeAsync();

    private async Task UpdateX509Context(X509Context x509Context)
    {
        // check if closed
        using (await _lock.AcquireWriteLockAsync(_lockTimeout))
        {
            _svid = _picker(x509Context.X509Svids);
            _bundles = x509Context.X509Bundles;
        }
    }
}
