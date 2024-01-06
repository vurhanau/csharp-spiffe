#if OS_WINDOWS

namespace Tests.Spiffe.WorkloadApi;

/// <summary>
/// Test Windows native target URI validation.
/// </summary>
public partial class TestAddress
{
    private static partial (string Addr, string Err)[] GetNativeTargetTestCases()
    {
        return
        [
            (
                Addr: "npipe:pipeName",
                Err: string.Empty
            ),
            (
                Addr: "npipe:pipe/name",
                Err: string.Empty
            ),
            (
                Addr: "npipe:pipe\\name",
                Err: string.Empty
            ),
            (
                Addr: "npipe:",
                Err: "Workload endpoint named pipe URI must include an opaque part"
            ),
            (
                Addr: "npipe://foo",
                Err: "Workload endpoint named pipe URI must be opaque"
            ),
            (
                Addr: "npipe:pipeName?query",
                Err: "Workload endpoint named pipe URI must not include query values"
            ),
            (
                Addr: "npipe:pipeName#fragment",
                Err: "Workload endpoint named pipe URI must not include a fragment"
            ),
        ];
    }
}
#endif
