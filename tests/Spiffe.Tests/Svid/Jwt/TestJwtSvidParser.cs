using System.Security.Claims;
using FluentAssertions;
using Spiffe.Bundle.Jwt;
using Spiffe.Error;
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

    [Fact]
    public async Task TestParse()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string jwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);

        JwtSvid parsed = await JwtSvidParser.Parse(jwt, source, [s_workload2.Id]);
        parsed.Should().Be(new JwtSvid(
            token: jwt,
            id: s_workload1,
            audience: [s_workload2.Id],
            expiry: now.AddHours(1),
            claims: claims.ToDictionary(c => c.Type, c => c.Value),
            hint: string.Empty));
    }

    [Fact]
    public async Task TestParseFailsIfMissingSubject()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>("Token missing sub claim");
    }

    [Fact]
    public async Task TestParseFailsIfMissingExp()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>("Token missing exp claim");
    }

    [Fact]
    public async Task TestParseFailsIfSubjectInvalid()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        string sub = "invalid spiffe id";
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, sub),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>($"Token has an invalid subject claim: '{sub}'");
    }

    [Fact]
    public async Task TestParseFailsIfAudienceInvalid()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        SpiffeId workload3 = SpiffeId.FromPath(s_td, "/workload3");
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [workload3.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>($"Expected audience in {s_workload2.Id} (audience={workload3.Id})");
    }

    [Fact]
    public async Task TestParseFailsIfValidFromInvalid()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Nbf, J.ToNumericDate(now.AddDays(1))),
            new(Claims.Iat, J.ToNumericDate(now)),
            new(Claims.Exp, J.ToNumericDate(now.AddHours(1))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>("Validation failed, token not valid yet (nbf)");
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
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>("Validation failed, token is expired (exp)");
    }

    [Fact]
    public async Task TestParseFailsIfIssuedAtInvalid()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        DateTime now = DateTime.UtcNow;
        List<Claim> claims = [
            new(Claims.Iss, "FAKECA"),
            new(Claims.Sub, s_workload1.Id),
            new(Claims.Iat, J.ToNumericDate(now.AddYears(1))),
            new(Claims.Exp, J.ToNumericDate(now.AddYears(2))),
            new(Claims.Aud, s_workload2.Id),
        ];
        string badJwt = J.Generate(claims, ca.JwtKey, ca.JwtKid);
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>("Validation field, token issued in the future (iat)");
    }

    [Fact]
    public async Task TestParseWithInvalidSignature()
    {
        CA ca = CA.Create(s_td);
        IJwtBundleSource source = new JwtBundleSet(new() { { s_td, ca.JwtBundle() } });
        string jwt1 = ca.CreateJwtSvid(s_workload1, [s_workload2.Id]).Token;
        string jwt2 = ca.CreateJwtSvid(s_workload1, [s_workload1.Id]).Token;
        string badJwt = jwt2[..jwt2.LastIndexOf('.')] + jwt1[jwt1.LastIndexOf('.')..];
        Func<Task> fn = async () => await JwtSvidParser.Parse(badJwt, source, [s_workload2.Id]);
        await fn.Should().ThrowAsync<JwtSvidException>();
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
        await Assert.ThrowsAsync<BundleNotFoundException>(() => JwtSvidParser.Parse(jwt, source, [workload2.Id]));
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
        await Assert.ThrowsAsync<JwtSvidException>(() => JwtSvidParser.Parse(jwt, source, [workload2.Id]));
    }
}
