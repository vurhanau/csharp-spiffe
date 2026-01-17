using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestBundleSource
{
    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestGetBundles()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        SpiffeId spiffeId = SpiffeId.FromPath(td, "/workload");
        using X509Certificate2 bundleCert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/X509/good-leaf-only.pem",
            "TestData/X509/key-pkcs8-rsa.pem");
        byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        string hint = "internal";
        X509SVIDResponse x509Resp = new();
        x509Resp.Svids.Add(new X509SVID()
        {
            SpiffeId = spiffeId.Id,
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            X509Svid = ByteString.CopyFrom(svidCert.RawData),
            X509SvidKey = ByteString.CopyFrom(svidKey),
            Hint = hint,
        });

        JWTBundlesResponse jwtResp = new();
        CA ca = CA.Create(td);
        JwtBundle b = ca.JwtBundle();
        byte[] bundleBytes = JwtBundles.Serialize(b);
        jwtResp.Bundles.Add(td.Name, ByteString.CopyFrom(bundleBytes));

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(x509Resp));
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(jwtResp));

        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        using BundleSource s = await BundleSource.CreateAsync(c);

        X509Bundle x509Bundle = s.GetX509Bundle(td);
        x509Bundle.TrustDomain.Should().Be(td);
        x509Bundle.X509Authorities.Should().ContainSingle();
        x509Bundle.X509Authorities[0].RawData.Should().Equal(bundleCert.RawData);

        JwtBundle jwtBundle = s.GetJwtBundle(td);
        jwtBundle.TrustDomain.Should().Be(td);
        jwtBundle.JwtAuthorities.Should().ContainSingle();
        jwtBundle.JwtAuthorities.Keys.Should().Equal(b.JwtAuthorities.Keys);
        JsonWebKey k1 = jwtBundle.JwtAuthorities.First().Value;
        JsonWebKey k2 = b.JwtAuthorities.First().Value;
        string json1 = JsonSerializer.Serialize(k1);
        string json2 = JsonSerializer.Serialize(k2);
        json1.Should().Be(json2);
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
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

        OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(
            () => BundleSource.CreateAsync(c, timeoutMillis: 60_000, cancellationToken: cancellation.Token));
        cancellation.Token.IsCancellationRequested.Should().BeTrue();
        ex.Message.Should().Be("Bundle source initialization was cancelled.");
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateTimedOut()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        X509SVIDResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() => BundleSource.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateJwtTimedOut()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        SpiffeId spiffeId = SpiffeId.FromPath(td, "/workload");
        using X509Certificate2 bundleCert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/X509/good-leaf-only.pem",
            "TestData/X509/key-pkcs8-rsa.pem");
        byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        string hint = "internal";
        X509SVIDResponse x509Resp = new();
        x509Resp.Svids.Add(new X509SVID()
        {
            SpiffeId = spiffeId.Id,
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            X509Svid = ByteString.CopyFrom(svidCert.RawData),
            X509SvidKey = ByteString.CopyFrom(svidKey),
            Hint = hint,
        });

        JWTBundlesResponse jwtResp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(x509Resp));
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, jwtResp));

        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() => BundleSource.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateX509TimedOut()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        X509SVIDResponse x509Resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);

        JWTBundlesResponse jwtResp = new();
        CA ca = CA.Create(td);
        JwtBundle b = ca.JwtBundle();
        byte[] bundleBytes = JwtBundles.Serialize(b);
        jwtResp.Bundles.Add(td.Name, ByteString.CopyFrom(bundleBytes));

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, x509Resp));
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, jwtResp));

        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() => BundleSource.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact]
    public void TestFailWhenInvalidState()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        BundleSource s = new();
        Action f1 = () => s.GetX509Bundle(td);
        Action f2 = () => s.GetJwtBundle(td);
        f1.Should().Throw<InvalidOperationException>();
        f2.Should().Throw<InvalidOperationException>();

        s.SetX509Context(new([], new([])));
        s.SetJwtBundles(new([]));
        s.Dispose();

        f1.Should().Throw<ObjectDisposedException>();
        f2.Should().Throw<ObjectDisposedException>();
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestBundleSourceCreateWithNullClient()
    {
        Func<Task> f = () => BundleSource.CreateAsync(null);
        await f.Should().ThrowAsync<ArgumentNullException>().WithParameterName("client");
    }
}
