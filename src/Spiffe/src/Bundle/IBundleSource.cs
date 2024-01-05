using Spiffe.Id;

namespace Spiffe.Bundle;

/// <summary>
/// Represents a source of bundles of type T keyed by trust domain.
/// </summary>
/// <typeparam name="T">Bundle type</typeparam>
public interface IBundleSource<out T>
{
    /// <summary>
    /// Returns the bundle of type T associated to the given trust domain.
    /// </summary>
    T GetBundleForTrustDomain(TrustDomain trustDomain);
}
