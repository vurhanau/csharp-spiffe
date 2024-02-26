using FluentAssertions;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;

namespace Spiffe.Tests.Bundle.Jwt;

public class TestJwtBundle
{
    [Fact]
    public void TestInitBundle()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        JwtBundle b = new(td, []);
        b.TrustDomain.Should().Be(td);
        b.JwtAuthorities.Should().BeEmpty();

        Action nullTrustDomain = () => new JwtBundle(null, []);
        nullTrustDomain.Should().Throw<ArgumentNullException>();

        Action nullJwtAuthorities = () => new JwtBundle(td, null);
        nullJwtAuthorities.Should().Throw<ArgumentNullException>();
    }
}
