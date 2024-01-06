using Spiffe.WorkloadApi;

namespace Tests.Spiffe.WorkloadApi;

/// <summary>
/// Test GRPC target validation.
/// </summary>
public partial class TestAddress
{
    [Fact]
    public void TestValidate()
    {
        var testCases = new (string Addr, string Err)[]
        {
            (
                Addr: "\t",
                Err: "Workload endpoint tcp socket URI format could not be determined"
            ),
            (
                Addr: "blah",
                Err: "Workload endpoint tcp socket URI format could not be determined"
            ),
            (
                Addr: "tcp:opaque",
                Err: "Workload endpoint tcp socket URI must include a host"
            ),
            (
                Addr: "tcp://",
                Err: "Workload endpoint tcp socket URI must include a host"
            ),
            (
                Addr: "tcp://1.2.3.4:5?whatever",
                Err: "Workload endpoint tcp socket URI must not include path and query values"
            ),
            (
                Addr: "tcp://1.2.3.4:5#whatever",
                Err: "Workload endpoint tcp socket URI must not include a fragment"
            ),
            (
                Addr: "tcp://john:doe@1.2.3.4:5/path",
                Err: "Workload endpoint tcp socket URI must not include user info"
            ),
            (
                Addr: "tcp://1.2.3.4:5/path",
                Err: "Workload endpoint tcp socket URI must not include path and query values"
            ),
            (
                Addr: "tcp://foo",
                Err: "Workload endpoint tcp socket URI host component must be an IP:port"
            ),
            (
                Addr: "tcp://1.2.3.4",
                Err: "Workload endpoint tcp socket URI host component must include a port"
            ),
            (
                Addr: "tcp://1.2.3.4:5",
                Err: string.Empty
            ),
        };
        AssertValidation(testCases);

        testCases = GetNativeTargetTestCases();
        AssertValidation(testCases);
    }

    private static void AssertValidation((string Addr, string Err)[] testCases)
    {
        foreach ((string addr, string err) in testCases)
        {
            bool assertOk = string.IsNullOrEmpty(err);
            if (assertOk)
            {
                Address.Validate(addr);
            }
            else
            {
                Exception e = Record.Exception(() => Address.Validate(addr));
                Assert.True(e != null, $"No exception thrown: input='{addr}', expected='{err}'");
                Assert.IsType<ArgumentException>(e);
                Assert.True(e.Message.StartsWith(err), $"Error mismatch: input='{addr}' expected='{err}' actual='{e.Message}'");
            }
        }
    }

    private static partial (string Addr, string Err)[] GetNativeTargetTestCases();
}
