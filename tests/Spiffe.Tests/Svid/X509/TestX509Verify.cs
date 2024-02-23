using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Tests.Util;

namespace Spiffe.Tests.Svid.X509;

public class TestX509Verify
{
    [Fact]
    public void TestVerifyPass()
    {
        TrustDomain td = TrustDomain.FromString("domain1.test");
        CA ca1 = CA.Create(td);
        CA ca2 = ca1.ChildCA();
        IX509BundleSource bundleSource = new TestX509BundleSource(ca1.X509Bundle());
        X509Certificate2Collection certs = ca2.CreateX509Svid(SpiffeId.FromPath(td, "/workload")).Certificates;
        X509Certificate2 leaf = certs[0];
        X509Certificate2Collection intermediates = [..certs.Skip(1)];

        bool ok = X509Verify.Verify(leaf, intermediates, bundleSource);
        ok.Should().BeTrue();
    }

    [Fact]
    public void TestVerifyFails()
    {
        TrustDomain td = TrustDomain.FromString("domain1.test");
        CA ca0 = CA.Create(td);
        CA ca1 = CA.Create(td);
        CA ca2 = ca1.ChildCA();
        IX509BundleSource bundleSource = new TestX509BundleSource(ca0.X509Bundle());
        X509Certificate2Collection certs = ca2.CreateX509Svid(SpiffeId.FromPath(td, "/workload")).Certificates;
        X509Certificate2 leaf = certs[0];
        X509Certificate2Collection intermediates = [..certs.Skip(1)];

        // leaf ---> intermediate -x-> root
        bool ok = X509Verify.Verify(leaf, intermediates, bundleSource);
        ok.Should().BeFalse();

        // leaf -x-> intermediate ---> root
        CA ca3 = ca1.ChildCA();
        intermediates = [ca3.Cert!];
        ok = X509Verify.Verify(leaf, intermediates, bundleSource);
        ok.Should().BeFalse();
    }

    [Fact]
    public void TestVerifyThrows()
    {
        TrustDomain td = TrustDomain.FromString("domain1.test");
        CA ca = CA.Create(td);
        IX509BundleSource bundleSource = new TestX509BundleSource(ca.X509Bundle());
        X509Certificate2Collection certs = ca.CreateX509Svid(SpiffeId.FromPath(td, "/workload")).Certificates;
        X509Certificate2 leaf = certs[0];
        X509Certificate2Collection intermediates = [..certs.Skip(1)];

        // Leaf CA
        Func<bool> f = () => X509Verify.Verify(ca.Cert!, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>("Leaf certificate with CA flag set to true");

        // Leaf with key usage KeyCertSign
        X509Certificate2 leafKeyCertSign = CA.CreateCACertificate(null, csr =>
        {
            csr.CertificateExtensions.Clear();
            csr.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign,
                    true));
        });
        f = () => X509Verify.Verify(leafKeyCertSign, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>("Leaf certificate with KeyCertSign key usage");

        // Leaf with key usage CrlSign
        X509Certificate2 leafCrlSign = CA.CreateCACertificate(null, csr =>
        {
            csr.CertificateExtensions.Clear();
            csr.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.CrlSign,
                    true));
        });
        f = () => X509Verify.Verify(leafCrlSign, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>("Leaf certificate with KeyCrlSign key usage");

        // Null params
        f = () => X509Verify.Verify(null, intermediates, bundleSource);
        f.Should().Throw<ArgumentNullException>();

        f = () => X509Verify.Verify(leaf, null, bundleSource);
        f.Should().Throw<ArgumentNullException>();

        f = () => X509Verify.Verify(leaf, intermediates, null);
        f.Should().Throw<ArgumentNullException>();
    }

    internal class TestX509BundleSource(X509Bundle bundle) : IX509BundleSource
    {
        public X509Bundle GetX509Bundle(TrustDomain trustDomain) => bundle;
    }
}
