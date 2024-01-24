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
    private static readonly string TrustDomainString = "example.org";

    private static readonly TrustDomain TrustDomain = TrustDomain.FromString(TrustDomainString);

    private static readonly string WorkloadSpiffeId = $"spiffe://${TrustDomainString}/myworkload";

    private static readonly byte[] s_bundleCertBytes =
        CertLoader.FromPemFile(Path.Join("TestData", "good-leaf-only.pem")).RawData;

    private static readonly ByteString s_bundleCertBytesString = ByteString.CopyFrom(s_bundleCertBytes);

    private static readonly X509Certificate2 s_workloadCert = CertLoader.FromPemFile(Path.Join("TestData", "good-cert-and-key.pem"));

    [Fact]
    public async Task TestFetchX509Bundles()
    {
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509BundlesResponse();
        resp.Bundles.Add(TrustDomain.Name, s_bundleCertBytesString);
        mockGrpcClient.Setup(c => c.FetchX509Bundles(It.IsAny<X509BundlesRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var b = await c.FetchX509BundlesAsync();
        VerifyX509BundleSet(b);
    }

    // [Fact] TODO: fix
    public async Task TestFetchX509Svids()
    {
        var mockGrpcClient = new Mock<SpiffeWorkloadAPIClient>();
        var resp = new X509SVIDResponse();
        var svid = new X509SVID
        {
            Bundle = s_bundleCertBytesString,
            SpiffeId = WorkloadSpiffeId,
            X509Svid = ByteString.CopyFrom(s_workloadCert.GetPublicKey()),
            // TODO: fix NPE
            X509SvidKey = ByteString.CopyFrom(s_workloadCert.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey()),
        };
        resp.Svids.Add(svid);

        mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
                      .Returns(CallHelpers.CreateAsyncServerStreamingCall(resp));

        var c = new WorkloadApiClient(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        var r = await c.FetchX509ContextAsync();
        VerifyX509BundleSet(r.X509Bundles);

        r.X509Svids.Should().ContainSingle();
        X509Certificate2Collection certs = r.X509Svids[0].Certificates;
        certs.Should().ContainSingle();
        certs[0].RawData.Should().Equal(s_workloadCert.RawData);
    }

    private static void VerifyX509BundleSet(X509BundleSet s)
    {
        s.Bundles.Should().ContainSingle();
        s.Bundles.Should().ContainKey(TrustDomain);
        var bundle = s.GetBundleForTrustDomain(TrustDomain);
        bundle.TrustDomain.Should().Be(TrustDomain);
        bundle.X509Authorities.Should().ContainSingle();
        bundle.X509Authorities[0].RawData.Should().Equal(s_bundleCertBytes);
    }
}
