using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Svid.X509;

/// <summary>
/// Represents a SPIFFE X.509 SVID.
/// <br/>
/// Contains a SPIFFE ID, a private key and a chain of X.509 certificates.
/// </summary>
public class X509Svid(SpiffeId spiffeId,
                      X509Certificate2 certificate,
                      X509Chain chain,
                      string hint)
{
    /// <summary>
    /// Gets SVID SPIFFE id
    /// </summary>
    public SpiffeId SpiffeId { get; } = spiffeId;

    /// <summary>
    /// Gets the X.509-SVID certificate chain back to an X.509 root for the trust domain.
    /// </summary>
    public X509Chain Chain { get; } = chain;

    /// <summary>
    /// Gets the X.509 certificate of the X.509-SVID.
    /// </summary>
    public X509Certificate2 Certificate { get; } = certificate;

    /// <summary>
    /// Gets an operator-specified string used to provide guidance on how this
    /// identity should be used by a workload when more than one SVID is returned.
    /// </summary>
    public string Hint { get; } = hint;
}
