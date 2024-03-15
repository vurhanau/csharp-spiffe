using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;

namespace Spiffe.Tests.Svid.X509;

public class TestX509Svid
{
    [Fact]
    public void TestInvalidConstructorArgs()
    {
        SpiffeId id = SpiffeId.FromString("spiffe://example.org/myworkload");
        Action f = () => new X509Svid(id, null, string.Empty);
        f.Should().Throw<ArgumentException>("Certificates collection must be non-empty");
        f = () => new X509Svid(id, [], string.Empty);
        f.Should().Throw<ArgumentException>("Certificates collection must be non-empty");

        X509Certificate2Collection c = [];
        c.ImportFromPemFile("TestData/X509/good-leaf-only.pem");
        f = () => new X509Svid(id, c, string.Empty);
        f.Should().Throw<ArgumentException>("Leaf certificate must have a private key");
    }
}
