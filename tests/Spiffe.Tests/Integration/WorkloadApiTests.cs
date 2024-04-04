using System.Net;
using CliWrap;
using CliWrap.EventStream;
using FluentAssertions;
using Grpc.Net.Client;
using Spiffe.Svid.Jwt;
using Spiffe.Tests;
using Spiffe.WorkloadApi;
using Xunit.Abstractions;

namespace Tests.Server.IntegrationTests;

public class TestWorkloadApiIntegration
{
    private readonly ITestOutputHelper _output;

    public TestWorkloadApiIntegration(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = Constants.TestTimeoutMillis)]
    public async Task TestFetchJWTSVID()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(Constants.TestTimeoutMillis / 2);
        var t = Task.Factory.StartNew(async () =>
        {
            var cmd = Cli.Wrap("dotnet")
                        .WithArguments(["run", "--framework", "net8.0"])
                        .WithWorkingDirectory("/Users/avurhanau/Projects/spiffe/csharp-spiffe/tests/Spiffe.Tests.Server");

            await foreach (var cmdEvent in cmd.ListenAsync(cts.Token))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        _output.WriteLine($"Process started; ID: {started.ProcessId}");
                        break;
                    case StandardOutputCommandEvent stdOut:
                        _output.WriteLine($"Out> {stdOut.Text}");
                        break;
                    case StandardErrorCommandEvent stdErr:
                        _output.WriteLine($"Err> {stdErr.Text}");
                        break;
                    case ExitedCommandEvent exited:
                        _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
                        break;
                }
            }
        });
        var ch = GrpcChannel.ForAddress("http://localhost:5001");
        var c = WorkloadApiClient.Create(ch);
        var ret = await c.FetchJwtSvidsAsync(new JwtSvidParams(
            audience: "foo",
            extraAudiences: [],
            subject: null));

        ret.Should().ContainSingle();
        cts.Cancel();
        await t;
    }

    private static int FreeTcpPort()
    {
        IPEndPoint e = new(IPAddress.Loopback, port: 0);
        return e.Port;
    }
}
