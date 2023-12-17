namespace Spiffe.Tests;

public class TestSpiffeId
{
    [Fact]
    public void TestParseSpiffeId()
    {
        var spiffeId = Spiffe.SpiffeId.SpiffeId.Parse("spiffe://example.org");
        Console.WriteLine(spiffeId.TrustDomain);
    }
}