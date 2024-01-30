using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Google.Protobuf;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Tests.Util;
using Spiffe.WorkloadApi;

namespace Spiffe.Test.WorkloadApi;

public class TestConvertor
{
    [Fact]
    public void TestParseX509BundleSet()
    {
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        using X509Certificate2 cert1 = CertUtil.FirstFromPemFile("TestData/good-leaf-only.pem");
        byte[] cert1Raw = cert1.RawData;
        byte[] cert2Raw = CertUtil.Concat(cert1, cert1);

        X509BundlesResponse r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFrom(cert1.RawData));
        r.Bundles.Add(td2.Name, ByteString.CopyFrom(cert2Raw));

        X509BundleSet bs = Convertor.ParseX509BundleSet(r);

        bs.Bundles.Should().HaveCount(2);
        bs.Bundles.Should().ContainKey(td1);
        bs.Bundles.Should().ContainKey(td2);
        bs.GetBundleForTrustDomain(td1).TrustDomain.Should().Be(td1);
        bs.GetBundleForTrustDomain(td2).TrustDomain.Should().Be(td2);

        X509Certificate2Collection b1 = bs.GetBundleForTrustDomain(td1).X509Authorities;
        b1.Should().HaveCount(1);
        b1[0].RawData.Should().Equal(cert1Raw);

        X509Certificate2Collection b2 = bs.GetBundleForTrustDomain(td2).X509Authorities;
        b2.Should().HaveCount(2);
        b2[0].RawData.Should().Equal(cert1Raw);
        b2[0].RawData.Should().Equal(cert1Raw);

        r = new();
        bs = Convertor.ParseX509BundleSet(r);
        bs.Bundles.Should().BeEmpty();

        r = new();
        r.Bundles.Add("$#_=malformed-domain", ByteString.CopyFrom(cert1Raw));
        Action malformedTrustDomainName = () => Convertor.ParseX509BundleSet(r);
        malformedTrustDomainName.Should().Throw<Exception>();

        r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFromUtf8("malformed"));
        Action malformedCert = () => Convertor.ParseX509BundleSet(r);
        malformedCert.Should().Throw<Exception>();

        Action nullBundleResponse = () => Convertor.ParseX509BundleSet(null);
        nullBundleResponse.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestParseX509Context()
    {
        // SpiffeId id1 = SpiffeId.FromString("spiffe://example1.org/workload1");
        // byte[] cert1 = CertUtil.GetCertBytes("TestData/good-leaf-and-intermediate.pem");
        // byte[] key1 = CertUtil.GetEcdsaBytesFromPemFile("TestData/key-pkcs8-ecdsa.pem");

        // SpiffeId id2 = SpiffeId.FromString("spiffe://example2.org/workload2");
        // byte[] cert2 = CertUtil.GetCertBytes("TestData/good-leaf-only.pem");
        // byte[] key2 = CertUtil.GetEcdsaBytesFromPemFile("TestData/key-pkcs8-rsa.pem");

        // byte[] bundle1 = CertUtil.Concat(cert1, cert2);
        // byte[] bundle2 = CertUtil.Concat(cert2, cert1);
        // TrustDomain federatedTd = TrustDomain.FromString("spiffe://federated.org");
        // byte[] federatedBundle = CertUtil.Concat(bundle1, bundle2);

        // X509SVIDResponse r = new();
        // r.Svids.Add(new X509SVID()
        // {
        //     SpiffeId = id1.Id,
        //     Bundle = ByteString.CopyFrom(bundle1),
        //     X509Svid = ByteString.CopyFrom(cert1),
        //     X509SvidKey = ByteString.CopyFrom(key1),
        //     Hint = "internal1",
        // });
        // r.Svids.Add(new X509SVID()
        // {
        //     SpiffeId = id2.Id,
        //     Bundle = ByteString.CopyFrom(bundle2),
        //     X509Svid = ByteString.CopyFrom(cert2),
        //     X509SvidKey = ByteString.CopyFrom(key2),
        //     Hint = "internal2",
        // });

        // r.FederatedBundles.Add(federatedTd.Name, ByteString.CopyFrom(federatedBundle));

        // X509Context x509Context = Convertor.ParseX509Context(r);
        // TODO: test verify context
        // TODO: test hint

        Action nullSvidResponse = () => Convertor.ParseX509Context(null);
        nullSvidResponse.Should().Throw<ArgumentNullException>();
    }
}
