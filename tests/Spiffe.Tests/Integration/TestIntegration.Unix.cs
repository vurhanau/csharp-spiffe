#if !OS_WINDOWS

using System.ComponentModel;

namespace Spiffe.Tests.Integration;

/// <summary>
/// Tests integration with Workload API over unix domain socket transport.
/// </summary>
public partial class TestIntegration
{
    [Fact(Timeout = Constants.IntegrationTestTimeoutMillis)]
    [Category(Constants.Integration)]
    public async Task TestUnixDomainSocketFetchJWTSVID()
    {
        string socket = Path.Join(Path.GetTempPath(), $"workload-api-{Guid.NewGuid()}.sock");
        await RunTest($"unix://{socket}");
        File.Delete(socket);
    }
}

#endif