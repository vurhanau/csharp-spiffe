using Microsoft.IdentityModel.Tokens;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
/// Bundle is a collection of trusted JWT authorities for a trust domain.
/// </summary>
public class JwtBundle
{
    /// <summary>
    /// Constructor
    /// </summary>
    public JwtBundle(TrustDomain trustDomain, Dictionary<string, JsonWebKey> jwtAuthorities)
    {
        TrustDomain = trustDomain ?? throw new ArgumentNullException(nameof(trustDomain));
        JwtAuthorities = jwtAuthorities ?? throw new ArgumentNullException(nameof(jwtAuthorities));
    }

    /// <summary>
    /// Gets a trust domain associated with bundle.
    /// </summary>
    public TrustDomain TrustDomain { get; }

    /// <summary>
    /// Gets trust domain authorities.
    /// </summary>
    public Dictionary<string, JsonWebKey> JwtAuthorities { get; }
}
