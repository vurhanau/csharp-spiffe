using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Svid.Jwt;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a client to interact with the Workload API.
/// <br/>
/// Supports one-time calls and watch updates for X.509 and JWT SVIDs and bundles.
/// </summary>
public interface IWorkloadApiClient
{
    /// <summary>
    /// Fetches an X.509 context on a one-time call.
    /// </summary>
    Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for X.509 context updates.
    /// <br/>
    /// A new Stream to the Workload API is opened for each call to this method, so that the client starts getting
    /// updates immediately after the Stream is ready and doesn't have to wait until the Workload API dispatches
    /// the next update based on the SVIDs TTL.
    /// </summary>
    Task WatchX509ContextAsync(IWatcher<X509Context> watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the X.509 bundles on a one-time call.
    /// </summary>
    Task<X509BundleSet> FetchX509BundlesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for X.509 bundles updates.
    /// <br/>
    /// A new Stream to the Workload API is opened for each call to this method, so that the client starts getting
    /// updates immediately after the Stream is ready and doesn't have to wait until the Workload API dispatches
    /// the next update.
    /// </summary>
    Task WatchX509BundlesAsync(IWatcher<X509BundleSet> watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all JWT-SVIDs.
    /// </summary>
    Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the JWT bundles for JWT-SVID validation,
    /// keyed by a SPIFFE ID of the trust domain to which they belong.
    /// </summary>
    Task<JwtBundleSet> FetchJwtBundlesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for changes to the JWT bundles.
    /// The watcher receives the updated JWT bundles.
    /// </summary>
    Task WatchJwtBundlesAsync(IWatcher<JwtBundleSet> watcher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the JWT-SVID token.
    /// The parsed and validated JWT-SVID is returned.
    /// </summary>
    Task<JwtSvid> ValidateJwtSvidAsync(string token, string audience, CancellationToken cancellationToken = default);
}
