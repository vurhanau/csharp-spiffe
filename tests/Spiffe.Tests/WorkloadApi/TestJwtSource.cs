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
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestJwtSource
{
    [Fact(Timeout = 10_000)]
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

    [Fact(Timeout = 10_000)]
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

    [Fact(Timeout = 10_000)]
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

    [Fact(Timeout = 10_000)]
    public async Task TestCreateWithNullClient()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => JwtSource.CreateAsync(null));
    }
}
