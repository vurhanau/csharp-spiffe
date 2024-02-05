using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Svid.X509;

/// <summary>
/// Represents a SPIFFE X.509 SVID.
/// <br/>
/// Contains a SPIFFE ID, a private key and a chain of X.509 certificates.
/// </summary>
public class X509Svid
{
    /// <summary>
    /// Creates X509 SVID.
    /// </summary>
    public X509Svid(SpiffeId id,
                    X509Certificate2Collection certificates,
                    string hint)
    {
        if (!certificates.Any())
        {
            throw new ArgumentException("Certificates collection must be non-empty");
        }

        if (!certificates[0].HasPrivateKey)
        {
            throw new ArgumentException("Leaf certificate must have a private key");
        }

        Id = id;
        Certificates = certificates;
        Hint = hint;
    }

    /// <summary>
    /// Gets SVID SPIFFE id.
    /// </summary>
    public SpiffeId Id { get; }

    /// <summary>
    /// X.509 certificates of the X509-SVID.
    /// The leaf certificate is the X509-SVID certificate with a private key.
    /// Any remaining certificates (if any) chain the X509-SVID certificate back to
    /// a X.509 root for the trust domain.
    /// </summary>
    public X509Certificate2Collection Certificates { get; }

    /// <summary>
    /// Gets an operator-specified string used to provide guidance on how this
    /// identity should be used by a workload when more than one SVID is returned.
    /// </summary>
    public string Hint { get; }
}
