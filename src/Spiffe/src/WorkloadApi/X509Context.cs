using Spiffe.Bundle.X509;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents the X.509 materials that are fetched from the Workload API.
/// Contains a list of <see cref="X509Svid"/> and a <see cref="X509BundleSet"/>.
/// </summary>
public class X509Context
{
    /// <summary>
    /// Gets X.509 SVIDs.
    /// </summary>
    public required IReadOnlyList<X509Svid> X509Svids { get; init; }

    /// <summary>
    /// Gets trust bundles.
    /// </summary>
    public required X509BundleSet X509BundleSet { get; init; }

    /// <summary>
    /// Gets the default SVID (the first in the list).
    /// </summary>
    public X509Svid? DefaultSvid => X509Svids.FirstOrDefault();
}
