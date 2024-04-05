#if !OS_WINDOWS

using System.ComponentModel;
using FluentAssertions;
using Grpc.Net.Client;
using Spiffe.Grpc;

namespace Spiffe.Tests.Integration;

/// <summary>
/// Tests integration with Workload API over unix domain socket transport.
/// </summary>
public partial class TestIntegration
{
    [Fact(Timeout = Constants.IntegrationTestTimeoutMillis)]
    [Category(Constants.Integration)]
    public async Task TestFetchViaUnixSocket()
    {
        string socket = Path.Join(Path.GetTempPath(), $"workload-api-{Guid.NewGuid()}.sock");
        await RunTest($"unix://{socket}");
        File.Delete(socket);
    }
}
#endif
