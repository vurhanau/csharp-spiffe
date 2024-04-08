#if OS_WINDOWS

using System.ComponentModel;

namespace Spiffe.Tests.Integration;

/// <summary>
/// Tests integration with Workload API over named pipe transport.
/// </summary>
public partial class TestIntegration
{
    [Fact(Timeout = Constants.IntegrationTestTimeoutMillis)]
    [Category(Constants.Integration)]
    public async Task TestFetchViaNamedPipe()
    {
        string namedPipe = $"npipe:workload-api-{Guid.NewGuid()}";
        await RunTest(namedPipe);
    }
}
#endif
