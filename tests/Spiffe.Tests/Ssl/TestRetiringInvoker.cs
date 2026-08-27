using System.Net;
using FluentAssertions;
using Spiffe.Ssl;

namespace Spiffe.Tests.Ssl;

public class TestRetiringInvoker
{
    [Fact]
    public async Task TestRetiredInvokerDisposesWhenLastResponseIsDisposed()
    {
        TestHandler handler = new(_ => Task.FromResult(Response(new StringContent("body"))));
        using RetiringInvoker invoker = new(new HttpMessageInvoker(handler), DateTimeOffset.UtcNow.AddHours(1));
        HttpResponseMessage response = await invoker.SendAsync(new HttpRequestMessage(), default);

        invoker.Retire(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.Zero);
        handler.IsDisposed.Should().BeFalse();
        response.Dispose();

        await handler.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TestRetiredInvokerRemainsAliveWhileStreamingResponseIsRead()
    {
        TestHandler handler = new(_ => Task.FromResult(Response(new StreamContent(new MemoryStream("body"u8.ToArray())))));
        using RetiringInvoker invoker = new(new HttpMessageInvoker(handler), DateTimeOffset.UtcNow.AddHours(1));
        using HttpResponseMessage response = await invoker.SendAsync(new HttpRequestMessage(), default);

        invoker.Retire(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.Zero);
        handler.IsDisposed.Should().BeFalse();
        (await response.Content.ReadAsStringAsync()).Should().Be("body");
        handler.IsDisposed.Should().BeFalse();

        response.Dispose();
        await handler.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TestRetiredInvokerIsForcedClosedAtDeadline()
    {
        TestHandler handler = new(_ => Task.FromResult(Response(new StringContent("body"))));
        using RetiringInvoker invoker = new(new HttpMessageInvoker(handler), DateTimeOffset.UtcNow.AddHours(1));
        _ = await invoker.SendAsync(new HttpRequestMessage(), default);

        invoker.Retire(DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(20));

        await handler.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        handler.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task TestContentReadFailureReleasesRetiredInvoker()
    {
        TestHandler handler = new(_ => Task.FromResult(Response(new ThrowingContent())));
        using RetiringInvoker invoker = new(new HttpMessageInvoker(handler), DateTimeOffset.UtcNow.AddHours(1));
        using HttpResponseMessage response = await invoker.SendAsync(new HttpRequestMessage(), default);
        invoker.Retire(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.Zero);

        Func<Task> read = async () => _ = await response.Content.ReadAsStringAsync();

        await read.Should().ThrowAsync<HttpRequestException>();
        await handler.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TestForcedRetirementCancelsRequest()
    {
        TestHandler handler = new(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Response(new StringContent("unreachable"));
        });
        using RetiringInvoker invoker = new(new HttpMessageInvoker(handler), DateTimeOffset.UtcNow.AddHours(1));

        Task<HttpResponseMessage> sendTask = invoker.SendAsync(new HttpRequestMessage(), default);

        invoker.Retire(DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(20));

        Func<Task> send = async () => _ = await sendTask;
        await send.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpResponseMessage Response(HttpContent content) => new() { Content = content };

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _send;

        internal TestHandler(Func<CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        internal TaskCompletionSource<bool> Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal bool IsDisposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _send(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            Disposed.TrySetResult(true);
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowingContent : HttpContent
    {
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            Task.FromException(new IOException());
    }
}
