using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Error;
using Spiffe.Id;

namespace Spiffe.Tests.Bundle.X509;

public class TestX509BundleSet
{
    [Fact]
    public void TestGetBundleForTrustDomain()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        X509Bundle b1 = new(td1, []);
        X509Bundle b2 = new(td2, []);
        X509BundleSet bs = new(new Dictionary<TrustDomain, X509Bundle>
        {
            { td1, b1 },
            { td2, b2 },
        });
        bs.GetBundleForTrustDomain(td1).Should().Be(b1);
        bs.GetBundleForTrustDomain(td2).Should().Be(b2);

        Action notFound = () => bs.GetBundleForTrustDomain(TrustDomain.FromString("spiffe://example3.org"));
        notFound.Should().Throw<BundleNotFoundException>();

        Action nullCtor = () => new X509BundleSet(null);
        nullCtor.Should().Throw<ArgumentNullException>();
    }
}
