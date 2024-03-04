using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Tests.Helper;

namespace Spiffe.Tests.Bundle.Jwt;

public class TestJwtBundleParser
{
    [Fact]
    public async Task TestParse()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        string jwksJson = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_1.json");
        byte[] jwksBytes = Encoding.UTF8.GetBytes(jwksJson);
        JwtBundle b = JwtBundleParser.Parse(td, jwksBytes);
        b.TrustDomain.Should().Be(td);
        b.JwtAuthorities.Should().ContainSingle();
        (string kid, JsonWebKey jwk) = b.JwtAuthorities.First();
        jwk.Kid.Should().Be(kid);
        JsonWebKey expectedJwk = JsonWebKey.Create(jwksJson);
        Keys.EqualJwk(jwk, expectedJwk).Should().BeTrue();

        Action nullTrustDomain = () => JwtBundleParser.Parse(null, jwksBytes);
        nullTrustDomain.Should().ThrowExactly<ArgumentNullException>();
    }
}
