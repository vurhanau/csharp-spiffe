using Grpc.Core;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Tests.Integration.Server;

public class WorkloadApiServer : SpiffeWorkloadAPIBase
{
    public override Task<JWTSVIDResponse> FetchJWTSVID(JWTSVIDRequest request, ServerCallContext context)
    {
        return Task.FromResult(new JWTSVIDResponse());
    }
}
