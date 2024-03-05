using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Bundle;

/// <summary>
/// Represents a source of SPIFFE bundles keyed by trust domain.
/// </summary>
public interface ISpiffeBundleSource
{
    /// <summary>
    /// Gets a SPIFFE trust bundle associated with trust domain.
    /// </summary>
    /// <exception cref="BundleNotFoundException">Throw if there is no bundle for trust domain</exception>
    SpiffeBundle GetSpiffeBundle(TrustDomain trustDomain);
}
