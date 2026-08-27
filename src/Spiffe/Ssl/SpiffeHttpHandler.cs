using System.Net.Http;
using Spiffe.Svid.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Ssl;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that presents the current X.509 SVID for mTLS
/// and automatically rotates the client certificate when the <see cref="X509Source"/> is updated.
/// </summary>
/// <remarks>
/// Unlike setting <see cref="System.Net.Security.SslClientAuthenticationOptions.ClientCertificateContext"/>
/// directly on a <see cref="SocketsHttpHandler"/> (which is captured once and never refreshed),
/// this handler swaps its inner <see cref="SocketsHttpHandler"/> each time the source reports
/// a new SVID, ensuring that all subsequent connections use the rotated certificate chain
/// including any intermediate CAs. Existing requests drain until their SVID expires or the
/// configured maximum drain time elapses.
/// </remarks>
public sealed class SpiffeHttpHandler : HttpMessageHandler
{
    private readonly X509Source _source;

    private readonly IAuthorizer _authorizer;

    private readonly TimeSpan _minDrain;

    private readonly TimeSpan _maxDrain;

    private readonly TimeSpan _connectionLifetime;

    private readonly object _lock = new();

    private RetiringInvoker _inner;

    private string? _thumbprint;

    private volatile bool _disposed;

    /// <summary>
    /// Creates a new <see cref="SpiffeHttpHandler"/> backed by the given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The X.509 source whose current SVID is used for mTLS.</param>
    /// <param name="authorizer">Authorizer used to validate the server's SPIFFE ID.</param>
    /// <param name="minDrain">
    /// The minimum time an in-flight request is allowed to drain when its SVID has already
    /// expired. Defaults to 5 seconds.
    /// </param>
    /// <param name="maxDrain">
    /// The maximum time an in-flight request can retain a previous handler after rotation.
    /// Defaults to 1 hour. Requests otherwise drain until the previous SVID expires.
    /// </param>
    /// <param name="connectionLifetime">
    /// The maximum lifetime of pooled connections. Defaults to 5 minutes; use
    /// <see cref="Timeout.InfiniteTimeSpan"/> to disable it. This causes new connections to
    /// re-authenticate with the current SVID if update notifications fail, but cannot rotate a
    /// single long-lived stream because it remains pinned to its connection.
    /// </param>
    public SpiffeHttpHandler(
        X509Source source,
        IAuthorizer authorizer,
        TimeSpan? minDrain = null,
        TimeSpan? maxDrain = null,
        TimeSpan? connectionLifetime = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        _minDrain = minDrain ?? TimeSpan.FromSeconds(5);
        _maxDrain = maxDrain ?? TimeSpan.FromHours(1);
        _connectionLifetime = connectionLifetime ?? TimeSpan.FromMinutes(5);
        if (_minDrain < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minDrain), "minDrain must be non-negative.");
        }

        if (_maxDrain < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDrain), "maxDrain must be non-negative.");
        }

        if (_minDrain > _maxDrain)
        {
            throw new ArgumentOutOfRangeException(nameof(minDrain), "minDrain must not exceed maxDrain.");
        }

        if (_connectionLifetime < TimeSpan.Zero && _connectionLifetime != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionLifetime), "connectionLifetime must be non-negative or infinite.");
        }

        X509Svid leaf = TryGetLeaf() ?? throw new InvalidOperationException("The X.509 source does not have a leaf certificate.");
        _inner = CreateInvoker(leaf);
        _thumbprint = leaf.Certificates[0].Thumbprint;
        _source.Updated += Refresh;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RetiringInvoker inner;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            inner = _inner;
        }

        return inner.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _source.Updated -= Refresh;
                _inner.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    internal RetiringInvoker CurrentInvoker
    {
        get
        {
            lock (_lock)
            {
                return _inner;
            }
        }
    }

    private RetiringInvoker CreateInvoker(X509Svid leaf)
    {
        SocketsHttpHandler handler = new()
        {
            SslOptions = SpiffeSslConfig.GetMtlsClientOptions(_source, _authorizer),
            PooledConnectionLifetime = _connectionLifetime,
        };
        return new RetiringInvoker(
            new HttpMessageInvoker(handler),
            leaf.Certificates[0].NotAfter.ToUniversalTime());
    }

    private void Refresh()
    {
        X509Svid? leaf = TryGetLeaf();
        if (leaf is null)
        {
            return;
        }

        string thumbprint = leaf.Certificates[0].Thumbprint;
        lock (_lock)
        {
            if (_disposed || string.Equals(thumbprint, _thumbprint, StringComparison.Ordinal))
            {
                return;
            }
        }

        RetiringInvoker newInvoker;
        try
        {
            newInvoker = CreateInvoker(leaf);
        }
        catch (Exception exception)
        {
            // Keep serving the previous credential if constructing its replacement fails.
            System.Diagnostics.Debug.WriteLine(exception);
            return;
        }

        RetiringInvoker old;
        lock (_lock)
        {
            if (_disposed || string.Equals(thumbprint, _thumbprint, StringComparison.Ordinal))
            {
                newInvoker.Dispose();
                return;
            }

            old = _inner;
            _inner = newInvoker;
            _thumbprint = thumbprint;
        }

        old.Retire(GetRetirementDeadline(old.CredentialExpiry), _minDrain);
    }

    private X509Svid? TryGetLeaf()
    {
        try
        {
            return _source.GetX509Svid();
        }
        catch (Exception exception)
        {
            // The source may have been disposed concurrently with an update notification.
            System.Diagnostics.Debug.WriteLine(exception);
            return null;
        }
    }

    private DateTimeOffset GetRetirementDeadline(DateTimeOffset credentialExpiry)
    {
        DateTimeOffset maxDeadline;
        try
        {
            maxDeadline = DateTimeOffset.UtcNow + _maxDrain;
        }
        catch (ArgumentOutOfRangeException)
        {
            maxDeadline = DateTimeOffset.MaxValue;
        }

        return credentialExpiry < maxDeadline ? credentialExpiry : maxDeadline;
    }
}
