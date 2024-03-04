using Grpc.Core;

namespace Spiffe.Tests.WorkloadApi;

internal class TestAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly TimeSpan _delay;

    private readonly IEnumerator<T> _enumerator;

    public TestAsyncStreamReader(IEnumerable<T> results, TimeSpan delay = default)
    {
        _enumerator = results.GetEnumerator();
        _delay = delay;
    }

    public T Current => _enumerator.Current;

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        if (_delay != default)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        return _enumerator.MoveNext();
    }
}

internal class TestErrorAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly Exception _e;

    public TestErrorAsyncStreamReader(Exception e)
    {
        _e = e;
    }

    public T Current => throw _e;

    public Task<bool> MoveNext(CancellationToken cancellationToken) => throw _e;
}

internal static class CallHelpers
{
    public static AsyncUnaryCall<TResponse> Unary<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    public static AsyncServerStreamingCall<TResponse> Stream<TResponse>(params TResponse[] response)
    {
        return Stream<TResponse>(default, response);
    }

    public static AsyncServerStreamingCall<TResponse> Stream<TResponse>(TimeSpan responseDelay, params TResponse[] response)
    {
        return new AsyncServerStreamingCall<TResponse>(
            new TestAsyncStreamReader<TResponse>(response, responseDelay),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    public static AsyncServerStreamingCall<TResponse> StreamError<TResponse>(Exception e)
    {
        return new AsyncServerStreamingCall<TResponse>(
            new TestErrorAsyncStreamReader<TResponse>(e),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
