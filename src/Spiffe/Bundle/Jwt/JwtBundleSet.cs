using System.Collections.ObjectModel;
using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
/// Represents a set of JWT bundles keyed by trust domain.
/// </summary>
public class JwtBundleSet : IJwtBundleSource
{
    /// <summary>
    /// Contrustor
    /// </summary>
    public JwtBundleSet(Dictionary<TrustDomain, JwtBundle> bundles)
    {
        _ = bundles ?? throw new ArgumentNullException(nameof(bundles));

        Bundles = new ReadOnlyDictionary<TrustDomain, JwtBundle>(bundles);
    }

    /// <summary>
    /// Gets a trust domain to X.509 bundle mapping.
    /// </summary>
    public IDictionary<TrustDomain, JwtBundle> Bundles { get; }

    /// <summary>
    /// Gets a bundle associated with the trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    public JwtBundle GetJwtBundle(TrustDomain trustDomain)
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
