using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public class WorkloadApiClient : IWorkloadApiClient
{
    private readonly GrpcChannel _channel;

    private readonly bool _disposeChannel;

    private readonly SpiffeWorkloadAPIClient _client;

    private bool _disposed = false;

    private static readonly Metadata Headers = new()
    {
        {
            "workload.spiffe.io", "true"
        },
    };

    private static readonly X509SVIDRequest X509SvidRequest = new();

    private static readonly X509BundlesRequest X509BundlesRequest = new();

    private static readonly X509Context X509EmptyContext = new([], new([]));

    private static readonly X509BundleSet X509EmptyBundleSet = new([]);

    private WorkloadApiClient(GrpcChannel channel,
                              SpiffeWorkloadAPIClient client,
                              bool disposeChannel)
    {
        _channel = channel;
        _client = client;
        _disposeChannel = disposeChannel;
    }

    /// <summary>
    /// Creates a new Workload API client configured with the given client options.
    /// </summary>
    public static IWorkloadApiClient Create(GrpcChannel channel, bool dispose = true)
    {
        _ = channel ?? throw new ArgumentNullException(nameof(channel));

        SpiffeWorkloadAPIClient client = new(channel);
        return new WorkloadApiClient(channel, client, dispose);
    }

    /// <inheritdoc/>
    public async Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default)
    {
        return await FetchAsync(FetchX509Svids, Helper.ToX509Context, X509EmptyContext, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509ContextAsync(Action<X509Context, CancellationToken> watcher, CancellationToken cancellationToken = default)
    {
        await WatchAsync(FetchX509Svids, Helper.ToX509Context, watcher, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509BundleSet> FetchX509BundlesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchAsync(FetchX509Bundles, Helper.ToX509BundleSet, X509EmptyBundleSet, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509BundlesAsync(Action<X509BundleSet, CancellationToken> watcher, CancellationToken cancellationToken = default)
    {
        await WatchAsync(FetchX509Bundles, Helper.ToX509BundleSet, watcher, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose(bool disposing) executes in two distinct scenarios.
    /// If disposing equals true, the method has been called directly
    /// or indirectly by a user's code. Managed and unmanaged resources
    /// can be disposed.
    /// If disposing equals false, the method has been called by the
    /// runtime from inside the finalizer and you should not reference
    /// other objects. Only unmanaged resources can be disposed.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _disposeChannel)
            {
                _channel.Dispose();
            }

            _disposed = true;
        }
    }

    private IAsyncEnumerable<X509SVIDResponse> FetchX509Svids(CancellationToken cancellationToken)
    {
        AsyncServerStreamingCall<X509SVIDResponse> call = _client.FetchX509SVID(X509SvidRequest, headers: Headers, cancellationToken: cancellationToken);
        return call.ResponseStream.ReadAllAsync(cancellationToken);
    }

    private IAsyncEnumerable<X509BundlesResponse> FetchX509Bundles(CancellationToken cancellationToken)
    {
        AsyncServerStreamingCall<X509BundlesResponse> call = _client.FetchX509Bundles(X509BundlesRequest, headers: Headers, cancellationToken: cancellationToken);
        return call.ResponseStream.ReadAllAsync(cancellationToken);
    }

    private static async Task<TResult> FetchAsync<TFrom, TResult>(Func<CancellationToken, IAsyncEnumerable<TFrom>> fetchFunc,
                                                                  Func<TFrom, TResult> mapperFunc,
                                                                  TResult fallbackIfAbsent,
                                                                  CancellationToken cancellationToken)
    {
        IAsyncEnumerable<TFrom> stream = fetchFunc(cancellationToken);
        IAsyncEnumerator<TFrom> enumerator = stream.GetAsyncEnumerator(cancellationToken);
        try
        {
            return await enumerator.MoveNextAsync()
                            ? mapperFunc(enumerator.Current)
                            : fallbackIfAbsent;
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private static async Task WatchAsync<TFrom, TResult>(Func<CancellationToken, IAsyncEnumerable<TFrom>> streamFunc,
                                                         Func<TFrom, TResult> mapperFunc,
                                                         Action<TResult, CancellationToken> watcher,
                                                         CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IAsyncEnumerable<TFrom> stream = streamFunc(cancellationToken);
            await foreach (TFrom response in stream)
            {
                TResult item = mapperFunc(response);
                watcher(item, cancellationToken);
            }
        }
    }
}
