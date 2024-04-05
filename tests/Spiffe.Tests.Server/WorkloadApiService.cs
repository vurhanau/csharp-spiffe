using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Grpc.Core;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.Server;

public class WorkloadApiService : SpiffeWorkloadAPIBase
{
    private const string SpiffeId = "spiffe://example.org/workload1";

    private const string BundlePem = @"
        -----BEGIN CERTIFICATE-----
        MIICBjCCAYygAwIBAgIQNj0chc2GkwvkNG0vVbWuADAKBggqhkjOPQQDAzAeMQsw
        CQYDVQQGEwJVUzEPMA0GA1UEChMGU1BJRkZFMB4XDTIwMDMyNDE0MTM0MVoXDTIw
        MDMyNDE1MTM1MVowHTELMAkGA1UEBhMCVVMxDjAMBgNVBAoTBVNQSVJFMFkwEwYH
        KoZIzj0CAQYIKoZIzj0DAQcDQgAE8ST0O2obQ9VYEyFEbiyIML7naZtAtA9DU9df
        zYCeA4fHplrgk0ZL+MBXOMjCEo0fLX+jxqMpLuPy7wGfwlqRaKOBrDCBqTAOBgNV
        HQ8BAf8EBAMCA6gwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAwGA1Ud
        EwEB/wQCMAAwHQYDVR0OBBYEFB3UYIIkxjMf9rh9tvwj86Y24+6gMB8GA1UdIwQY
        MBaAFNM1QzCBy3PuB2d3zJi6GSEqqVF5MCoGA1UdEQQjMCGGH3NwaWZmZTovL2V4
        YW1wbGUub3JnL3dvcmtsb2FkLTEwCgYIKoZIzj0EAwMDaAAwZQIwKhlIltKg+K/3
        W05Snv56s7X9NuUDKHjaCQsutyIiYxbxQz5jZgjafMusAwr+lMQkAjEAsY4Omqtj
        MT7lix7GtnRkvgmaWRTyooxyR1C2w8PYS6lSo6FJCIV6e1EBvryj6Vm1
        -----END CERTIFICATE-----";

    public override async Task FetchX509Bundles(X509BundlesRequest request, IServerStreamWriter<X509BundlesResponse> responseStream, ServerCallContext context)
    {
        X509BundlesResponse resp = new();
        X509Certificate2Collection c = [];
        c.ImportFromPem(BundlePem);
        resp.Bundles.Add(SpiffeId, ByteString.CopyFrom(c[0].RawData));
        await responseStream.WriteAsync(resp);
        c[0].Dispose();
    }
}
