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
    public TrustDomain? TrustDomain { get; init; }

    /// <summary>
    /// Gets trust domain authority chain.
    /// </summary>
    public X509Chain? Chain { get; init; }
}
