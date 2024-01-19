using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spiffe.Bundle.X509;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public class WorkloadApiClient : IWorkloadApiClient
{
    private readonly SpiffeWorkloadAPIClient _client;

    private readonly Action<CallOptions> _configureCall;

    private readonly ILogger _logger;

    private static readonly (string Key, string Value) SpiffeHeader = ("workload.spiffe.io", "true");

    private static readonly X509SVIDRequest X509SvidRequest = new();

    private static readonly X509BundlesRequest X509BundlesRequest = new();

    internal WorkloadApiClient(SpiffeWorkloadAPIClient client,
                               Action<CallOptions> configureCall,
                               ILogger logger)
    {
        _client = client;
        _configureCall = configureCall;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new Workload API client.
    /// </summary>
    public static IWorkloadApiClient Create(GrpcChannel channel)
    {
        _ = channel ?? throw new ArgumentNullException(nameof(channel));

        SpiffeWorkloadAPIClient client = new(channel);
        Action<CallOptions> configureCall = _ => { };
        ILogger logger = NullLogger.Instance;

        return new WorkloadApiClient(client, configureCall, logger);
    }

    /// <inheritdoc/>
    public async Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default)
    {
        return await FetchAsync(
            opts => _client.FetchX509SVID(X509SvidRequest, opts),
            Convertor.ParseX509Context,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509ContextAsync(IX509ContextWatcher watcher, CancellationToken cancellationToken = default)
    {
        Backoff backoff = new();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await WatchX509ContextInternal(watcher, backoff, cancellationToken);
            }
            catch (Exception e)
            {
                watcher.OnX509ContextWatchError(e);
                bool rethrow = await HandleWatchError(e, backoff, cancellationToken);
                if (rethrow)
                {
                    throw;
                }
            }
        }
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
        _configureCall?.Invoke(options);

        return options;
    }

    private async Task WatchX509ContextInternal(IX509ContextWatcher watcher, Backoff backoff, CancellationToken cancellationToken)
    {
        CallOptions callOptions = GetCallOptions(cancellationToken);

        _logger.LogDebug("Watching X509 contexts");
        using AsyncServerStreamingCall<X509SVIDResponse> call = _client.FetchX509SVID(X509SvidRequest, callOptions);
        IAsyncEnumerable<X509SVIDResponse> stream = call.ResponseStream.ReadAllAsync(cancellationToken);
        await foreach (X509SVIDResponse response in stream)
        {
            try
            {
                backoff.Reset();
                X509Context x509Context = Convertor.ParseX509Context(response);
                watcher.OnX509ContextUpdate(x509Context);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to parse X509-SVID response");
                watcher.OnX509ContextWatchError(e);
            }
        }
    }

    /// <summary>
    /// Returns true if <paramref name="e"/> should be rethrown
    /// </summary>
    private async Task<bool> HandleWatchError(Exception e,
                                              Backoff backoff,
                                              CancellationToken cancellationToken)
    {
        if (e is RpcException rpcException)
        {
            StatusCode code = rpcException.StatusCode;
            if (code == StatusCode.Cancelled)
            {
                return true;
            }

            if (code == StatusCode.InvalidArgument)
            {
                _logger.LogWarning(e, "Canceling watch");
                return true;
            }
        }

        _logger.LogWarning(e, "Failed to watch the Workload API");
        TimeSpan retryAfter = backoff.Duration();
        _logger.LogDebug("Retrying watch in {} seconds", retryAfter.TotalSeconds);
        await Task.Delay(retryAfter, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return false;
    }
}
