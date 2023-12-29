namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a client to interact with the Workload API.
/// <br/>
/// Supports one-shot calls and watch updates for X.509 and JWT SVIDs and bundles.
/// </summary>
public interface IWorkloadApiClient : IDisposable
{
    /// <summary>
    /// Fetches an X.509 context on a one-shot call.
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
    /// Fetches the X.509 bundles on a one-shot call.
    /// </summary>
    Task FetchX509BundlesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for X.509 bundles updates.
    /// <br/>
    /// A new Stream to the Workload API is opened for each call to this method, so that the client starts getting
    /// updates immediately after the Stream is ready and doesn't have to wait until the Workload API dispatches
    /// the next update.
    /// </summary>
    Task WatchX509BundlesAsync(CancellationToken cancellationToken = default);
}
