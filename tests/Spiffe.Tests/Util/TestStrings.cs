using FluentAssertions;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using Spiffe.Util;
using Spiffe.WorkloadApi;

namespace Spiffe.Tests.Util;

public class TestStrings
{
    [Fact]
    public void TestToString()
    {
        TrustDomain td = TrustDomain.FromString("spiffe://example.org");
        CA ca = CA.Create(td);
        SpiffeId w1 = SpiffeId.FromPath(td, "/workload1");
        SpiffeId w2 = SpiffeId.FromPath(td, "/workload2");
        X509Svid s1 = ca.CreateX509Svid(w1);
        X509Svid s2 = ca.CreateX509Svid(w2);
        X509BundleSet xbs = new(new Dictionary<TrustDomain, X509Bundle> { { td, ca.X509Bundle() } });
        JwtSvid j1 = ca.CreateJwtSvid(w1, [w2.Id], "hello");
        JwtBundleSet jbs = new(new Dictionary<TrustDomain, JwtBundle> { { td, ca.JwtBundle() } });
        X509Context ctx = new([s1, s2], xbs);

        string s = Strings.ToString(xbs);
        string sv = Strings.ToString(xbs, true);
        s.Should().Contain(td.Name);
        sv.Should().Contain(td.Name);
        sv.Length.Should().BeGreaterThanOrEqualTo(s.Length);

        s = Strings.ToString(ca.JwtBundle());
        s.Should().Contain(ca.JwtKid);

        s = Strings.ToString(jbs);
        sv = Strings.ToString(jbs, true);
        s.Should().Contain(td.Name);
        sv.Should().Contain(td.Name);
        sv.Length.Should().BeGreaterThanOrEqualTo(s.Length);

        s = Strings.ToString(j1);
        sv = Strings.ToString(j1, true);
        s.Should().Contain(w1.Id);
        sv.Should().Contain(w1.Id);
        sv.Length.Should().BeGreaterThanOrEqualTo(s.Length);

        s = Strings.ToString(s1);
        sv = Strings.ToString(s1, true);
        s.Should().Contain(w1.Id);
        sv.Should().Contain(w1.Id);
        sv.Length.Should().BeGreaterThanOrEqualTo(s.Length);

        s = Strings.ToString(ctx);
        sv = Strings.ToString(ctx, true);
        s.Should().Contain(w1.Id);
        sv.Should().Contain(w1.Id);
        s.Should().Contain(w2.Id);
        sv.Should().Contain(w2.Id);
        sv.Length.Should().BeGreaterThanOrEqualTo(s.Length);

        s = Strings.ToString(new X509SVID { SpiffeId = w1.Id });
        s.Should().Contain(w1.Id);

        s = Strings.ToString((X509Svid)null);
        s.Should().Be("null");
        s = Strings.ToString((JwtSvid)null);
        s.Should().Be("null");
    }
}
