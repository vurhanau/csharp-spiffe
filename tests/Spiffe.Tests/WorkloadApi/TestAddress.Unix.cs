#if !OS_WINDOWS

namespace Tests.Spiffe.WorkloadApi;

/// <summary>
/// Test Unix native target URI validation.
/// </summary>
public partial class TestAddress
{
    private static partial (string Addr, string Err)[] GetNativeTargetTestCases()
    {
        return
        [
            (
                Addr: "unix:opaque",
                Err: "Workload endpoint unix socket URI must include a host"
            ),
            (
                Addr: "unix://",
                Err: "Workload endpoint unix socket URI must include a host"
            ),
            (
                Addr: "unix://foo?whatever",
                Err: "Workload endpoint unix socket URI must not include query values"
            ),
            (
                Addr: "unix://foo#whatever",
                Err: "Workload endpoint unix socket URI must not include a fragment"
            ),
            (
                Addr: "unix://john:doe@foo/path",
                Err: "Workload endpoint unix socket URI must not include user info"
            ),
            (
                Addr: "unix://foo",
                Err: string.Empty
            )
        ];
    }
}
#endif
