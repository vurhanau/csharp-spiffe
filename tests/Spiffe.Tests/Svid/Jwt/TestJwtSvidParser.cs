using FluentAssertions;
using Spiffe.Bundle.Jwt;
using Spiffe.Error;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Tests.Helper;

namespace Spiffe.Tests.Svid.Jwt;

public class TestJwtSvidParser
{
    [Fact]
    public async Task TestParse()
    {
        string domain = "spiffe://example.org";
        TrustDomain td = TrustDomain.FromString(domain);
        SpiffeId workload1 = SpiffeId.FromPath(td, "/workload1");
        SpiffeId workload2 = SpiffeId.FromPath(td, "/workload2");
        CA ca = CA.Create(td);
        IJwtBundleSource source = new JwtBundleSet(new() { { td, ca.JwtBundle() } });
        JwtSvid original = ca.CreateJwtSvid(workload1, [workload2.Id]);
        JwtSvid parsed = await JwtSvidParser.Parse(original.Token, source, [workload2.Id]);
        parsed.Should().Be(original);
    }

    [Fact]
    public async Task TestParseWithInvalidSignature()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        SpiffeId workload1 = SpiffeId.FromPath(td, "/workload1");
        SpiffeId workload2 = SpiffeId.FromPath(td, "/workload2");
        CA ca = CA.Create(td);
        IJwtBundleSource source = new JwtBundleSet(new() { { td, ca.JwtBundle() } });
        string jwt1 = ca.CreateJwtSvid(workload1, [workload2.Id]).Token;
        string jwt2 = ca.CreateJwtSvid(workload1, [workload1.Id]).Token;
        string badJwt = jwt2[..jwt2.LastIndexOf('.')] + jwt1[jwt1.LastIndexOf('.')..];
        await Assert.ThrowsAsync<JwtSvidException>(() => JwtSvidParser.Parse(badJwt, source, [workload2.Id]));
    }

    [Fact]
    public async Task TestParseFailsWhenBundleNotFound()
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
    public async Task TestParseFailsWhenKeyNotFound()
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
