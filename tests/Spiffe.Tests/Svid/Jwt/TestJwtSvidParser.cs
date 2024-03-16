using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Tests.Helper;
using Claims = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;
using J = Spiffe.Tests.Helper.Jwt;

namespace Spiffe.Tests.Svid.Jwt;

public class TestJwtSvidParser
{
    private static readonly TrustDomain s_td = TrustDomain.FromString("spiffe://example.org");

    private static readonly SpiffeId s_workload1 = SpiffeId.FromPath(s_td, "/workload1");

    private static readonly SpiffeId s_workload2 = SpiffeId.FromPath(s_td, "/workload2");

    private static readonly CA s_ca = CA.Create(s_td);

    private static readonly IJwtBundleSource s_source = new JwtBundleSet(new() { { s_td, s_ca.JwtBundle() } });

    private static Func<List<Claim>, string> Jwt => c => J.Generate(c, s_ca.JwtKey, s_ca.JwtKid);

    [Fact]
    public async Task TestParse()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string jwt = Jwt(claims);
        JwtSvid parsed = await JwtSvidParser.ParseAsync(jwt, s_source, [s_workload2.Id]);
        parsed.Should().Be(new JwtSvid(
            token: jwt,
            id: s_workload1,
            audience: [s_workload2.Id],
            expiry: now.AddHours(1),
            claims: claims.ToDictionary(c => c.Type, c => c.Value),
            hint: string.Empty));
    }

    [Fact]
    public async Task TestValidate()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string jwt = Jwt(claims);
        TokenValidationResult validationResult = await JwtSvidParser.ValidateAsync(jwt, s_source, [s_workload2.Id]);
        validationResult.IsValid.Should().BeTrue();

        Func<Task> f = async () => await JwtSvidParser.ValidateAsync(jwt, s_source, [s_workload1.Id]);
        await f.Should().ThrowAsync<JwtSvidException>().WithMessage($"Expected audience is {s_workload1.Id} (audience={s_workload2.Id})");
    }

    [Fact]
    public async Task TestParseFailsIfMissingSubject()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage("Token missing sub claim");
    }

    [Fact]
    public async Task TestParseFailsIfMissingExp()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage("Token missing exp claim");
    }

    [Fact]
    public async Task TestParseFailsIfSubjectInvalid()
    {
        DateTime now = DateTime.UtcNow;
        string sub = "invalid spiffe id";
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, sub),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage($"Token has an invalid subject claim: '{sub}'");
    }

    [Fact]
    public async Task TestParseFailsIfAudienceInvalid()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        SpiffeId workload3 = SpiffeId.FromPath(s_td, "/workload3");
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [workload3.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage($"Expected audience is {workload3.Id} (audience={s_workload2.Id})");
    }

    [Fact]
    public async Task TestParseFailsIfValidFromInvalid()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Nbf, J.ToNumericDate(now.AddDays(1))),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage("Validation failed, token not valid yet (nbf)");
    }

    [Fact]
    public async Task TestParseIssuedAtUnset()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string jwt = Jwt(claims);
        JwtSvid parsed = await JwtSvidParser.ParseAsync(jwt, s_source, [s_workload2.Id]);
        parsed.Should().Be(new JwtSvid(
            token: jwt,
            id: s_workload1,
            audience: [s_workload2.Id],
            expiry: now.AddHours(1),
            claims: claims.ToDictionary(c => c.Type, c => c.Value),
            hint: string.Empty));
    }

    [Fact]
    public async Task TestParseFailsIfKidMissing()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string jwt = J.Generate(claims, s_ca.JwtKey, string.Empty);
        Func<Task> f = async () => await JwtSvidParser.ParseAsync(jwt, s_source, [s_workload2.Id]);
        await f.Should().ThrowAsync<JwtSvidException>().WithMessage("Token header missing key id");
    }

    [Fact]
    public async Task TestParseFailsIfAlgUnknown()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        JwtHeader header = new()
        {
            ["alg"] = "unknown",
        };
        JwtPayload payload = new(claims);
        JwtSecurityToken jwt = new(header, payload);
        string str = new JwtSecurityTokenHandler().WriteToken(jwt);
        Func<Task> f = async () => await JwtSvidParser.ParseAsync(str, s_source, [s_workload2.Id]);
        await f.Should().ThrowAsync<JwtSvidException>().WithMessage("Unsupported token signature algorithm 'unknown'");
    }

    [Fact]
    public async Task TestParseFailsIfValidToInvalid()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now.AddYears(-2))),
            new(Claims.Exp, J.ToNumericDate(now.AddYears(-1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage("Validation failed, token is expired (exp)");
    }

    [Fact]
    public async Task TestParseFailsIfIssuedAtInvalid()
    {
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now.AddYears(1))),
            new(Claims.Exp, J.ToNumericDate(now.AddYears(2))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = Jwt(claims);
        Func<Task> fn = async () => await JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>().WithMessage("Validation failed, token issued in the future (iat)");
    }

    [Fact]
    public async Task TestParseWithInvalidSignature()
    {
        string jwt1 = s_ca.CreateJwtSvid(s_workload1, [s_workload2.Id]).Token;
        string jwt2 = s_ca.CreateJwtSvid(s_workload1, [s_workload1.Id]).Token;
        string badJwt = jwt2[..jwt2.LastIndexOf('.')] + jwt1[jwt1.LastIndexOf('.')..];
        JwtSvidException e = await Assert.ThrowsAsync<JwtSvidException>(() => JwtSvidParser.ParseAsync(badJwt, s_source, [s_workload1.Id]));
        e.Message.Should().Be("JWT token validation failed");
        e.InnerException?.Message?.Should().StartWith("IDX10511: Signature validation failed.");
    }

    [Fact]
    public async Task TestParseFailsIfBundleNotFound()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example.org");
        SpiffeId workload1 = SpiffeId.FromPath(td1, "/workload1");
        SpiffeId workload2 = SpiffeId.FromPath(td1, "/workload2");
        CA ca1 = CA.Create(td1);

        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        CA ca2 = CA.Create(td2);

        IJwtBundleSource source = new JwtBundleSet(new() { { td2, ca2.JwtBundle() } });
        string jwt = ca1.CreateJwtSvid(workload1, [workload2.Id]).Token;
        await Assert.ThrowsAsync<BundleNotFoundException>(() => JwtSvidParser.ParseAsync(jwt, source, [workload2.Id]));
    }

    [Fact]
    public async Task TestParseFailsIfKeyNotFound()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example.org");
        SpiffeId workload1 = SpiffeId.FromPath(td1, "/workload1");
        SpiffeId workload2 = SpiffeId.FromPath(td1, "/workload2");
        CA ca1 = CA.Create(td1);

        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        CA ca2 = CA.Create(td2);

        IJwtBundleSource source = new JwtBundleSet(new() { { td1, ca2.JwtBundle() } });
        string jwt = ca1.CreateJwtSvid(workload1, [workload2.Id]).Token;
        await Assert.ThrowsAsync<JwtSvidException>(() => JwtSvidParser.ParseAsync(jwt, source, [workload2.Id]));
    }
}
