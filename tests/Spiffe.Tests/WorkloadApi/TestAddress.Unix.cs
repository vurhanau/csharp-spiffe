#if !OS_WINDOWS

using Spiffe.WorkloadApi;

namespace Spiffe.Tests.WorkloadApi;

/// <summary>
/// Test Unix native target URI validation.
/// </summary>
public partial class TestAddress
{
    [Fact]
    public void TestParseUnixSocketAddress()
    {
        (string Addr, string Expected, string Err)[] testCases =
        [
            (
                Addr: "unix:opaque",
                Expected: string.Empty,
                Err: "Workload endpoint unix socket URI must not be opaque"
            ),
            (
                Addr: "unix://",
                Expected: string.Empty,
                Err: "Workload endpoint unix socket URI must include a path"
            ),
            (
                Addr: "unix://foo?whatever",
                Expected: string.Empty,
                Err: "Workload endpoint unix socket URI must not include query values"
            ),
            (
                Addr: "unix://foo#whatever",
                Expected: string.Empty,
                Err: "Workload endpoint unix socket URI must not include a fragment"
            ),
            (
                Addr: "unix://john:doe@foo/path",
                Expected: string.Empty,
                Err: "Workload endpoint unix socket URI must not include user info"
            ),
            (
                Addr: "unix://foo",
                Expected: "foo",
                Err: string.Empty
            ),
            (
                Addr: "tcp://foo",
                Expected: string.Empty,
                Err: "Workload endpoint socket URI must have a supported scheme"
            ),
            (
                Addr: "unix:///tmp/api.sock",
                Expected: "/tmp/api.sock",
                Err: string.Empty
            )
        ];

        AssertParse(testCases, Address.ParseUnixSocketTarget);
    }
}

#endif
