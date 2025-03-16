using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Ssl;

/// <summary>
///     TLS configuration which verifies and authorizes
///     the peer's X509-SVID.
/// </summary>
public static class SpiffeSslConfig
{
    /// <summary>
    ///     Creates TLS server authentication config backed by X509 SVID.
    /// </summary>
    public static SslServerAuthenticationOptions GetTlsServerOptions(IX509Source x509Source) =>
        new() { ClientCertificateRequired = false, ServerCertificateContext = CreateContext(x509Source) };

    /// <summary>
    ///     Creates MTLS server authentication config backed by X509 SVID.
    /// </summary>
    public static SslServerAuthenticationOptions GetMtlsServerOptions(IX509Source x509Source, IAuthorizer authorizer) =>
        new()
        {
            ClientCertificateRequired = true,
            RemoteCertificateValidationCallback = (_, cert, chain, _) =>
                ValidateRemoteCertificate(cert, chain, x509Source, authorizer),
            ServerCertificateContext = CreateContext(x509Source)
        };

    /// <summary>
    ///     Creates TLS client authentication config backed by X509 SVID.
    /// </summary>
    public static SslClientAuthenticationOptions GetTlsClientOptions(IX509BundleSource x509BundleSource) =>
        new()
        {
            RemoteCertificateValidationCallback = (_, cert, chain, _) =>
                ValidateRemoteCertificate(cert, chain, x509BundleSource, Authorizers.AuthorizeAny())
        };

    /// <summary>
    ///     Creates MTLS client authentication config backed by X509 SVID.
    /// </summary>
    public static SslClientAuthenticationOptions GetMtlsClientOptions(IX509Source x509Source, IAuthorizer authorizer) =>
        new()
        {
            RemoteCertificateValidationCallback = (_, cert, chain, _) =>
                ValidateRemoteCertificate(cert, chain, x509Source, authorizer),
            ClientCertificateContext = CreateContext(x509Source)
        };

    private static bool ValidateRemoteCertificate(
        X509Certificate? cert,
        X509Chain? chain,
        IX509BundleSource x509BundleSource,
        IAuthorizer authorizer)
    {
        if (cert == null || chain == null)
        {
            return false;
        }

        X509Certificate2 leaf = new(cert);
        X509Certificate2Collection intermediates = chain.ChainPolicy.ExtraStore;

        bool ok = X509Verify.Verify(leaf, intermediates, x509BundleSource);
        if (!ok)
        {
            return false;
        }

        SpiffeId id = X509Verify.GetSpiffeIdFromCertificate(leaf);
        ok = authorizer.Authorize(id);

        return ok;
    }

    private static SslStreamCertificateContext CreateContext(IX509Source x509Source)
    {
        X509Svid svid = x509Source.GetX509Svid();
        X509Certificate2Collection c = svid.Certificates;
        if (!c.Any())
        {
            throw new ArgumentException("SVID doesn't contain any certificates");
        }

        X509Certificate2 leaf = c[0];
        X509Certificate2Collection intermediates = c.Count > 1 ? [..c.Skip(1)] : [];

        return SslStreamCertificateContext.Create(leaf, intermediates, true);
    }
}
