using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Represents a set of X.509 bundles keyed by trust domain.
/// </summary>
public class X509BundleSet
{
    /// <summary>
    /// Gets a trust domain to X.509 bundle mapping.
    /// </summary>
    public Dictionary<TrustDomain, X509Bundle>? Bundles { get; init; }

    internal static X509BundleSet Empty => new() { Bundles = [] };
}
