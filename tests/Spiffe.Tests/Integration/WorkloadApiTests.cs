using System.ComponentModel;
using FluentAssertions;
using Grpc.Net.Client;
using Spiffe.Grpc;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;
using Xunit.Abstractions;

namespace Spiffe.Tests.Integration;

public class WorkloadApiTests
{
    private readonly ITestOutputHelper _output;

    public WorkloadApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = Constants.IntegrationTestTimeoutMillis)]
    [Category(Constants.Integration)]
    public async Task TestHttpFetchJWTSVID()
    {
        using CancellationTokenSource cts = new();
        cts.CancelAfter(Constants.TestTimeoutMillis);
        int port = WorkloadApiServer.GetAvailablePort();
        string address = $"http://localhost:{port}";
        WorkloadApiServer api = new(_output);
        Task apiTask = await api.RunAsync(address, cts.Token);

        GrpcChannel ch = GrpcChannelFactory.CreateChannel(address);
        IWorkloadApiClient c = WorkloadApiClient.Create(ch);
        List<JwtSvid> resp = await c.FetchJwtSvidsAsync(new JwtSvidParams(
            audience: "foo",
            extraAudiences: [],
            subject: null));
        resp.Should().ContainSingle();

        cts.Cancel();
        await apiTask;
    }

}
