namespace Spiffe.Tests.WorkloadApi;

/// <summary>
/// Test GRPC target validation.
/// </summary>
public partial class TestAddress
{
    private static void AssertParse((string Addr, string Expected, string Err)[] testCases, Func<string, string> parseFunc)
    {
        foreach ((string addr, string expected, string err) in testCases)
        {
            bool assertOk = string.IsNullOrEmpty(err);
            if (assertOk)
            {
                string actual = parseFunc(addr);
                Assert.True(expected == parseFunc(addr), $"Result mismatch: input='{addr}' expected='{expected}' actual='{actual}'");
            }
            else
            {
                Exception? e = Record.Exception(() => parseFunc(addr));
                Assert.True(e != null, $"No exception thrown: input='{addr}', expected='{err}'");
                Assert.IsType<ArgumentException>(e);
                Assert.True(e.Message.StartsWith(err), $"Error mismatch: input='{addr}' expected='{err}' actual='{e.Message}'");
            }
        }
    }
}
