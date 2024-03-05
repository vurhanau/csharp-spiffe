using Spiffe.Id;

namespace Spiffe.Bundle;

/// <summary>
/// Represents a set of JWT bundles keyed by trust domain.
/// </summary>
public class SpiffeBundleSet : ISpiffeBundleSource
{
    /// <inheritdoc/>
    public SpiffeBundle GetSpiffeBundle(TrustDomain trustDomain) => throw new NotImplementedException();
}
