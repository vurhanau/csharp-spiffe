using FluentAssertions;
using Spiffe.Id;
using Spiffe.Svid.Jwt;

namespace Spiffe.Tests.Svid.Jwt;

public class TestJwtSvid
{
    [Fact]
    public void TestJwtSvidConstructorFails()
    {
        SpiffeId id = SpiffeId.FromString("spiffe://example.org/workload");
        Action f = () => new JwtSvid(null, id, [], DateTime.UtcNow, [], string.Empty);
        f.Should().Throw<ArgumentNullException>().WithParameterName("token");

        f = () => new JwtSvid("tokenxyz", null, [], DateTime.UtcNow, [], string.Empty);
        f.Should().Throw<ArgumentNullException>().WithParameterName("id");

        f = () => new JwtSvid("tokenxyz", id, null, DateTime.UtcNow, [], string.Empty);
        f.Should().Throw<ArgumentNullException>().WithParameterName("audience");

        f = () => new JwtSvid("tokenxyz", id, [], DateTime.UtcNow, null, string.Empty);
        f.Should().Throw<ArgumentNullException>().WithParameterName("claims");

        f = () => new JwtSvid("tokenxyz", id, [], DateTime.UtcNow, [], null);
        f.Should().Throw<ArgumentNullException>().WithParameterName("hint");
    }

    [Fact]
    public void TestJwtSvidEquals()
    {
        SpiffeId id = SpiffeId.FromString("spiffe://example.org/workload");
        JwtSvid s1 = new("tokenxyz", id, ["aud"], DateTime.UtcNow, [], string.Empty);
        JwtSvid s2 = new("tokenxyz", id, ["aud"], DateTime.UtcNow, [], string.Empty);
        JwtSvid s3 = new("tokenabc", id, ["aud"], DateTime.UtcNow, [], string.Empty);
        s1.Equals(s2).Should().BeTrue();
        s1.GetHashCode().Should().Be(s2.GetHashCode());
        s1.Equals(s3).Should().BeFalse();
        s1.GetHashCode().Should().NotBe(s3.GetHashCode());
        s1.Equals(new object()).Should().BeFalse();
    }
}
