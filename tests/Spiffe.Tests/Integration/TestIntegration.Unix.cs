using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        Action cleanup = () => { };
        await RunTest(() =>
        {
            string socket = Path.Join(Path.GetTempPath(), $"workload-api-{Guid.NewGuid()}.sock");
            cleanup += () => File.Delete(socket);
            return $"unix://{socket}";
        });

        cleanup();
    }
}
