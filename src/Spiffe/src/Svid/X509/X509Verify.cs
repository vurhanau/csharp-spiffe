using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Bundle.X509;
using Spiffe.Id;

namespace Spiffe.Svid.X509;

/// <summary>
/// X509 SVID verificatiion utility.
/// </summary>
public static class X509Verify
{
    /// <summary>
    /// Verifies an X509-SVID chain using the X.509 bundle source.
    /// It returns the SPIFFE ID of the X509-SVID and one or more chains back to a root
    /// in the bundle.
    /// </summary>
    public static bool Verify(X509Certificate2 leaf,
                              X509Certificate2Collection intermediates,
                              IX509BundleSource bundleSource)
    {
        _ = leaf ?? throw new ArgumentNullException(nameof(leaf));
        _ = intermediates ?? throw new ArgumentNullException(nameof(intermediates));
        _ = bundleSource ?? throw new ArgumentNullException(nameof(bundleSource));

        SpiffeId id = GetSpiffeIdFromCertificate(leaf);

        if (IsCA(leaf))
        {
            throw new ArgumentException("Leaf certificate with CA flag set to true");
        }

        if (HasKeyUsageFlag(leaf, X509KeyUsageFlags.KeyCertSign))
        {
            throw new ArgumentException("Leaf certificate with KeyCertSign key usage");
        }

        if (HasKeyUsageFlag(leaf, X509KeyUsageFlags.CrlSign))
        {
            throw new ArgumentException("Leaf certificate with KeyCrlSign key usage");
        }

        // TODO: add ExtKeyUsageAny validation

        X509Bundle bundle = bundleSource.GetX509Bundle(id.TrustDomain);

        X509Chain chain = new();
        X509ChainPolicy p = chain.ChainPolicy;
        p.TrustMode = X509ChainTrustMode.CustomRootTrust;
        p.RevocationMode = X509RevocationMode.NoCheck;
        p.CustomTrustStore.AddRange(bundle.X509Authorities);
        p.ExtraStore.AddRange(intermediates);

        return chain.Build(leaf);
    }

    /// <summary>
    /// Extracts the SPIFFE ID from the URI SAN of the provided
    /// certificate. It will return an an error if the certificate does not have
    /// exactly one URI SAN with a well-formed SPIFFE ID.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="certificate"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="certificate"/> doesn't have exactly 1 SAN.</exception>
    public static SpiffeId GetSpiffeIdFromCertificate(X509Certificate2 certificate)
    {
        _ = certificate ?? throw new ArgumentNullException(nameof(certificate));

        IEnumerable<string> san = certificate.Extensions.Cast<X509Extension>()
                                             .Where(ext => ext.Oid?.Value == "2.5.29.17") // n.Oid.FriendlyName == "Subject Alternative Name"
                                             .Select(ext => new AsnEncodedData(ext.Oid, ext.RawData))
                                             .Select(ext => ext.Format(true));
        if (!san.Any())
        {
            throw new ArgumentException("Certificate doesn't contain URI SAN");
        }

        if (san.Count() > 1)
        {
            throw new ArgumentException("Certificate contains more than one URI SAN");
        }

        string str = san.First().Trim();

        // Windows: "URL=spiffe://example.org/workload"
        // Unix, MacOS (OpenSSL): "URI:spiffe://example.org/workload".
        int uriOffset = str.IndexOf(SpiffeId.SchemePrefix);
        if (string.IsNullOrEmpty(str) || uriOffset < 0)
        {
            throw new ArgumentException($"Certificate SAN does not contain Spiffe ID: {str}");
        }

        str = str.Substring(uriOffset);

        return SpiffeId.FromString(str);
    }

    private static bool HasKeyUsageFlag(X509Certificate2 cert, X509KeyUsageFlags flag)
    {
        return cert.Extensions.OfType<X509KeyUsageExtension>().Any(ku => (ku.KeyUsages & flag) == flag);
    }

    private static bool IsCA(X509Certificate2 cert)
    {
        return cert.Extensions.OfType<X509BasicConstraintsExtension>().Any(c => c.CertificateAuthority);
    }
}
