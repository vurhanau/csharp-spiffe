using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Bundle is a collection of trusted X.509 authorities for a trust domain.
/// </summary>
public class X509Bundle
{
    /// <summary>
    /// Constructor
    /// </summary>
    public X509Bundle(TrustDomain trustDomain, X509Certificate2Collection x509Authorities)
    {
        TrustDomain = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));
        X509Authorities = x509Authorities ?? throw new ArgumentNullException(nameof(x509Authorities));
    }

    /// <summary>
    /// Gets a trust domain associated with bundle.
    /// </summary>
    public TrustDomain TrustDomain { get; }

    /// <summary>
    /// Gets trust domain authorities.
    /// </summary>
    public X509Certificate2Collection X509Authorities { get; }
}
