#if OS_WINDOWS

using Spiffe.WorkloadApi;

namespace Tests.Spiffe.WorkloadApi;

/// <summary>
/// Test Windows native target URI validation.
/// </summary>
public partial class TestAddress
{
    [Fact]
    public void TestParseNamedPipeAddress()
    {
        (string Addr, string Expected, string Err)[] testCases =
        [
            (
                Addr: "npipe:pipeName",
                Expected: @"\\.\pipe\pipeName",
                Err: string.Empty
            ),
            (
                Addr: "npipe:pipe/name",
                Expected: @"\\.\pipe\pipe/name",
                Err: string.Empty
            ),
            (
                Addr: "npipe:pipe\\name",
                Expected: @"\\.\pipe\pipe\name",
                Err: string.Empty
            ),
            (
                Addr: "npipe:",
                Expected: string.Empty,
                Err: "Workload endpoint named pipe URI must include an opaque part"
            ),
            (
                Addr: "npipe://foo",
                Expected: string.Empty,
                Err: "Workload endpoint named pipe URI must be opaque"
            ),
            (
                Addr: "npipe:pipeName?query",
                Expected: string.Empty,
                Err: "Workload endpoint named pipe URI must not include query values"
            ),
            (
                Addr: "npipe:pipeName#fragment",
                Expected: string.Empty,
                Err: "Workload endpoint named pipe URI must not include a fragment"
            ),
        ];

        AssertParse(testCases, Address.ParseNamedPipeTarget);
    }
}
#endif