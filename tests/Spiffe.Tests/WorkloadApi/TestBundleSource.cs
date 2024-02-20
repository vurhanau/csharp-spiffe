using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Tests.Util;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestBundleSource
{
    [Fact(Timeout = 10_000)]
    public async Task TestGetX509Bundle()
    {
        SpiffeId spiffeId = SpiffeId.FromString("spiffe://example.org/workload");
        using X509Certificate2 bundleCert = CertUtil.FirstFromPemFile("TestData/X509/good-leaf-only.pem");
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/X509/good-leaf-only.pem",
            "TestData/X509/key-pkcs8-rsa.pem");
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
                      .Returns(CallHelpers.Stream(resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        using BundleSource s = await BundleSource.CreateAsync(c);

        X509Bundle bundle = s.GetX509Bundle(spiffeId.TrustDomain);
        bundle.TrustDomain.Should().Be(spiffeId.TrustDomain);
        bundle.X509Authorities.Should().ContainSingle();
        bundle.X509Authorities[0].RawData.Should().Equal(bundleCert.RawData);
    }

    [Fact(Timeout = 10_000)]
    public async Task TestCreateCancelled()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        X509SVIDResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        using CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(500);
        Stopwatch stopwatch = Stopwatch.StartNew();

        using BundleSource s = await BundleSource.CreateAsync(c, timeoutMillis: 60_000, cancellationToken: cancellation.Token);

        stopwatch.ElapsedMilliseconds.Should().BeInRange(250, 5000);
        cancellation.Token.IsCancellationRequested.Should().BeTrue();
        s.IsInitialized.Should().BeFalse();
    }

    [Fact(Timeout = 10_000)]
    public async Task TestCreateTimedOut()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        X509SVIDResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => BundleSource.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact]
    public void TestFailWhenInvalidState()
    {
        BundleSource s = new();
        Action f1 = () => s.GetX509Bundle(TrustDomain.FromString("spiffe://example.org"));

        f1.Should().Throw<InvalidOperationException>();

        s.SetX509Context(new([], new([])));
        s.Dispose();

        f1.Should().Throw<ObjectDisposedException>();
    }
}
