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

    [Fact]
    public void TestParseEmptyKeys()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        byte[] emptyKeysJson = "{\"keys\":[]}"u8.ToArray();
        JwtBundle b = JwtBundleParser.Parse(td, emptyKeysJson);
        b.TrustDomain.Should().Be(td);
        b.JwtAuthorities.Should().BeEmpty();
    }

    [Fact]
    public void TestParseMalformedJson()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        byte[] malformedBytes = "not-valid-json-at-all!!!"u8.ToArray();
        Action f = () => JwtBundleParser.Parse(td, malformedBytes);
        f.Should().ThrowExactly<JwtBundleException>().WithMessage("Unable to parse JWKS");
    }
}
