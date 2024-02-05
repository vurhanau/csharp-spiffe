using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Ssl;

/// <summary>
/// TLS configuration which verifies and authorizes
/// the peer's X509-SVID.
/// </summary>
public static class SpiffeSslConfig
{
    /// <summary>
    /// Creates MTLS server authentication config backed by X509 SVID.
    /// </summary>
    public static SslServerAuthenticationOptions GetMtlsServerOptions(X509Source x509Source)
    {
        return new SslServerAuthenticationOptions
        {
            ClientCertificateRequired = true,
            RemoteCertificateValidationCallback = (_, cert, chain, _) => ValidateRemoteCertificate(cert, chain, x509Source),
            ServerCertificateContext = CreateContext(x509Source),
        };
    }

    /// <summary>
    /// Creates MTLS client authentication config backed by X509 SVID.
    /// </summary>
    public static SslClientAuthenticationOptions GetMtlsClientOptions(X509Source x509Source)
    {
        return new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, cert, chain, _) => ValidateRemoteCertificate(cert, chain, x509Source),
            ClientCertificateContext = CreateContext(x509Source),
        };
    }

    private static bool ValidateRemoteCertificate(X509Certificate? cert, X509Chain? chain, X509Source x509Source)
    {
        if (cert == null || chain == null)
        {
            return false;
        }

        X509Certificate2 leaf = new(cert);
        X509Certificate2Collection intermediates = chain.ChainPolicy.ExtraStore;

        return X509Verify.Verify(leaf, intermediates, x509Source);
    }

    private static SslStreamCertificateContext CreateContext(X509Source x509Source)
    {
        X509Svid svid = x509Source.GetX509Svid();
        X509Certificate2 leaf = svid.Certificates[0];
        X509Certificate2Collection intermediates = [..svid.Certificates.Skip(1)];

        return SslStreamCertificateContext.Create(leaf, intermediates, true);
    }
}
