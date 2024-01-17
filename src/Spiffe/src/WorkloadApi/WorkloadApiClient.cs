using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public class WorkloadApiClient : IWorkloadApiClient
{
    private readonly SpiffeWorkloadAPIClient _client;

    private readonly Action<CallOptions>? _configureCallOptions;

    private static readonly (string Key, string Value) SpiffeHeader = ("workload.spiffe.io", "true");

    private static readonly X509SVIDRequest X509SvidRequest = new();

    private static readonly X509BundlesRequest X509BundlesRequest = new();

    // Visible for testing
    internal WorkloadApiClient(SpiffeWorkloadAPIClient client, Action<CallOptions>? configureCallOptions)
    {
        _client = client;
        _configureCallOptions = configureCallOptions;
    }

    /// <summary>
    /// Creates a new Workload API client.
    /// </summary>
    public static IWorkloadApiClient Create(GrpcChannel channel)
    {
        _ = channel ?? throw new ArgumentNullException(nameof(channel));

        SpiffeWorkloadAPIClient client = new(channel);
        return new WorkloadApiClient(client, null);
    }

    /// <inheritdoc/>
    public async Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default)
    {
        return await FetchAsync(
            opts => _client.FetchX509SVID(X509SvidRequest, opts),
            Convertor.ToX509Context,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509ContextAsync(Action<X509Context, CancellationToken> watcher, CancellationToken cancellationToken = default)
    {
        await WatchAsync(
            opts => _client.FetchX509SVID(X509SvidRequest, opts),
            Convertor.ToX509Context,
            watcher,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509BundleSet> FetchX509BundlesAsync(CancellationToken cancellationToken = default)
    {
        return await FetchAsync(
            opts => _client.FetchX509Bundles(X509BundlesRequest, opts),
            Convertor.ToX509BundleSet,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509BundlesAsync(Action<X509BundleSet, CancellationToken> watcher, CancellationToken cancellationToken = default)
    {
        await WatchAsync(
            opts => _client.FetchX509Bundles(X509BundlesRequest, opts),
            Convertor.ToX509BundleSet,
            watcher,
            cancellationToken);
    }

    private async Task<TResult> FetchAsync<TFrom, TResult>(Func<CallOptions, AsyncServerStreamingCall<TFrom>> callFunc,
                                                           Func<TFrom, TResult> mapperFunc,
                                                           CancellationToken cancellationToken)
    {
        CallOptions callOptions = GetCallOptions(cancellationToken);
        using AsyncServerStreamingCall<TFrom> call = callFunc(callOptions);
        IAsyncEnumerable<TFrom> stream = call.ResponseStream.ReadAllAsync(cancellationToken);
        IAsyncEnumerator<TFrom> enumerator = stream.GetAsyncEnumerator(cancellationToken);
        try
        {
            bool hasItem = await enumerator.MoveNextAsync();
            if (!hasItem)
            {
                throw new InvalidOperationException("Failed to fetch item: enumerator is empty");
            }

            return mapperFunc(enumerator.Current);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private async Task WatchAsync<TFrom, TResult>(Func<CallOptions, AsyncServerStreamingCall<TFrom>> callFunc,
                                                  Func<TFrom, TResult> mapperFunc,
                                                  Action<TResult, CancellationToken> watcher,
                                                  CancellationToken cancellationToken)
    {
        CallOptions callOptions = GetCallOptions(cancellationToken);
        using AsyncServerStreamingCall<TFrom> call = callFunc(callOptions);
        IAsyncEnumerable<TFrom> stream = call.ResponseStream.ReadAllAsync(cancellationToken);
        await foreach (TFrom response in stream)
        {
            TResult item = mapperFunc(response);
            watcher?.Invoke(item, cancellationToken);
        }
    }

    private CallOptions GetCallOptions(CancellationToken cancellationToken)
    {
        CallOptions options = new(headers: [new(SpiffeHeader.Key, SpiffeHeader.Value)],
                                  cancellationToken: cancellationToken);
        _configureCallOptions?.Invoke(options);

        return options;
    }
}
