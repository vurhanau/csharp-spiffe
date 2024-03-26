using Google.Protobuf;
using Grpc.Core;
using Spiffe.Tests.Common;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.Server;

public class WorkloadApiService : SpiffeWorkloadAPIBase
{
    private const string SpiffeId = "spiffe://example.org/workload1";

    private static readonly CA s_ca = CA.Create();

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
        string svid = s_ca.CreateJwtSvid(SpiffeId, request.Audience, string.Empty);
        resp.Svids.Add(new JWTSVID
        {
            Svid = svid,
            SpiffeId = SpiffeId,
            Hint = string.Empty,
        });
        return Task.FromResult(resp);
    }
}
