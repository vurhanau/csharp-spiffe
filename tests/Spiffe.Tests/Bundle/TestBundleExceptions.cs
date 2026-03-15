using FluentAssertions;
using Spiffe.Bundle;
using Spiffe.Bundle.Jwt;
using Spiffe.Svid.Jwt;

namespace Spiffe.Tests.Bundle;

public class TestBundleExceptions
{
    [Fact]
    public void TestBundleNotFoundException()
    {
        string message = "Bundle not found for trust domain example.org";
        BundleNotFoundException ex = new(message);
        ex.Message.Should().Be(message);
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void TestJwtBundleException()
    {
        string message = "Unable to parse JWKS";
        Exception inner = new InvalidOperationException("parse error");
        JwtBundleException ex = new(message, inner);
        ex.Message.Should().Be(message);
        ex.InnerException.Should().Be(inner);
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void TestJwtSvidException()
    {
        string message = "Token missing sub claim";
        JwtSvidException ex = new(message);
        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeNull();

        Exception inner = new InvalidOperationException("cause");
        JwtSvidException exWithInner = new(message, inner);
        exWithInner.Message.Should().Be(message);
        exWithInner.InnerException.Should().Be(inner);
    }
}
