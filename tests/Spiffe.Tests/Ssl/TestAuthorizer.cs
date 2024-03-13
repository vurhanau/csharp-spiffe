using FluentAssertions;
using Spiffe.Id;
using Spiffe.Ssl;

namespace Spiffe.Tests.Ssl;

public class TestAuthorizer
{
    [Fact]
    public void TestAuthorizers()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        SpiffeId W1() => SpiffeId.FromPath(td1, "/workload1");
        SpiffeId W2() => SpiffeId.FromPath(td2, "/workload2");

        IAuthorizer authorizer = Authorizers.AuthorizeAny();
        authorizer.Authorize(W1()).Should().BeTrue();
        authorizer.Authorize(null).Should().BeTrue();

        authorizer = Authorizers.AuthorizeId(W1());
        authorizer.Authorize(W1()).Should().BeTrue();
        authorizer.Authorize(W2()).Should().BeFalse();
        authorizer.Authorize(null).Should().BeFalse();
        Action f = () => Authorizers.AuthorizeId(null);
        f.Should().Throw<ArgumentNullException>();

        Authorizers.AuthorizeOneOf([W1()]).Authorize(W1()).Should().BeTrue();
        Authorizers.AuthorizeOneOf([W1(), W2()]).Authorize(W1()).Should().BeTrue();
        Authorizers.AuthorizeOneOf([W1(), W2()]).Authorize(W2()).Should().BeTrue();
        Authorizers.AuthorizeOneOf([W1()]).Authorize(W2()).Should().BeFalse();
        Authorizers.AuthorizeOneOf([]).Authorize(W1()).Should().BeFalse();
        Authorizers.AuthorizeOneOf([]).Authorize(null).Should().BeFalse();
        f = () => Authorizers.AuthorizeOneOf(null);
        f.Should().Throw<ArgumentNullException>();

        authorizer = Authorizers.AuthorizeMemberOf(td1);
        authorizer.Authorize(W1()).Should().BeTrue();
        authorizer.Authorize(W2()).Should().BeFalse();
        authorizer.Authorize(null).Should().BeFalse();
        f = () => Authorizers.AuthorizeMemberOf(null);
        f.Should().Throw<ArgumentNullException>();

        authorizer = Authorizers.AuthorizeIf(id => id.Path.EndsWith('1'));
        authorizer.Authorize(W1()).Should().BeTrue();
        authorizer.Authorize(W2()).Should().BeFalse();
        f = () => authorizer.Authorize(null);
        f.Should().Throw<NullReferenceException>();
        f = () => Authorizers.AuthorizeIf(null);
        f.Should().Throw<ArgumentNullException>();
    }
}
