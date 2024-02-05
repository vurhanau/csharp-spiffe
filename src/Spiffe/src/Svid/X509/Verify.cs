using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Svid.X509;

/// <summary>
/// X509 SVID verificatiion utility.
/// </summary>
public static class Verify
{
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

        string str = san.First();
        if (!str.StartsWith("URI:"))
        {
            throw new ArgumentException("Certificate SAN format is not supported");
        }

        str = str.Substring("URI:".Length);

        return SpiffeId.FromString(str);
    }
}
