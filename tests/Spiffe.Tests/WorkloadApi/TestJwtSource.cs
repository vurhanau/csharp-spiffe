using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestJwtSource
{
    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestGetBundle()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        JWTBundlesResponse resp = new();
        CA ca = CA.Create(td);
        JwtBundle b = ca.JwtBundle();
        byte[] bundleBytes = JwtBundles.Serialize(b);
        resp.Bundles.Add(td.Name, ByteString.CopyFrom(bundleBytes));

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        using JwtSource s = await JwtSource.CreateAsync(c);

        JwtBundle bundle = s.GetJwtBundle(td);
        bundle.TrustDomain.Should().Be(td);
        bundle.JwtAuthorities.Should().ContainSingle();
        bundle.JwtAuthorities.Keys.Should().Equal(b.JwtAuthorities.Keys);
        JsonWebKey k1 = bundle.JwtAuthorities.First().Value;
        JsonWebKey k2 = b.JwtAuthorities.First().Value;
        string json1 = JsonSerializer.Serialize(k1);
        string json2 = JsonSerializer.Serialize(k2);
        json1.Should().Be(json2);
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestFetchJwtSvid()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        SpiffeId id = SpiffeId.FromPath(td, "/workload1");
        SpiffeId aud = SpiffeId.FromPath(td, "/workload2");
        CA ca = CA.Create(td);
        JWTSVIDResponse resp = new();
        JwtSvid svid = ca.CreateJwtSvid(id, [aud.Id], "hint");
        resp.Svids.Add(new JWTSVID
        {
            SpiffeId = svid.Id.Id,
            Svid = svid.Token,
            Hint = svid.Hint,
        });

        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(new JWTBundlesResponse()));
        mockGrpcClient.Setup(c => c.FetchJWTSVIDAsync(It.IsAny<JWTSVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Unary(resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        using JwtSource s = await JwtSource.CreateAsync(c);

        List<JwtSvid> fetched = await s.FetchJwtSvidsAsync(new JwtSvidParams(aud.Id, [], id));
        fetched.Should().ContainSingle();
        fetched[0].Should().Be(svid);
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateCancelled()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        JWTBundlesResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        using CancellationTokenSource cancellation = new();
        cancellation.CancelAfter(500);

        using JwtSource s = await JwtSource.CreateAsync(c, timeoutMillis: 60_000, cancellationToken: cancellation.Token);

        cancellation.Token.IsCancellationRequested.Should().BeTrue();
        s.IsInitialized.Should().BeFalse();
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateTimedOut()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        JWTBundlesResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => JwtSource.CreateAsync(c, timeoutMillis: 500));
    }

    [Fact]
    public void TestFailWhenInvalidState()
    {
        Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        JWTBundlesResponse resp = new();
        TimeSpan respDelay = TimeSpan.FromHours(1);
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(respDelay, resp));
        WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        JwtSource s = new(c);

        Action f = () => s.GetJwtBundle(TrustDomain.FromString("spiffe://example.org"));
        f.Should().Throw<InvalidOperationException>();

        s.SetJwtBundleSet(new([]));
        s.Dispose();

        f.Should().Throw<ObjectDisposedException>();
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestCreateWithNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => JwtSource.CreateAsync(null));
    }
}
