using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestWorkloadApiClient
{
    private static readonly TrustDomain TrustDomain = TrustDomain.FromString("example.org");

    [Fact]
    public async Task TestFetchX509Bundles()
    {
        using X509Certificate2 cert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509BundlesResponse();
        resp.Bundles.Add("spiffe://example.org", ByteString.CopyFrom(cert.RawData));
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var b = await c.FetchX509BundlesAsync();
        b.Bundles.Should().ContainSingle();
        VerifyX509BundleSet(b, TrustDomain, cert);
    }

    [Fact]
    public async Task TestFetchJwtBundles()
    {
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new JWTBundlesResponse();
        var jwksJson = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_1.json");
        var jwks = JsonWebKeySet.Create(jwksJson);
        var jwksBytes = Encoding.UTF8.GetBytes(jwksJson);
        var td = TrustDomain.FromString("spiffe://example.org");
        resp.Bundles.Add(td.SpiffeId.Id, ByteString.CopyFrom(jwksBytes));
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var b = await c.FetchJwtBundlesAsync();
        VerifyJwtBundleSet(b, td, jwks);
    }

    [Fact]
    public async Task TestFetchX509BundlesError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<X509BundlesResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        Func<Task<X509BundleSet>> fetch = () => c.FetchX509BundlesAsync();
        await fetch.Should().ThrowAsync<Exception>(err.Message);
    }

    [Fact]
    public async Task TestFetchJwtBundlesError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<JWTBundlesResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        Func<Task<JwtBundleSet>> fetch = () => c.FetchJwtBundlesAsync();
        await fetch.Should().ThrowAsync<Exception>(err.Message);
    }

    [Fact]
    public async Task TestWatchX509Bundles()
    {
        TrustDomain firstTrustDomain = TrustDomain.FromString("spiffe://example1.org");
        using X509Certificate2 first = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");

        TrustDomain secondTrustDomain = TrustDomain.FromString("spiffe://example2.org");
        using X509Certificate2 second = Certificates.FirstFromPemFile("TestData/X509/good-leaf-and-intermediate.pem");

        static X509BundlesResponse Resp(TrustDomain trustDomain, X509Certificate2 cert)
        {
            var r = new X509BundlesResponse();
            r.Bundles.Add(trustDomain.Name, ByteString.CopyFrom(cert.RawData));
            return r;
        }

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(
                                                Resp(firstTrustDomain, first),
                                                Resp(secondTrustDomain, second)));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        List<X509BundleSet> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<X509BundleSet>(
            b =>
            {
                received.Add(b);
                if (received.Count == 2)
                {
                    done.Cancel();
                }
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchX509BundlesAsync(watcher, done.Token);

        received.Should().HaveCount(2);

        received[0].Bundles.Should().ContainSingle();
        VerifyX509BundleSet(received[0], firstTrustDomain, first);

        received[0].Bundles.Should().ContainSingle();
        VerifyX509BundleSet(received[1], secondTrustDomain, second);

        exceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task TestWatchJwtBundles()
    {
        TrustDomain firstTrustDomain = TrustDomain.FromString("spiffe://example1.org");
        string firstJwksJson = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_1.json");
        JsonWebKeySet firstJwks = JsonWebKeySet.Create(firstJwksJson);

        TrustDomain secondTrustDomain = TrustDomain.FromString("spiffe://example2.org");
        string secondJwksJson = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_2.json");
        JsonWebKeySet secondJwks = JsonWebKeySet.Create(secondJwksJson);

        static JWTBundlesResponse Resp(TrustDomain trustDomain, string jwks)
        {
            JWTBundlesResponse r = new();
            byte[] jwksBytes = Encoding.UTF8.GetBytes(jwks);
            r.Bundles.Add(trustDomain.SpiffeId.Id, ByteString.CopyFrom(jwksBytes));
            return r;
        }

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(
                                                Resp(firstTrustDomain, firstJwksJson),
                                                Resp(secondTrustDomain, secondJwksJson)));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        List<JwtBundleSet> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<JwtBundleSet>(
            b =>
            {
                received.Add(b);
                if (received.Count == 2)
                {
                    done.Cancel();
                }
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchJwtBundlesAsync(watcher, done.Token);

        received.Should().HaveCount(2);
        VerifyJwtBundleSet(received[0], firstTrustDomain, firstJwks);
        VerifyJwtBundleSet(received[1], secondTrustDomain, secondJwks);
        exceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task TestWatchX509BundlesError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<X509BundlesResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance, NoBackoff);

        List<X509BundleSet> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<X509BundleSet>(
            b =>
            {
                received.Add(b);
                done.Cancel();
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchX509BundlesAsync(watcher, done.Token);

        exceptions.Should().ContainSingle();
        exceptions[0].Should().Be(err);
        received.Should().BeEmpty();
    }

    [Fact]
    public async Task TestWatchJwtBundlesError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchJWTBundles(It.IsAny<JWTBundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<JWTBundlesResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance, NoBackoff);

        List<JwtBundleSet> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<JwtBundleSet>(
            b =>
            {
                received.Add(b);
                done.Cancel();
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchJwtBundlesAsync(watcher, done.Token);

        exceptions.Should().ContainSingle();
        exceptions[0].Should().Be(err);
        received.Should().BeEmpty();
    }

    [Fact]
    public async Task TestFetchX509Context()
    {
        var spiffeId = SpiffeId.FromPath(TrustDomain, "/workload");
        var hint = "internal";
        using X509Certificate2 bundleCert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-and-intermediate.pem");
        using X509Certificate2 cert = X509Certificate2.CreateFromPemFile(
            "TestData/X509/good-leaf-only.pem",
            "TestData/X509/key-pkcs8-rsa.pem");
        byte[] key = cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509SVIDResponse();
        resp.Svids.Add(new X509SVID
        {
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            SpiffeId = spiffeId.Id,
            Hint = hint,
            X509Svid = ByteString.CopyFrom(cert.RawData),
            X509SvidKey = ByteString.CopyFrom(key),
        });

        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var r = await c.FetchX509ContextAsync();
        r.X509Bundles.Bundles.Should().ContainSingle();
        VerifyX509BundleSet(r.X509Bundles, TrustDomain, bundleCert);

        r.X509Svids.Should().ContainSingle();
        var svid = r.X509Svids[0];
        VerifyX509SvidRsa(svid, spiffeId, cert, hint);
    }

    [Fact]
    public async Task TestFetchJwtSvids()
    {
        CA ca = CA.Create(TrustDomain);
        SpiffeId spiffeId = SpiffeId.FromPath(TrustDomain, "/workload");
        string aud = "audience";

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        JWTSVIDResponse resp = new();
        JwtSvid expectedSvid = ca.CreateJwtSvid(spiffeId, [aud], "internal");
        resp.Svids.Add(new JWTSVID
        {
            SpiffeId = expectedSvid.Id.Id,
            Svid = expectedSvid.Token,
            Hint = expectedSvid.Hint,
        });

        mockGrpcClient.Setup(c => c.FetchJWTSVIDAsync(It.IsAny<JWTSVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Unary(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        List<JwtSvid> r = await c.FetchJwtSvidsAsync(new JwtSvidParams(aud, [], null));
        r.Should().ContainSingle();
        r[0].Should().Be(expectedSvid);
    }

    [Fact]
    public async Task TestFetchX509ContextError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<X509SVIDResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        Func<Task<X509Context>> fetch = () => c.FetchX509ContextAsync();
        await fetch.Should().ThrowAsync<Exception>(err.Message);
    }

    [Fact]
    public async Task TestWatchX509Context()
    {
        using X509Certificate2 bundleCert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");

        SpiffeId spiffeId = SpiffeId.FromPath(TrustDomain, "/workload");
        string hint = "internal";
        using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
            "TestData/X509/good-leaf-only.pem",
            "TestData/X509/key-pkcs8-rsa.pem");
        byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();

        TrustDomain federatedTrustDomain = TrustDomain.FromString("spiffe://example-federated.org");
        X509Certificate2 federatedCert = Certificates.FirstFromPemFile("TestData/X509/good-leaf-and-intermediate.pem");

        // TODO: add CRL test
        var resp = new X509SVIDResponse();
        resp.Svids.Add(new X509SVID()
        {
            Bundle = ByteString.CopyFrom(bundleCert.RawData),
            SpiffeId = spiffeId.Id,
            X509Svid = ByteString.CopyFrom(svidCert.RawData),
            X509SvidKey = ByteString.CopyFrom(svidKey),
            Hint = hint,
        });
        resp.FederatedBundles.Add(new Dictionary<string, ByteString>()
        {
            { federatedTrustDomain.Name, ByteString.CopyFrom(federatedCert.RawData) },
        });

        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.Stream(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);

        List<X509Context> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<X509Context>(
            b =>
            {
                received.Add(b);
                done.Cancel();
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchX509ContextAsync(watcher, done.Token);

        received.Should().ContainSingle();
        received[0].X509Svids.Should().ContainSingle();
        VerifyX509SvidRsa(received[0].X509Svids[0], spiffeId, svidCert, hint);

        var b = received[0].X509Bundles;
        b.Bundles.Should().HaveCount(2);
        VerifyX509BundleSet(b, TrustDomain, svidCert);
        VerifyX509BundleSet(b, federatedTrustDomain, federatedCert);

        exceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task TestWatchX509ContextError()
    {
        var err = new Exception("Oops!");
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.StreamError<X509SVIDResponse>(err));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance, NoBackoff);

        List<X509Context> received = [];
        List<Exception> exceptions = [];

        using CancellationTokenSource done = new();
        var watcher = new Watcher<X509Context>(
            b =>
            {
                received.Add(b);
                done.Cancel();
            },
            ex =>
            {
                exceptions.Add(ex);
                done.Cancel();
            });

        done.CancelAfter(1000);
        await c.WatchX509ContextAsync(watcher, done.Token);

        exceptions.Should().ContainSingle();
        exceptions[0].Should().Be(err);
        received.Should().BeEmpty();
    }

    private static void VerifyX509BundleSet(X509BundleSet b, TrustDomain expectedTrustDomain, X509Certificate2 expectedCert)
    {
        b.Bundles.Should().ContainKey(expectedTrustDomain);
        var bundle = b.GetX509Bundle(expectedTrustDomain);
        bundle.TrustDomain.Should().Be(expectedTrustDomain);
        bundle.X509Authorities.Should().ContainSingle();
        bundle.X509Authorities[0].RawData.Should().Equal(expectedCert.RawData);
    }

    private static void VerifyJwtBundleSet(JwtBundleSet b, TrustDomain expectedTrustDomain, JsonWebKeySet expectedJwks)
    {
        b.Bundles.Should().ContainSingle();
        b.Bundles.Should().ContainKey(expectedTrustDomain);
        JwtBundle bundle = b.GetJwtBundle(expectedTrustDomain);
        bundle.TrustDomain.Should().Be(expectedTrustDomain);
        bundle.JwtAuthorities.Should().HaveCount(expectedJwks.Keys.Count);
        foreach ((string kid, JsonWebKey k1) in bundle.JwtAuthorities)
        {
            JsonWebKey k2 = expectedJwks.Keys.First(k => k.Kid == kid);
            Keys.EqualJwk(k1, k2).Should().BeTrue();
        }
    }

    private static void VerifyX509SvidRsa(X509Svid svid,
                                          SpiffeId expectedSpiffeId,
                                          X509Certificate2 expectedCert,
                                          string expectedHint)
    {
        svid.Id.Should().Be(expectedSpiffeId);
        svid.Hint.Should().Be(expectedHint);

        X509Certificate2Collection certs = svid.Certificates;
        certs.Should().ContainSingle();
        certs[0].HasPrivateKey.Should().BeTrue();
        certs[0].RawData.Should().Equal(expectedCert.RawData);
        var expectedKey = expectedCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        certs[0].GetRSAPrivateKey()?.ExportPkcs8PrivateKey().Should().Equal(expectedKey);
    }

    private static Backoff NoBackoff() => new()
    {
        InitialDelay = TimeSpan.Zero,
        MaxDelay = TimeSpan.Zero,
    };
}
