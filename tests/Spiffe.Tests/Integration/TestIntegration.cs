using System.ComponentModel;
using FluentAssertions;
using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;
using Xunit.Abstractions;

namespace Spiffe.Tests.Integration;

/// <summary>
/// Tests integration with Workload API over http transport.
/// </summary>
public partial class TestIntegration
{
    private readonly ITestOutputHelper _output;

    public TestIntegration(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = Constants.IntegrationTestTimeoutMillis)]
    [Category(Constants.Integration)]
    public async Task TestFetchViaHttp()
    {
        await RunTest(() =>
        {
            int port = TestServer.GetAvailablePort();
            return $"http://localhost:{port}";
        });
    }

    private async Task RunTest(Func<string> addressFunc)
    {
        for (int i = 0; i < Constants.IntegrationTestRetriesMax; i++)
        {
            try
            {
                using CancellationTokenSource cts = new();
                cts.CancelAfter(Constants.IntegrationTestTimeoutMillis);

                string address = addressFunc();
                TestServer server = new(_output);
                Task serverTask = await server.ListenAsync(address, cts.Token);

                using GrpcChannel ch = GrpcChannelFactory.CreateChannel(address);
                IWorkloadApiClient c = WorkloadApiClient.Create(ch);
                X509BundleSet resp = await c.FetchX509BundlesAsync();
                resp.Bundles.Should().ContainSingle();

                await cts.CancelAsync();
                await serverTask;

                break;
            }
            catch (Exception e)
            {
                _output.WriteLine($"Test failed, attempt: {i + 1}: {e.Message}");
                await Task.Delay(100);
            }
        }
    }
}
