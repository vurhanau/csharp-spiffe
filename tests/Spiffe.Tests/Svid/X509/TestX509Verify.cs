using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;

namespace Spiffe.Tests.Svid.X509;

public class TestX509Verify
{
    [Fact]
    public void TestVerifyPass()
    {
        TrustDomain td = TrustDomain.FromString("domain1.test");
        using CA ca1 = CA.Create(td);
        using CA ca2 = ca1.ChildCA();
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
        using CA ca0 = CA.Create(td);
        using CA ca1 = CA.Create(td);
        using CA ca2 = ca1.ChildCA();
        IX509BundleSource bundleSource = new TestX509BundleSource(ca0.X509Bundle());
        X509Certificate2Collection certs = ca2.CreateX509Svid(SpiffeId.FromPath(td, "/workload")).Certificates;
        X509Certificate2 leaf = certs[0];
        X509Certificate2Collection intermediates = [..certs.Skip(1)];

        // leaf ---> intermediate -x-> root
        bool ok = X509Verify.Verify(leaf, intermediates, bundleSource);
        ok.Should().BeFalse();

        // leaf -x-> intermediate ---> root
        using CA ca3 = ca1.ChildCA();
        intermediates = [ca3.Cert!];
        ok = X509Verify.Verify(leaf, intermediates, bundleSource);
        ok.Should().BeFalse();
    }

    [Fact]
    public void TestVerifyThrows()
    {
        TrustDomain td = TrustDomain.FromString("domain1.test");
        SpiffeId id = SpiffeId.FromPath(td, "/workload");
        using CA ca = CA.Create(td);
        IX509BundleSource bundleSource = new TestX509BundleSource(ca.X509Bundle());
        X509Certificate2Collection certs = ca.CreateX509Svid(id).Certificates;
        X509Certificate2 leaf = certs[0];
        X509Certificate2Collection intermediates = [..certs.Skip(1)];

        // Leaf CA
        using X509Certificate2 leafCA = CA.CreateX509Svid(ca.Cert, id, null, csr =>
        {
            Collection<X509Extension> exts = csr.CertificateExtensions;
            X509BasicConstraintsExtension ext = exts.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
            if (ext != null)
            {
                exts.Remove(ext);
            }

            exts.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true));
        });
        Func<bool> f = () => X509Verify.Verify(leafCA, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>().WithMessage("Leaf certificate with CA flag set to true");

        // Leaf with key usage KeyCertSign
        using X509Certificate2 leafKeyCertSign = CA.CreateX509Svid(ca.Cert, id, opts => opts.KeyUsage = X509KeyUsageFlags.KeyCertSign);
        f = () => X509Verify.Verify(leafKeyCertSign, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>().WithMessage("Leaf certificate with KeyCertSign key usage");

        // Leaf with key usage CrlSign
        using X509Certificate2 leafCrlSign = CA.CreateX509Svid(ca.Cert, id, opts => opts.KeyUsage = X509KeyUsageFlags.CrlSign);
        f = () => X509Verify.Verify(leafCrlSign, intermediates, bundleSource);
        f.Should().Throw<ArgumentException>().WithMessage("Leaf certificate with KeyCrlSign key usage");

        // Null params
        f = () => X509Verify.Verify(null, intermediates, bundleSource);
        f.Should().Throw<ArgumentNullException>();

        f = () => X509Verify.Verify(leaf, null, bundleSource);
        f.Should().Throw<ArgumentNullException>();

        f = () => X509Verify.Verify(leaf, intermediates, null);
        f.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestGetSpiffeIdFromCertificateFails()
    {
        Action f = () => X509Verify.GetSpiffeIdFromCertificate(null);
        f.Should().Throw<ArgumentNullException>();
    }

    internal class TestX509BundleSource(X509Bundle bundle) : IX509BundleSource
    {
        public X509Bundle GetX509Bundle(TrustDomain trustDomain) => bundle;
    }
}
