using FluentAssertions;
using Spiffe.Bundle.Jwt;
using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Tests.Bundle.Jwt;

public class TestJwtBundleSet
{
    [Fact]
    public void TestGetBundleForTrustDomain()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        JwtBundle b1 = new(td1, []);
        JwtBundle b2 = new(td2, []);
        JwtBundleSet bs = new(new Dictionary<TrustDomain, JwtBundle>
        {
            { td1, b1 },
            { td2, b2 },
        });
        bs.GetJwtBundle(td1).Should().Be(b1);
        bs.GetJwtBundle(td2).Should().Be(b2);

        Action notFound = () => bs.GetJwtBundle(TrustDomain.FromString("spiffe://example3.org"));
        notFound.Should().Throw<BundleNotFoundException>();

        Action nullCtor = () => new JwtBundleSet(null);
        nullCtor.Should().Throw<ArgumentNullException>();
    }
}
