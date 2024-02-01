using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Tests.Util;
using Spiffe.Tests.WorkloadApi;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Test.WorkloadApi;

public class TestX509Source
{
    [Fact(Timeout = 10_000)]
    public async Task TestGetBundleAndSvid()
    {
        SpiffeId spiffeId = SpiffeId.FromString("spiffe://example.org/workload");
        using X509Certificate2 bundleCert = CertUtil.FirstFromPemFile("TestData/good-leaf-only.pem");
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/good-leaf-only.pem",
            "TestData/key-pkcs8-rsa.pem");
        byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        string hint = "internal";
        X509SVIDResponse resp = new();
        resp.Svids.Add(new X509SVID()
        {
            SpiffeId = spiffeId.Id,
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            X509Svid = ByteString.CopyFrom(svidCert.RawData),
            X509SvidKey = ByteString.CopyFrom(svidKey),
            Hint = hint,
        });

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        X509Source s = await X509Source.CreateAsync(c);

        X509Svid svid = s.GetX509Svid();
        VerifyX509SvidRsa(svid, spiffeId, svidCert, hint);

        X509Bundle bundle = s.GetX509Bundle(spiffeId.TrustDomain);
        VerifyX509BundleSet(bundle, spiffeId.TrustDomain, svidCert);
    }

    [Fact(Timeout = 10_000)]
    public async Task TestPickSvid()
    {
        SpiffeId spiffeId = SpiffeId.FromString("spiffe://example.org/workload");
        using X509Certificate2 bundleCert = CertUtil.FirstFromPemFile("TestData/good-leaf-only.pem");
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/good-leaf-only.pem",
            "TestData/key-pkcs8-rsa.pem");
        byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        X509SVIDResponse resp = new();
        X509SVID s1 = new()
        {
            SpiffeId = spiffeId.Id,
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            X509Svid = ByteString.CopyFrom(svidCert.RawData),
            X509SvidKey = ByteString.CopyFrom(svidKey),
            Hint = "internal1",
        };
        X509SVID s2 = new(s1)
        {
            SpiffeId = "spiffe://example.org/workload2",
            Hint = "internal2",
        };
        resp.Svids.Add(s1);
        resp.Svids.Add(s2);

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        X509Source s = await X509Source.CreateAsync(c, svids => svids[1]);

        X509Svid svid = s.GetX509Svid();
        VerifyX509SvidRsa(svid, SpiffeId.FromString(s2.SpiffeId), svidCert, "internal2");
    }

    [Fact(Timeout = 10_000)]
    public async Task TestCreateCancelled()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Callback(async () => await Task.Delay(TimeSpan.FromHours(1)))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(new X509SVIDResponse()));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        // Respect cancellation token
        using CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(500);
        await Assert.ThrowsAsync<OperationCanceledException>(() => X509Source.CreateAsync(c, cancellationToken: cancellation.Token));

        // Respect timeout
        await Assert.ThrowsAsync<OperationCanceledException>(() => X509Source.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact]
    public void TestFailWhenInvalidState()
    {
        X509Source s = new(l => null);
        Action f1 = () => s.GetX509Bundle(TrustDomain.FromString("spiffe://example.org"));
        Action f2 = () => s.GetX509Svid();

        f1.Should().Throw<InvalidOperationException>();
        f2.Should().Throw<InvalidOperationException>();

        s.SetX509Context(new([], new([])));
        s.Dispose();

        f1.Should().Throw<ObjectDisposedException>();
        f2.Should().Throw<ObjectDisposedException>();
    }

    private static void VerifyX509SvidRsa(X509Svid svid,
                                          SpiffeId expectedSpiffeId,
                                          X509Certificate2 expectedCert,
                                          string expectedHint)
    {
        svid.SpiffeId.Should().Be(expectedSpiffeId);
        svid.Hint.Should().Be(expectedHint);

        X509Certificate2Collection certs = svid.Certificates;
        certs.Should().ContainSingle();
        certs[0].HasPrivateKey.Should().BeTrue();
        certs[0].RawData.Should().Equal(expectedCert.RawData);
        byte[] expectedKey = expectedCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        certs[0].GetRSAPrivateKey()?.ExportPkcs8PrivateKey().Should().Equal(expectedKey);
    }

    private static void VerifyX509BundleSet(X509Bundle bundle, TrustDomain expectedTrustDomain, X509Certificate2 expectedCert)
    {
        bundle.TrustDomain.Should().Be(expectedTrustDomain);
        bundle.X509Authorities.Should().ContainSingle();
        bundle.X509Authorities[0].RawData.Should().Equal(expectedCert.RawData);
    }
}
