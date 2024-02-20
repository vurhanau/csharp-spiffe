using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Represents a set of X.509 bundles keyed by trust domain.
/// </summary>
public class X509BundleSet
{
    /// <summary>
    /// Contrustor
    /// </summary>
    public X509BundleSet(Dictionary<TrustDomain, X509Bundle> bundles)
    {
        Bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
    }

    /// <summary>
    /// Gets a trust domain to X.509 bundle mapping.
    /// </summary>
    public Dictionary<TrustDomain, X509Bundle> Bundles { get; }

    /// <summary>
    /// Gets a bundle associated with the trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    public X509Bundle GetBundleForTrustDomain(TrustDomain trustDomain)
    {
        bool found = Bundles.TryGetValue(trustDomain, out X509Bundle? bundle);
        if (!found || bundle == null)
        {
            string message = $"Bundle not found for trust domain '{trustDomain}'";
            throw new BundleNotFoundException(message);
        }

        return bundle;
    }
}
