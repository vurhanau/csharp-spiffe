using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Ssl;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;

namespace Spiffe.Tests.Ssl;

public class TestSpiffeHttpHandler
{
    private static readonly TrustDomain s_td = TrustDomain.FromString("spiffe://example.test");

    private static readonly SpiffeId s_workloadId = SpiffeId.FromPath(s_td, "/workload");

    private static readonly CA s_ca = CA.Create(s_td);

    [Fact]
    public void TestUpdatedEventFiresOnSourceUpdate()
    {
        X509Source source = new(_ => s_ca.CreateX509Svid(s_workloadId));
        int updateCount = 0;
        source.Updated += () => updateCount++;

        source.SetX509Context(MakeContext());
        source.SetX509Context(MakeContext());

        updateCount.Should().Be(2);
    }

    [Fact]
    public void TestHandlerCreatedWithCurrentSvid()
    {
        using X509Source source = MakeInitializedSource();
        using SpiffeHttpHandler handler = new(source, Authorizers.AuthorizeAny());

        // Construction succeeds — the SVID was read and SslStreamCertificateContext was created
        handler.Should().NotBeNull();
    }

    [Fact]
    public void TestHandlerRefreshesOnSourceUpdate()
    {
        using X509Source source = MakeInitializedSource();
        using SpiffeHttpHandler handler = new(source, Authorizers.AuthorizeAny());
        HttpMessageInvoker first = handler.CurrentInvoker;

        source.SetX509Context(MakeContext());
        HttpMessageInvoker second = handler.CurrentInvoker;

        source.SetX509Context(MakeContext());
        HttpMessageInvoker third = handler.CurrentInvoker;

        second.Should().NotBeSameAs(first);
        third.Should().NotBeSameAs(second);
    }

    [Fact]
    public void TestDisposeUnsubscribesFromUpdated()
    {
        using X509Source source = MakeInitializedSource();
        SpiffeHttpHandler handler = new(source, Authorizers.AuthorizeAny());
        HttpMessageInvoker invokerBeforeDispose = handler.CurrentInvoker;
        handler.Dispose();

        source.SetX509Context(MakeContext());

        // Refresh must not have run: inner invoker unchanged after disposal
        handler.CurrentInvoker.Should().BeSameAs(invokerBeforeDispose);
    }

    [Fact]
    public async Task TestSendAsyncThrowsAfterDispose()
    {
        using X509Source source = MakeInitializedSource();
        SpiffeHttpHandler handler = new(source, Authorizers.AuthorizeAny());
        handler.Dispose();

        using HttpClient http = new(handler, disposeHandler: false);
        Func<Task> send = () => http.GetAsync("https://localhost");
        await send.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void TestNullArgumentsThrow()
    {
        using X509Source source = MakeInitializedSource();
        Action nullSource = () => _ = new SpiffeHttpHandler(null!, Authorizers.AuthorizeAny());
        Action nullAuthorizer = () => _ = new SpiffeHttpHandler(source, null!);

        nullSource.Should().Throw<ArgumentNullException>().WithParameterName("source");
        nullAuthorizer.Should().Throw<ArgumentNullException>().WithParameterName("authorizer");
    }

    [Fact]
    public void TestInvalidDrainDelayThrows()
    {
        using X509Source source = MakeInitializedSource();
        Action negative = () => _ = new SpiffeHttpHandler(source, Authorizers.AuthorizeAny(), TimeSpan.FromSeconds(-1));
        Action tooLarge = () => _ = new SpiffeHttpHandler(source, Authorizers.AuthorizeAny(), TimeSpan.FromMinutes(11));

        negative.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("drainDelay");
        tooLarge.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("drainDelay");
    }

    private static X509Source MakeInitializedSource()
    {
        X509Source source = new(_ => s_ca.CreateX509Svid(s_workloadId));
        source.SetX509Context(MakeContext());
        return source;
    }

    private static X509Context MakeContext()
    {
        X509Svid svid = s_ca.CreateX509Svid(s_workloadId);
        X509BundleSet bundles = new(new() { { s_td, s_ca.X509Bundle() } });
        return new([svid], bundles);
    }
}
