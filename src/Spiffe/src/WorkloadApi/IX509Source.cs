using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a source of X.509 SVIDs and X.509 bundles maintained via the Workload API.
/// </summary>
public interface IX509Source
{
    /// <summary>
    /// Gets a default SVID.
    /// </summary>
    X509Svid? GetX509Svid();

    /// <summary>
    /// Gets a trust bundle associated with trust domain.
    /// </summary>
    X509Bundle? GetX509Bundle(TrustDomain trustDomain);
}
