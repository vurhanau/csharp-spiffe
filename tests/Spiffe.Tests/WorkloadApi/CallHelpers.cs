using Grpc.Core;

namespace Spiffe.Tests.WorkloadApi;

internal class MyAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IEnumerator<T> enumerator;

    public MyAsyncStreamReader(IEnumerable<T> results)
    {
        enumerator = results.GetEnumerator();
    }

    public T Current => enumerator.Current;

    public Task<bool> MoveNext(CancellationToken cancellationToken) =>
        Task.Run(() => enumerator.MoveNext());
}

internal static class CallHelpers
{
    public static AsyncServerStreamingCall<TResponse> CreateAsyncServerStreamingCall<TResponse>(TResponse response)
    {
        return new AsyncServerStreamingCall<TResponse>(
            new MyAsyncStreamReader<TResponse>([response]),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(StatusCode statusCode)
    {
        var status = new Status(statusCode, string.Empty);
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(new RpcException(status)),
            Task.FromResult(new Metadata()),
            () => status,
            () => new Metadata(),
            () => { });
    }
}
