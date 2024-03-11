using Spiffe.Bundle.X509;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a source of X.509 SVIDs and X.509 bundles maintained via the Workload API.
/// </summary>
public interface IX509Source : IX509BundleSource, IX509SvidSource, IDisposable;
