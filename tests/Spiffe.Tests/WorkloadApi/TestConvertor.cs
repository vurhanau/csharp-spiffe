using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Google.Protobuf;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Tests.Util;
using Spiffe.WorkloadApi;

namespace Spiffe.Test.WorkloadApi;

public class TestConvertor
{
    [Fact]
    public void TestParseX509BundleSet()
    {
        // Parse valid bundle set
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

        // Parse empty bundle set
        r = new();
        bs = Convertor.ParseX509BundleSet(r);
        bs.Bundles.Should().BeEmpty();

        // Parse malformed bundle set with a malformed trust domain name
        r = new();
        r.Bundles.Add("$#_=malformed-domain", ByteString.CopyFrom(cert1Raw));
        Action malformedTrustDomainName = () => Convertor.ParseX509BundleSet(r);
        malformedTrustDomainName.Should().Throw<Exception>();

        // Parse malformed bundle set with a malformed bundle
        r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFromUtf8("malformed"));
        Action malformedCert = () => Convertor.ParseX509BundleSet(r);
        malformedCert.Should().Throw<Exception>();

        // Parse null bundle set
        Action nullBundleResponse = () => Convertor.ParseX509BundleSet(null);
        nullBundleResponse.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestParseX509Context()
    {
        // Parse 2 SVIDs and 1 federated bundle
        SpiffeId id1 = SpiffeId.FromString("spiffe://example1.org/workload1");
        byte[][] cert1 = CertUtil.GetCertBytes("TestData/good-leaf-and-intermediate.pem");
        byte[] key1 = CertUtil.GetEcdsaBytesFromPemFile("TestData/key-pkcs8-ecdsa.pem");
        byte[] bundle1 = CertUtil.Concat(cert1[1], cert1[0]);
        string hint1 = "internal1";

        SpiffeId id2 = SpiffeId.FromString("spiffe://example2.org/workload2");
        byte[] cert2 = CertUtil.GetCertBytes("TestData/good-leaf-only.pem")[0];
        byte[] key2 = CertUtil.GetRsaBytesFromPemFile("TestData/key-pkcs8-rsa.pem");
        byte[] bundle2 = CertUtil.Concat(cert2, cert2);
        string hint2 = "internal2";

        TrustDomain federatedTd = TrustDomain.FromString("spiffe://federated.org");
        byte[] federatedBundle = CertUtil.Concat(cert1[0], cert2);

        X509SVIDResponse r = new();
        r.Svids.Add(new X509SVID()
        {
            SpiffeId = id1.Id,
            Bundle = ByteString.CopyFrom(bundle1),
            X509Svid = ByteString.CopyFrom(CertUtil.Concat(cert1)),
            X509SvidKey = ByteString.CopyFrom(key1),
            Hint = hint1,
        });
        r.Svids.Add(new X509SVID()
        {
            SpiffeId = id2.Id,
            Bundle = ByteString.CopyFrom(bundle2),
            X509Svid = ByteString.CopyFrom(CertUtil.Concat(cert2)),
            X509SvidKey = ByteString.CopyFrom(key2),
            Hint = hint2,
        });
        r.FederatedBundles.Add(federatedTd.Name, ByteString.CopyFrom(federatedBundle));

        X509Context x509Context = Convertor.ParseX509Context(r);

        // Verify bundles
        X509BundleSet b = x509Context.X509Bundles;
        b.Bundles.Should().HaveCount(3);
        b.Bundles.Should().ContainKeys(id1.TrustDomain, id2.TrustDomain, federatedTd);
        X509Bundle b1 = b.GetBundleForTrustDomain(id1.TrustDomain);
        b1.TrustDomain.Should().Be(id1.TrustDomain);
        b1.X509Authorities.Should().HaveCount(2);
        b1.X509Authorities[0].RawData.Should().Equal(cert1[1]);
        b1.X509Authorities[1].RawData.Should().Equal(cert1[0]);

        X509Bundle b2 = b.GetBundleForTrustDomain(id2.TrustDomain);
        b2.TrustDomain.Should().Be(id2.TrustDomain);
        b2.X509Authorities.Should().HaveCount(2);
        b2.X509Authorities[0].RawData.Should().Equal(cert2);
        b2.X509Authorities[1].RawData.Should().Equal(cert2);

        X509Bundle fb = b.GetBundleForTrustDomain(federatedTd);
        fb.TrustDomain.Should().Be(federatedTd);
        fb.X509Authorities.Should().HaveCount(2);
        fb.X509Authorities[0].RawData.Should().Equal(cert1[0]);
        fb.X509Authorities[1].RawData.Should().Equal(cert2);

        // Verify SVIDs
        x509Context.X509Svids.Should().HaveCount(2);
        void VerifyFirstSvid()
        {
            X509Svid svid1 = x509Context.X509Svids[0];
            svid1.SpiffeId.Should().Be(id1);
            svid1.Hint.Should().Be(hint1);
            svid1.Certificates.Should().HaveCount(2); // leaf + intermediate
            svid1.Certificates[0].RawData.Should().Equal(cert1[0]);
            svid1.Certificates[0].HasPrivateKey.Should().BeTrue();
            svid1.Certificates[0].GetECDsaPrivateKey()!.ExportPkcs8PrivateKey().Should().Equal(key1);
            svid1.Certificates[1].RawData.Should().Equal(cert1[1]);
        }

        VerifyFirstSvid();

        X509Svid svid2 = x509Context.X509Svids[1];
        svid2.SpiffeId.Should().Be(id2);
        svid2.Hint.Should().Be(hint2);
        svid2.Certificates.Should().ContainSingle();
        svid2.Certificates[0].RawData.Should().Equal(cert2);
        svid2.Certificates[0].GetRSAPrivateKey()!.ExportPkcs8PrivateKey().Should().Equal(key2);

        // Parse 2 SVIDs with the same hint - should return just first
        r.Svids[1].Hint = hint1;
        x509Context = Convertor.ParseX509Context(r);
        x509Context.X509Svids.Should().ContainSingle();
        VerifyFirstSvid();

        // Parse null
        Action nullSvidResponse = () => Convertor.ParseX509Context(null);
        nullSvidResponse.Should().Throw<ArgumentNullException>();
    }
}
