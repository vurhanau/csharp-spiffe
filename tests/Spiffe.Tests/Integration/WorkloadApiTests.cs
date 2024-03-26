using System.Diagnostics;
using FluentAssertions;
using Grpc.Net.Client;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;

namespace Tests.Server.IntegrationTests;

public class TestWorkloadApiIntegration
{
    [Fact]
    public async Task TestFetchJWTSVID()
    {
        var info = new ProcessStartInfo()
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = "/Users/avurhanau/Projects/spiffe/csharp-spiffe/tests/Spiffe.Tests.Server",
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        CountdownEvent e = new(1);
        var t = Task.Factory.StartNew(() =>
        {
            Process p = Process.Start(info);
            p.BeginOutputReadLine();
            e.Signal();
            p.WaitForExit();
        });

        e.Wait(30_000);
        var ch = GrpcChannel.ForAddress("http://localhost:5001");
        var c = WorkloadApiClient.Create(ch);
        var ret = await c.FetchJwtSvidsAsync(new JwtSvidParams(
            audience: "foo",
            extraAudiences: [],
            subject: null));

        ret.Should().ContainSingle();
        await t;
    }
}
