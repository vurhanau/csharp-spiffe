using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Util;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Source of SPIFFE bundles maintained via the Workload API
/// </summary>
public sealed class BundleSource : IX509BundleSource, IJwtBundleSource, IDisposable
{
    private readonly ReaderWriterLockSlim _lock;

    private readonly TaskCompletionSource<bool> _initializedX509;

    private readonly TaskCompletionSource<bool> _initializedJwt;

    private X509BundleSet? _x509Bundles;

    private JwtBundleSet? _jwtBundles;

    private volatile int _disposed;

    /// <summary>
    /// Constructs bundle source.
    /// Visible for testing.
    /// </summary>
    internal BundleSource()
    {
        _lock = new ReaderWriterLockSlim();
        _initializedX509 = new TaskCompletionSource<bool>();
        _initializedJwt = new TaskCompletionSource<bool>();
    }

    /// <summary>
    /// Indicates if source is initialized.
    /// </summary>
    public bool IsInitialized =>
        _initializedX509.Task.IsCompletedSuccessfully && _initializedJwt.Task.IsCompletedSuccessfully;

    private bool IsDisposed => _disposed != 0;

    /// <summary>
    /// Returns the X.509 bundle for the given trust domain.
    /// </summary>
    public X509Bundle GetX509Bundle(TrustDomain trustDomain)
    {
        Throws.IfNotInitialized(nameof(BundleSource), IsInitialized);
        Throws.IfDisposed(nameof(BundleSource), IsDisposed);

        _lock.EnterReadLock();
        try
        {
            return _x509Bundles!.GetX509Bundle(trustDomain);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns the JWT bundle for the given trust domain.
    /// </summary>
    public JwtBundle GetJwtBundle(TrustDomain trustDomain)
    {
        Throws.IfNotInitialized(nameof(BundleSource), IsInitialized);
        Throws.IfDisposed(nameof(BundleSource), IsDisposed);

        _lock.EnterReadLock();
        try
        {
            return _jwtBundles!.GetJwtBundle(trustDomain);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Creates a new <see cref="BundleSource"/>.
    /// It blocks until the initial update has been received from the Workload API.
    /// The source should be closed when no longer in use to free underlying resources.
    /// </summary>
    public static async Task<BundleSource> CreateAsync(IWorkloadApiClient client,
                                                       int timeoutMillis = 60_000,
                                                       CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));

        BundleSource source = new();
        Watcher<X509Context> x509Watcher = new(source.SetX509Context);
        _ = Task.Run(
            () => client.WatchX509ContextAsync(x509Watcher, cancellationToken),
            cancellationToken);

        Watcher<JwtBundleSet> jwtWatcher = new(source.SetJwtBundles);
        _ = Task.Run(
            () => client.WatchJwtBundlesAsync(jwtWatcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(timeoutMillis, cancellationToken)
                    .ConfigureAwait(false);

        return source;
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
    /// Visible for testing.
    /// </summary>
    internal void SetX509Context(X509Context x509Context)
    {
        Throws.IfDisposed(nameof(BundleSource), IsDisposed);

        _lock.EnterWriteLock();
        try
        {
            _x509Bundles = x509Context.X509Bundles;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _initializedX509.SetResult(true);
    }

    /// <summary>
    /// Visible for testing.
    /// </summary>
    internal void SetJwtBundles(JwtBundleSet jwtBundles)
    {
        Throws.IfDisposed(nameof(BundleSource), IsDisposed);

        _lock.EnterWriteLock();
        try
        {
            _jwtBundles = jwtBundles;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        _initializedJwt.TrySetResult(true);
    }

    /// <summary>
    /// Waits until the source is updated or the <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    private async Task WaitUntilUpdated(int timeoutMillis, CancellationToken cancellationToken = default)
    {
        await Wait.Until(
            "Bundle source",
            [_initializedX509.Task, _initializedJwt.Task],
            () =>
            {
                _initializedX509.TrySetResult(false);
                _initializedJwt.TrySetResult(false);
            },
            () => IsInitialized,
            () => IsDisposed,
            timeoutMillis,
            cancellationToken);
    }
}
