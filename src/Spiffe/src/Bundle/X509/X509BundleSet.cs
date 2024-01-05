using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Represents a set of X.509 bundles keyed by trust domain.
/// </summary>
public class X509BundleSet : IBundleSource<X509Bundle>
{
    /// <summary>
    /// Gets a trust domain to X.509 bundle mapping.
    /// </summary>
    public Dictionary<TrustDomain, X509Bundle>? Bundles { get; init; }

    internal static X509BundleSet Empty => new() { Bundles = [] };

    /// <summary>
    /// Returns the X.509 bundle for a given trust domain.
    /// </summary>
    // TODO: implement
    public X509Bundle GetBundleForTrustDomain(TrustDomain trustDomain) => throw new NotImplementedException();
}
