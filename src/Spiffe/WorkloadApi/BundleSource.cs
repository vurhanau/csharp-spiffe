using Spiffe.Bundle;
using Spiffe.Bundle.X509;
using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Source of SPIFFE bundles maintained via the Workload API
/// </summary>
public sealed class BundleSource : IX509BundleSource, IDisposable
{
    private readonly ReaderWriterLockSlim _lock;

    private X509BundleSet? _x509Bundles;

    private volatile int _initialized;

    private volatile int _disposed;

    /// <summary>
    /// Constructs bundle source.
    /// Visible for testing.
    /// </summary>
    internal BundleSource()
    {
        _lock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Indicates if source is initialized.
    /// </summary>
    public bool IsInitialized => _initialized == 1;

    private bool IsDisposed => _disposed != 0;

    /// <summary>
    /// Returns the SPIFFE bundle for the given trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Thrown if bundle not found.</exception>
    public SpiffeBundle GetBundle(TrustDomain trustDomain)
    {
        ThrowIfNotInitalized();
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            bool hasX509Authorities = _x509Bundles!.Bundles.ContainsKey(trustDomain);
            if (!hasX509Authorities)
            {
                throw new BundleNotFoundException($"No SPIFFE bundle for trust domain '{trustDomain.Name}'");
            }

            X509Bundle x509Authorities = _x509Bundles.Bundles[trustDomain];
            return new SpiffeBundle
            {
                TrustDomain = trustDomain,
                X509Authorities = x509Authorities,
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns the X.509 bundle for the given trust domain.
    /// </summary>
    public X509Bundle GetX509Bundle(TrustDomain trustDomain)
    {
        ThrowIfNotInitalized();
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return _x509Bundles!.GetBundleForTrustDomain(trustDomain);
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
        Watcher<X509Context> watcher = new(source.SetX509Context);
        _ = Task.Run(
            () => client.WatchX509ContextAsync(watcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(timeoutMillis, cancellationToken);

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
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            _x509Bundles = x509Context.X509Bundles;
            _initialized = 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Waits until the source is updated or the <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    private async Task WaitUntilUpdated(int timeoutMillis, CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeout = new();
        timeout.CancelAfter(timeoutMillis);

        while (!IsInitialized &&
               !IsDisposed &&
               !timeout.IsCancellationRequested &&
               !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(50, CancellationToken.None);
        }

        if (!IsInitialized)
        {
            timeout.Token.ThrowIfCancellationRequested();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    private void ThrowIfNotInitalized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Bundle source is not initialized");
        }
    }
}
