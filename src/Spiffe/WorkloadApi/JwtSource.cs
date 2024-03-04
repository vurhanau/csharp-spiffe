using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Svid.Jwt;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public sealed class JwtSource : IJwtSource
{
    private readonly ReaderWriterLockSlim _lock;

    private readonly IWorkloadApiClient _client;

    private JwtBundleSet? _bundles;

    private volatile int _initialized;

    private volatile int _disposed;

    internal JwtSource(IWorkloadApiClient client)
    {
        _lock = new ReaderWriterLockSlim();
        _client = client;
    }

    /// <summary>
    /// Indicates if source is initialized.
    /// </summary>
    public bool IsInitialized => _initialized == 1;

    private bool IsDisposed => _disposed != 0;

    /// <inheritdoc/>
    public async Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default)
    {
        return await _client.FetchJwtSvidsAsync(jwtParams, cancellationToken);
    }

    /// <summary>
    /// Returns the JWT bundle for the given trust domain.
    /// </summary>
    public JwtBundle GetJwtBundle(TrustDomain trustDomain)
    {
        ThrowIfNotInitalized();
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return _bundles!.GetJwtBundle(trustDomain);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Disposes source
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _lock.Dispose();
        }
    }

    /// <summary>
    /// Creates a new <see cref="JwtSource"/>. It awaits until the initial update
    /// has been received from the Workload API for <paramref name="timeoutMillis"/>.
    /// The source should be closed when no longer in use to free underlying resources.
    /// </summary>
    public static async Task<JwtSource> CreateAsync(IWorkloadApiClient client,
                                                    int timeoutMillis = 60_000,
                                                    CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));

        JwtSource source = new(client);
        Watcher<JwtBundleSet> watcher = new(source.SetJwtBundleSet);
        _ = Task.Run(
            () => client.WatchJwtBundlesAsync(watcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(timeoutMillis, cancellationToken);

        return source;
    }

    /// <summary>
    /// Visible for testing.
    /// </summary>
    internal void SetJwtBundleSet(JwtBundleSet jwtBundleSet)
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            _bundles = jwtBundleSet;
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
            throw new InvalidOperationException("JWT source is not initialized");
        }
    }
}
