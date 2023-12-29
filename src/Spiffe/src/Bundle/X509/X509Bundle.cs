using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Represents a collection of trusted X.509 authorities for a trust domain.
/// </summary>
public class X509Bundle
{
    /// <summary>
    /// Gets a trust domain associated with bundle.
    /// </summary>
    public required SpiffeTrustDomain TrustDomain { get; init; }

    /// <summary>
    /// Gets trust domain trusted authorities.
    /// </summary>
    public required IReadOnlyList<X509Certificate2> X509Authorities { get; init; }
}
