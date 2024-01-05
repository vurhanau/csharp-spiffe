using Spiffe.Bundle;
using Spiffe.Bundle.X509;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Source of X.509 SVIDs and Bundles.
/// </summary>
public interface IX509Source : IX509SvidSource, IBundleSource<X509Bundle>, IDisposable
{
}
