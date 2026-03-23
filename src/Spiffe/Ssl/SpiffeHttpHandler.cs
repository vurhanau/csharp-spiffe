using System.Net.Http;
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
/// including any intermediate CAs.
/// </remarks>
public sealed class SpiffeHttpHandler : HttpMessageHandler
{
    private readonly X509Source _source;

    private readonly IAuthorizer _authorizer;

    private readonly TimeSpan _drainDelay;

    private volatile HttpMessageInvoker _inner;

    private volatile bool _disposed;

    /// <summary>
    /// Creates a new <see cref="SpiffeHttpHandler"/> backed by the given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The X.509 source whose current SVID is used for mTLS.</param>
    /// <param name="authorizer">Authorizer used to validate the server's SPIFFE ID.</param>
    /// <param name="drainDelay">
    /// How long to wait before disposing the previous inner handler after a certificate rotation,
    /// to allow in-flight requests to complete. Defaults to 30 seconds.
    /// </param>
    public SpiffeHttpHandler(X509Source source, IAuthorizer authorizer, TimeSpan? drainDelay = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        _drainDelay = drainDelay ?? TimeSpan.FromSeconds(30);
        _inner = CreateInvoker();
        _source.Updated += Refresh;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.SendAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _source.Updated -= Refresh;
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private HttpMessageInvoker CreateInvoker() => new(new SocketsHttpHandler
    {
        SslOptions = SpiffeSslConfig.GetMtlsClientOptions(_source, _authorizer),
    });

    private void Refresh()
    {
        if (_disposed)
        {
            return;
        }

        HttpMessageInvoker old = Interlocked.Exchange(ref _inner, CreateInvoker());
        _ = Task.Delay(_drainDelay).ContinueWith(_ => old.Dispose(), TaskScheduler.Default);
    }
}
