using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Tests.Util;
using Spiffe.Tests.WorkloadApi;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Test.WorkloadApi;

public class TestWorkloadApiClient
{
    private static readonly TrustDomain TrustDomain = TrustDomain.FromString("example.org");

    [Fact]
    public async Task TestFetchX509Bundles()
    {
        using X509Certificate2 cert = CertLoader.FirstFromPemFile("TestData/good-leaf-only.pem");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509BundlesResponse();
        resp.Bundles.Add(TrustDomain.Name, ByteString.CopyFrom(cert.RawData));
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var b = await c.FetchX509BundlesAsync();
        VerifyX509BundleSet(b, cert.RawData);
    }

    [Fact]
    public async Task TestWatchX509Bundles()
    {
        using X509Certificate2 first = CertLoader.FirstFromPemFile("TestData/good-leaf-only.pem");
        using X509Certificate2 second = CertLoader.FirstFromPemFile("TestData/good-leaf-and-intermediate.pem");
        static X509BundlesResponse Resp(X509Certificate2 cert)
        {
            var r = new X509BundlesResponse();
            r.Bundles.Add(TrustDomain.Name, ByteString.CopyFrom(cert.RawData));
            return r;
        }

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(
                                                Resp(first),
                                                Resp(second)));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        List<X509BundleSet> receivedBundles = [];
        List<Exception> exceptions = [];
        var watcher = new Watcher<X509BundleSet>(receivedBundles.Add, exceptions.Add);

        using CancellationTokenSource cts = new();
        cts.CancelAfter(100);
        await c.WatchX509BundlesAsync(watcher, cts.Token);

        receivedBundles.Should().HaveCount(2);

        X509BundleSet b0 = receivedBundles[0];
        b0.Bundles.Should().ContainSingle();
        b0.Bundles.Should().ContainKey(TrustDomain);
        b0.Bundles[TrustDomain].X509Authorities.Should().ContainSingle();
        b0.Bundles[TrustDomain].X509Authorities[0].RawData.Should().Equal(first.RawData);

        X509BundleSet b1 = receivedBundles[1];
        b1.Bundles.Should().ContainSingle();
        b1.Bundles.Should().ContainKey(TrustDomain);
        b1.Bundles[TrustDomain].X509Authorities.Should().ContainSingle();
        b1.Bundles[TrustDomain].X509Authorities[0].RawData.Should().Equal(second.RawData);

        exceptions.Should().BeEmpty();
    }

    // TODO: fix
    [Fact]
    public async Task TestWatchX509BundlesError()
    {
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingErrorCall<X509BundlesResponse>());

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        List<X509BundleSet> receivedBundles = [];
        List<Exception> exceptions = [];
        var watcher = new Watcher<X509BundleSet>(receivedBundles.Add, exceptions.Add);

        using CancellationTokenSource cts = new();
        cts.CancelAfter(100);
        await c.WatchX509BundlesAsync(watcher, cts.Token);

        exceptions.Should().NotBeEmpty();
        receivedBundles.Should().BeEmpty();
    }

    [Fact]
    public async Task TestFetchX509Svids()
    {
        using X509Certificate2 bundleCert = CertLoader.FirstFromPemFile("TestData/good-leaf-and-intermediate.pem");
        using X509Certificate2 cert = X509Certificate2.CreateFromPemFile(
            "TestData/good-leaf-only.pem",
            "TestData/key-pkcs8-rsa.pem");
        byte[] key = cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509SVIDResponse();
        var svid = new X509SVID
        {
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            SpiffeId = "spiffe://example.org/myworkload",
            X509Svid = ByteString.CopyFrom(cert.RawData),
            X509SvidKey = ByteString.CopyFrom(key),
        };
        resp.Svids.Add(svid);

        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var r = await c.FetchX509ContextAsync();
        VerifyX509BundleSet(r.X509Bundles, bundleCert.RawData);

        r.X509Svids.Should().ContainSingle();
        X509Certificate2Collection certs = r.X509Svids[0].Certificates;
        certs.Should().ContainSingle();
        certs[0].RawData.Should().Equal(cert.RawData);
        certs[0].GetRSAPrivateKey()?.ExportPkcs8PrivateKey().Should().Equal(key);
    }

    private static void VerifyX509BundleSet(X509BundleSet s, byte[] expectedBundle)
    {
        s.Bundles.Should().ContainSingle();
        s.Bundles.Should().ContainKey(TrustDomain);
        var bundle = s.GetBundleForTrustDomain(TrustDomain);
        bundle.TrustDomain.Should().Be(TrustDomain);
        bundle.X509Authorities.Should().ContainSingle();
        bundle.X509Authorities[0].RawData.Should().Equal(expectedBundle);
    }
}
