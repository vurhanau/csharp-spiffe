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
    /// Gets SVID SPIFFE id
    /// </summary>
    public required SpiffeId SpiffeId { get; init; }

    /// <summary>
    /// Gets the X.509-SVID certificate chain back to an X.509 root for the trust domain.
    /// </summary>
    public required X509Chain Chain { get; init; }

    /// <summary>
    /// Gets the X.509 certificate of the X.509-SVID.
    /// </summary>
    public required X509Certificate2 Certificate { get; init; }

    /// <summary>
    /// Gets an operator-specified string used to provide guidance on how this
    /// identity should be used by a workload when more than one SVID is returned.
    /// </summary>
    public required string Hint { get; init; }
}
