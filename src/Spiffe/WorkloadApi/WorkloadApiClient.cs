using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Svid.Jwt;
using Spiffe.Util;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public class WorkloadApiClient : IWorkloadApiClient
{
    private readonly SpiffeWorkloadAPIClient _client;

    private readonly Action<CallOptions> _configureCall;

    private readonly ILogger _logger;

    private readonly Func<Backoff> _backoffFunc;

    private static readonly (string Key, string Value) s_spiffeHeader = ("workload.spiffe.io", "true");

    private static readonly X509SVIDRequest s_x509SvidRequest = new();

    private static readonly X509BundlesRequest s_x509BundlesRequest = new();

    private static readonly JWTBundlesRequest s_jwtBundlesRequest = new();

    internal WorkloadApiClient(SpiffeWorkloadAPIClient client,
                               Action<CallOptions> configureCall,
                               ILogger logger,
                               Func<Backoff>? backoffFunc = null)
    {
        _client = client;
        _configureCall = configureCall;
        _logger = logger;
        _backoffFunc = backoffFunc ?? Backoff.Create;
    }

    /// <summary>
    /// Creates a new Workload API client.
    /// </summary>
    public static IWorkloadApiClient Create(GrpcChannel channel, ILogger? logger = null)
    {
        _ = channel ?? throw new ArgumentNullException(nameof(channel));

        SpiffeWorkloadAPIClient client = new(channel);
        logger ??= NullLogger.Instance;

        return new WorkloadApiClient(client, NoopConfigurer, logger);
    }

    /// <inheritdoc/>
    public async Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default)
    {
        return await Fetch(
            opts => _client.FetchX509SVID(s_x509SvidRequest, opts),
            Convertor.ParseX509Context,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509ContextAsync(IWatcher<X509Context> watcher, CancellationToken cancellationToken = default)
    {
        await Watch(watcher, WatchX509ContextInternal, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509BundleSet> FetchX509BundlesAsync(CancellationToken cancellationToken = default)
    {
        return await Fetch(
            opts => _client.FetchX509Bundles(s_x509BundlesRequest, opts),
            Convertor.ParseX509BundleSet,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchX509BundlesAsync(IWatcher<X509BundleSet> watcher, CancellationToken cancellationToken = default)
    {
        await Watch(watcher, WatchX509BundlesInternal, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default)
    {
        return await FetchJwtSvidsInternal(jwtParams, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JwtBundleSet> FetchJwtBundlesAsync(CancellationToken cancellationToken = default)
    {
        return await Fetch(
            opts => _client.FetchJWTBundles(s_jwtBundlesRequest, opts),
            Convertor.ParseJwtBundleSet,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WatchJwtBundlesAsync(IWatcher<JwtBundleSet> watcher, CancellationToken cancellationToken = default)
    {
        await Watch(watcher, WatchJwtBundlesInternal, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JwtSvid> ValidateJwtSvid(string token, string audience, CancellationToken cancellationToken = default)
    {
        ValidateJWTSVIDRequest req = new()
        {
            Svid = token,
            Audience = audience,
        };
        CallOptions opts = GetCallOptions(cancellationToken);
        _ = await _client.ValidateJWTSVIDAsync(req, opts);

        return JwtSvidParser.ParseInsecure(token, [audience]);
    }

    private async Task<List<JwtSvid>> FetchJwtSvidsInternal(JwtSvidParams jwtParams, CancellationToken cancellationToken = default)
    {
        List<string> aud = [jwtParams.Audience];
        aud.AddRange(jwtParams.ExtraAudiences);

        JWTSVIDRequest request = new();
        request.Audience.AddRange(aud);
        if (jwtParams.Subject != null)
        {
            request.SpiffeId = jwtParams.Subject.ToString();
        }

        CallOptions opts = GetCallOptions(cancellationToken);
        JWTSVIDResponse response = await _client.FetchJWTSVIDAsync(request, opts);
        List<JwtSvid> svids = Convertor.ParseJwtSvids(response, aud);

        return svids;
    }

    private async Task<TResult> Fetch<TFrom, TResult>(Func<CallOptions, AsyncServerStreamingCall<TFrom>> callFunc,
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

    private async Task WatchX509ContextInternal(IWatcher<X509Context> watcher,
                                                Backoff backoff,
                                                CancellationToken cancellationToken)
    {
        await WatchInternal<X509SVIDResponse, X509Context>(
            watcher,
            opts => _client.FetchX509SVID(s_x509SvidRequest, opts),
            Convertor.ParseX509Context,
            Strings.ToString,
            backoff,
            cancellationToken);
    }

    private async Task WatchX509BundlesInternal(IWatcher<X509BundleSet> watcher,
                                                Backoff backoff,
                                                CancellationToken cancellationToken)
    {
        await WatchInternal<X509BundlesResponse, X509BundleSet>(
            watcher,
            opts => _client.FetchX509Bundles(s_x509BundlesRequest, opts),
            Convertor.ParseX509BundleSet,
            Strings.ToString,
            backoff,
            cancellationToken);
    }

    private async Task WatchJwtBundlesInternal(IWatcher<JwtBundleSet> watcher,
                                               Backoff backoff,
                                               CancellationToken cancellationToken)
    {
        await WatchInternal<JWTBundlesResponse, JwtBundleSet>(
            watcher,
            opts => _client.FetchJWTBundles(s_jwtBundlesRequest, opts),
            Convertor.ParseJwtBundleSet,
            Strings.ToString,
            backoff,
            cancellationToken);
    }

    private async Task Watch<T>(IWatcher<T> watcher,
                                Func<IWatcher<T>, Backoff, CancellationToken, Task> watchInternalFunc,
                                CancellationToken cancellationToken)
    {
        string ty = typeof(T).Name;
        _logger.LogTrace("Start watching {}", ty);

        Backoff backoff = _backoffFunc();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await watchInternalFunc(watcher, backoff, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug("Watch {ty} error: {}", ty, e);

                watcher.OnError(e);

                bool rethrow = await HandleWatchError(e, backoff, cancellationToken);
                if (rethrow)
                {
                    throw;
                }
            }
        }

        _logger.LogTrace("Stopped watching {}", ty);
    }

    private async Task WatchInternal<TFrom, TResult>(IWatcher<TResult> watcher,
                                                     Func<CallOptions, AsyncServerStreamingCall<TFrom>> callFunc,
                                                     Func<TFrom, TResult> parserFunc,
                                                     Func<TResult, bool, string> stringFunc,
                                                     Backoff backoff,
                                                     CancellationToken cancellationToken)
                                                     where TFrom : IMessage
    {
        string fty = typeof(TFrom).Name;
        string rty = typeof(TResult).Name;

        _logger.LogDebug("Watching {}", fty);

        CallOptions callOptions = GetCallOptions(cancellationToken);
        using AsyncServerStreamingCall<TFrom> call = callFunc(callOptions);
        IAsyncEnumerable<TFrom> stream = call.ResponseStream.ReadAllAsync(cancellationToken);
        await foreach (TFrom response in stream)
        {
            backoff.Reset();
            _logger.LogDebug("Backoff reset");

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("{} response: {}", fty, Strings.ToString(response));
            }

            TResult parsed = parserFunc(response);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("{} parsed response: {}", rty, stringFunc(parsed, true));
            }

            watcher.OnUpdate(parsed);
            _logger.LogDebug("{} updated", rty);
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
                _logger.LogDebug("Status code 'cancelled' - no backoff, rethrow");
                return true;
            }

            if (code == StatusCode.InvalidArgument)
            {
                _logger.LogWarning(e, "Invalid argument, canceling watch");
                return true;
            }
        }

        TimeSpan retryAfter = backoff.Duration();
        _logger.LogWarning(e, "Failed to watch the Workload API, retrying in {} seconds", retryAfter.TotalSeconds);

        try
        {
            await Task.Delay(retryAfter, cancellationToken);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogDebug(oce, "Retry backoff watch cancelled");
        }

        return false;
    }

    private CallOptions GetCallOptions(CancellationToken cancellationToken)
    {
        CallOptions options = new(headers: [new(s_spiffeHeader.Key, s_spiffeHeader.Value)],
                                  cancellationToken: cancellationToken);
        _configureCall.Invoke(options);

        return options;
    }

    private static void NoopConfigurer(CallOptions ignore)
    {
        // noop
    }
}
