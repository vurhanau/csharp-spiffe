using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Id;

namespace Spiffe.Tests.Bundle.X509;

public class TestX509Bundle
{
    [Fact]
    public void TestInitBundle()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example1.org");
        Action ok = () => new X509Bundle(td, []);
        ok.Should().NotThrow();

        Action nullTrustDomain = () => new X509Bundle(null, []);
        nullTrustDomain.Should().Throw<ArgumentNullException>();

        Action nullAuthorities = () => new X509Bundle(td, null);
        nullAuthorities.Should().Throw<ArgumentNullException>();
    }
}
