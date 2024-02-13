using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
/// Represents a source of JWT bundles keyed by trust domain.
/// </summary>
public interface IJwtBundleSource
{
    /// <summary>
    /// Gets a JWT trust bundle associated with trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    JwtBundle GetJwtBundle(TrustDomain trustDomain);
}
