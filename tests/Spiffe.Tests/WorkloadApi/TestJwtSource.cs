using System.Diagnostics;
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
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.WorkloadApi;

public class TestJwtSource
{
    [Fact(Timeout = 10_000)]
    public async Task TestGetBundleAndSvid()
    {
        // SpiffeId spiffeId = SpiffeId.FromString("spiffe://example.org/workload");
        // using X509Certificate2 bundleCert = CertUtil.FirstFromPemFile("TestData/Jwt/");
        // using X509Certificate2 svidCert = X509Certificate2.CreateFromPemFile(
        //     "TestData/X509/good-leaf-only.pem",
        //     "TestData/X509/key-pkcs8-rsa.pem");
        // byte[] svidKey = svidCert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        // string hint = "internal";
        // JWTSVIDResponse resp = new();
        // resp.Svids.Add(new JWTSVID()
        // {
        //     SpiffeId = spiffeId.Id,
        //     Svid = ByteString.
        //     Hint = hint,
        // });

        // Mock<SpiffeWorkloadAPIClient> mockGrpcClient = new();
        // mockGrpcClient.Setup(c => c.FetchX509SVID(It.IsAny<X509SVIDRequest>(), It.IsAny<CallOptions>()))
        //               .Returns(CallHelpers.Stream(resp));
        // WorkloadApiClient c = new(mockGrpcClient.Object, _ => { }, NullLogger.Instance);
        // using X509Source s = await X509Source.CreateAsync(c);

        // X509Svid svid = s.GetX509Svid();
        // VerifyX509SvidRsa(svid, spiffeId, svidCert, hint);

        // X509Bundle bundle = s.GetX509Bundle(spiffeId.TrustDomain);
        // VerifyX509BundleSet(bundle, spiffeId.TrustDomain, svidCert);
    }
}
