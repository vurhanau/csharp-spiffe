using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;

namespace Spiffe.Bundle;

/// <summary>
/// Bundle is a collection of trusted public key material for a trust domain,
/// conforming to the SPIFFE Bundle Format as part of the SPIFFE Trust Domain
/// and Bundle specification:
/// <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE_Trust_Domain_and_Bundle.md"/>
/// </summary>
public class SpiffeBundle : IX509BundleSource, IJwtBundleSource
{
    internal TrustDomain TrustDomain { get; init; }

    internal TimeSpan RefreshHint { get; init; }

    internal long SequenceNumber { get; init; }

    internal X509Bundle X509Authorities { get; init; }

    internal JwtBundle JwtAuthorities { get; init; }

    public JwtBundle GetJwtBundle(TrustDomain trustDomain)
    {
        return JwtAuthorities;
    }

    public X509Bundle GetX509Bundle(TrustDomain trustDomain)
    {
        return X509Authorities;
    }
}
