using Google.Protobuf;
using Grpc.Core;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.Server;

public class WorkloadApiService : SpiffeWorkloadAPIBase
{
    public override Task FetchJWTBundles(JWTBundlesRequest request,
                                         IServerStreamWriter<JWTBundlesResponse> responseStream,
                                         ServerCallContext context)
    {
        JWTBundlesResponse resp = new();
        resp.Bundles.Add(new Dictionary<string, ByteString>()
        {
            ["hello"] = ByteString.CopyFromUtf8("world"),
        });
        return responseStream.WriteAsync(resp);
    }

    public override Task<JWTSVIDResponse> FetchJWTSVID(JWTSVIDRequest request, ServerCallContext context)
    {
        JWTSVIDResponse resp = new();
        resp.Svids.Add(new JWTSVID
        {
            SpiffeId = "spiffe://example.org/myworkload",
            Hint = "hello",
        });
        return Task.FromResult(resp);
    }
}
