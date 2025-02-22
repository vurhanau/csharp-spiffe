using Spiffe.Bundle.X509;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
///     Represents the X.509 materials that are fetched from the Workload API.
///     <br />
///     Contains a list of <see cref="X509Svid" /> and <see cref="X509BundleSet" /> .
/// </summary>
public class X509Context(List<X509Svid> svids, X509BundleSet bundles)
{
    /// <summary>
    ///     Gets X.509 SVIDs.
    /// </summary>
    public List<X509Svid> X509Svids { get; } = svids;

    /// <summary>
    ///     Gets trust bundles.
    /// </summary>
    public X509BundleSet X509Bundles { get; } = bundles;
}
