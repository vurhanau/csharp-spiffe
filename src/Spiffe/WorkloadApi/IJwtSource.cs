using Spiffe.Bundle.Jwt;
using Spiffe.Svid.Jwt;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Source of JWT-SVID and JWT bundles maintained via the Workload API.
/// </summary>
public interface IJwtSource : IJwtBundleSource, IJwtSvidSource, IDisposable;
