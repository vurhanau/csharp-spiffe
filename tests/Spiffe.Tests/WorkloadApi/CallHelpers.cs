using Grpc.Core;

namespace Spiffe.Tests.WorkloadApi;

internal class TestAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IEnumerator<T> enumerator;

    public TestAsyncStreamReader(IEnumerable<T> results)
    {
        enumerator = results.GetEnumerator();
    }

    public T Current => enumerator.Current;

    public Task<bool> MoveNext(CancellationToken cancellationToken) =>
        Task.Run(enumerator.MoveNext);
}

internal class TestErrorAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    public T Current => throw new Exception("Oops!");

    public Task<bool> MoveNext(CancellationToken cancellationToken) => throw new Exception("Oops!");
}

internal static class CallHelpers
{
    public static AsyncServerStreamingCall<TResponse> CreateAsyncServerStreamingCall<TResponse>(params TResponse[] response)
    {
        return new AsyncServerStreamingCall<TResponse>(
            new TestAsyncStreamReader<TResponse>(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    public static AsyncServerStreamingCall<TResponse> CreateAsyncServerStreamingErrorCall<TResponse>()
    {
        return new AsyncServerStreamingCall<TResponse>(
            new TestErrorAsyncStreamReader<TResponse>(),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
