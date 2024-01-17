﻿using Spiffe.Bundle.X509;

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
    Task WatchX509ContextAsync(Action<X509Context, CancellationToken> watcher, CancellationToken cancellationToken = default);

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
    Task WatchX509BundlesAsync(Action<X509BundleSet, CancellationToken> watcher, CancellationToken cancellationToken = default);
}
