using Spiffe.Id;

namespace Spiffe.Bundle.X509;

/// <summary>
/// Represents a source of X.509 bundles keyed by trust domain.
/// </summary>
public interface IX509BundleSource
{
    /// <summary>
    /// Gets a X509 trust bundle associated with trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    X509Bundle GetX509Bundle(TrustDomain trustDomain);
}
