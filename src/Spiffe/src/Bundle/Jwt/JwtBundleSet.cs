using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
/// Represents a set of JWT bundles keyed by trust domain.
/// </summary>
public class JwtBundleSet
{
    /// <summary>
    /// Contrustor
    /// </summary>
    public JwtBundleSet(Dictionary<TrustDomain, JwtBundle> bundles)
    {
        Bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
    }

    /// <summary>
    /// Gets a trust domain to X.509 bundle mapping.
    /// </summary>
    public Dictionary<TrustDomain, JwtBundle> Bundles { get; }

    /// <summary>
    /// Gets a bundle associated with the trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    public JwtBundle GetBundleForTrustDomain(TrustDomain trustDomain)
    {
        bool found = Bundles.TryGetValue(trustDomain, out JwtBundle? bundle);
        if (!found || bundle == null)
        {
            string message = $"Bundle not found for trust domain '{trustDomain}'";
            throw new BundleNotFoundException(message);
        }

        return bundle;
    }
}
