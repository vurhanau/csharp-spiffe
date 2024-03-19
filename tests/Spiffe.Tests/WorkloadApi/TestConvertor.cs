using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using Spiffe.WorkloadApi;

namespace Spiffe.Tests.WorkloadApi;

public class TestConvertor
{
    [Fact]
    public void TestParseX509BundleSet()
    {
        // Parse valid bundle set
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        using X509Certificate2 cert1 = Certificates.FirstFromPemFile("TestData/X509/good-leaf-only.pem");
        byte[] cert1Raw = cert1.RawData;
        byte[] cert2Raw = Certificates.Concat(cert1, cert1);

        X509BundlesResponse r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFrom(cert1.RawData));
        r.Bundles.Add(td2.Name, ByteString.CopyFrom(cert2Raw));

        X509BundleSet bs = Convertor.ParseX509BundleSet(r);

        bs.Bundles.Should().HaveCount(2);
        bs.Bundles.Should().ContainKey(td1);
        bs.Bundles.Should().ContainKey(td2);
        bs.GetX509Bundle(td1).TrustDomain.Should().Be(td1);
        bs.GetX509Bundle(td2).TrustDomain.Should().Be(td2);

        X509Certificate2Collection b1 = bs.GetX509Bundle(td1).X509Authorities;
        b1.Should().HaveCount(1);
        b1[0].RawData.Should().Equal(cert1Raw);

        X509Certificate2Collection b2 = bs.GetX509Bundle(td2).X509Authorities;
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
        byte[][] cert1 = Certificates.GetCertBytes("TestData/X509/good-leaf-and-intermediate.pem");
        using ECDsa key1 = Certificates.GetEcdsaFromPemFile("TestData/X509/key-pkcs8-ecdsa.pem");
        byte[] bundle1 = Certificates.Concat(cert1[1], cert1[0]);
        string hint1 = "internal1";

        SpiffeId id2 = SpiffeId.FromString("spiffe://example2.org/workload2");
        byte[] cert2 = Certificates.GetCertBytes("TestData/X509/good-leaf-only.pem")[0];
        byte[] key2 = Certificates.GetRsaBytesFromPemFile("TestData/X509/key-pkcs8-rsa.pem");
        byte[] bundle2 = Certificates.Concat(cert2, cert2);
        string hint2 = "internal2";

        TrustDomain federatedTd = TrustDomain.FromString("spiffe://federated.org");
        byte[] federatedBundle = Certificates.Concat(cert1[0], cert2);

        X509SVIDResponse r = new();
        X509SVID s1 = new()
        {
            SpiffeId = id1.Id,
            Bundle = ByteString.CopyFrom(bundle1),
            X509Svid = ByteString.CopyFrom(Certificates.Concat(cert1)),
            X509SvidKey = ByteString.CopyFrom(key1.ExportPkcs8PrivateKey()),
            Hint = hint1,
        };
        X509SVID s2 = new()
        {
            SpiffeId = id2.Id,
            Bundle = ByteString.CopyFrom(bundle2),
            X509Svid = ByteString.CopyFrom(Certificates.Concat(cert2)),
            X509SvidKey = ByteString.CopyFrom(key2),
            Hint = hint2,
        };
        r.Svids.Add(s1);
        r.Svids.Add(s2);
        r.FederatedBundles.Add(federatedTd.Name, ByteString.CopyFrom(federatedBundle));

        X509Context x509Context = Convertor.ParseX509Context(r);

        // Verify bundles
        X509BundleSet b = x509Context.X509Bundles;
        b.Bundles.Should().HaveCount(3);
        b.Bundles.Should().ContainKeys(id1.TrustDomain, id2.TrustDomain, federatedTd);
        X509Bundle b1 = b.GetX509Bundle(id1.TrustDomain);
        b1.TrustDomain.Should().Be(id1.TrustDomain);
        b1.X509Authorities.Should().HaveCount(2);
        b1.X509Authorities[0].RawData.Should().Equal(cert1[1]);
        b1.X509Authorities[1].RawData.Should().Equal(cert1[0]);

        X509Bundle b2 = b.GetX509Bundle(id2.TrustDomain);
        b2.TrustDomain.Should().Be(id2.TrustDomain);
        b2.X509Authorities.Should().HaveCount(2);
        b2.X509Authorities[0].RawData.Should().Equal(cert2);
        b2.X509Authorities[1].RawData.Should().Equal(cert2);

        X509Bundle fb = b.GetX509Bundle(federatedTd);
        fb.TrustDomain.Should().Be(federatedTd);
        fb.X509Authorities.Should().HaveCount(2);
        fb.X509Authorities[0].RawData.Should().Equal(cert1[0]);
        fb.X509Authorities[1].RawData.Should().Equal(cert2);

        // Verify SVIDs
        x509Context.X509Svids.Should().HaveCount(2);
        void VerifyFirstSvid()
        {
            X509Svid svid1 = x509Context.X509Svids[0];
            svid1.Id.Should().Be(id1);
            svid1.Hint.Should().Be(hint1);
            svid1.Certificates.Should().HaveCount(2); // leaf + intermediate
            svid1.Certificates[0].RawData.Should().Equal(cert1[0]);
            svid1.Certificates[0].HasPrivateKey.Should().BeTrue();
            ECDsa actualKey = svid1.Certificates[0].GetECDsaPrivateKey()!;
            VerifyEcdsa(actualKey, key1);
            svid1.Certificates[1].RawData.Should().Equal(cert1[1]);
        }

        VerifyFirstSvid();

        X509Svid svid2 = x509Context.X509Svids[1];
        svid2.Id.Should().Be(id2);
        svid2.Hint.Should().Be(hint2);
        svid2.Certificates.Should().ContainSingle();
        svid2.Certificates[0].RawData.Should().Equal(cert2);
        svid2.Certificates[0].GetRSAPrivateKey()!.ExportPkcs8PrivateKey().Should().Equal(key2);

        // Parse 2 SVIDs with the same hint - should return just first
        r.Svids[1] = new X509SVID(s2)
        {
            Hint = hint1,
        };
        x509Context = Convertor.ParseX509Context(r);
        x509Context.X509Svids.Should().ContainSingle();
        VerifyFirstSvid();

        // Parse 2 SVIDs from the same trust domain
        var s3 = new X509SVID(s2)
        {
            SpiffeId = SpiffeId.FromPath(id1.TrustDomain, id2.Path).Id,
        };
        r.Svids[1] = s3;
        x509Context = Convertor.ParseX509Context(r);
        x509Context.X509Bundles.Bundles.Should().HaveCount(2);

        // Parse null
        Action nullSvidResponse = () => Convertor.ParseX509Context(null);
        nullSvidResponse.Should().Throw<ArgumentNullException>();

        // No SVIDs
        r.Svids.Clear();
        x509Context = Convertor.ParseX509Context(r);
        x509Context.X509Svids.Should().BeEmpty();

        // 2 SVIDs with empty hints
        r.Svids.Add(new X509SVID(s1) { Hint = string.Empty, });
        r.Svids.Add(new X509SVID(s2) { Hint = string.Empty, });
        x509Context = Convertor.ParseX509Context(r);
        x509Context.X509Svids.Should().HaveCount(2);
        x509Context.X509Svids[0].Id.Should().Be(SpiffeId.FromString(s1.SpiffeId));
        x509Context.X509Svids[1].Id.Should().Be(SpiffeId.FromString(s2.SpiffeId));
        x509Context.X509Svids[0].Hint.Should().Be(string.Empty);
        x509Context.X509Svids[1].Hint.Should().Be(string.Empty);
    }

    [Fact]
    public async Task TestParseJwtBundleSet()
    {
        // Parse valid bundle set
        TrustDomain td1 = TrustDomain.FromString("spiffe://example1.org");
        string jwksJson1 = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_1.json");
        JsonWebKeySet jwks1 = JsonWebKeySet.Create(jwksJson1);
        byte[] jwksBytes1 = Encoding.UTF8.GetBytes(jwksJson1);

        TrustDomain td2 = TrustDomain.FromString("spiffe://example2.org");
        string jwksJson2 = await File.ReadAllTextAsync("TestData/Jwt/jwks_valid_2.json");
        JsonWebKeySet jwks2 = JsonWebKeySet.Create(jwksJson2);
        byte[] jwksBytes2 = Encoding.UTF8.GetBytes(jwksJson2);

        JWTBundlesResponse r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFrom(jwksBytes1));
        r.Bundles.Add(td2.Name, ByteString.CopyFrom(jwksBytes2));

        JwtBundleSet bs = Convertor.ParseJwtBundleSet(r);

        bs.Bundles.Should().HaveCount(2);
        bs.Bundles.Should().ContainKey(td1);
        bs.Bundles.Should().ContainKey(td2);
        bs.GetJwtBundle(td1).TrustDomain.Should().Be(td1);
        bs.GetJwtBundle(td2).TrustDomain.Should().Be(td2);

        void VerifyJwtBundle(TrustDomain td, JsonWebKeySet jwks)
        {
            Dictionary<string, JsonWebKey> b = bs.GetJwtBundle(td).JwtAuthorities;
            b.Should().HaveCount(jwks.Keys.Count);
            foreach (JsonWebKey k in jwks.Keys)
            {
                Keys.EqualJwk(b[k.KeyId], k).Should().BeTrue();
            }
        }

        VerifyJwtBundle(td1, jwks1);
        VerifyJwtBundle(td2, jwks2);

        // Parse empty bundle set
        r = new();
        bs = Convertor.ParseJwtBundleSet(r);
        bs.Bundles.Should().BeEmpty();

        // Parse malformed bundle set with a malformed trust domain name
        r = new();
        r.Bundles.Add("$#_=malformed-domain", ByteString.CopyFrom(jwksBytes1));
        Action malformedTrustDomainName = () => Convertor.ParseJwtBundleSet(r);
        malformedTrustDomainName.Should().Throw<Exception>();

        // Parse malformed bundle set with a malformed bundle
        r = new();
        r.Bundles.Add(td1.Name, ByteString.CopyFromUtf8("malformed"));
        Action malformedCert = () => Convertor.ParseJwtBundleSet(r);
        malformedCert.Should().Throw<Exception>();

        // Parse null bundle set
        Action nullBundleResponse = () => Convertor.ParseJwtBundleSet(null);
        nullBundleResponse.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestParseJwtSvid()
    {
        using ECDsa signingKey = Keys.CreateEC256Key();
        DateTime expiry = DateTime.Now.AddHours(1);
        string workload1 = "spiffe://example1.org/workload1";
        string workload2 = "spiffe://example2.org/workload2";
        string hint1 = "internal1";
        string hint2 = "internal2";
        IEnumerable<Claim> claims1 = Jwt.GetClaims(workload1, [workload2], expiry);
        IEnumerable<Claim> claims2 = Jwt.GetClaims(workload1, [workload2], expiry);
        string jwt1 = Jwt.Generate(claims1, signingKey);
        string jwt2 = Jwt.Generate(claims2, signingKey);

        JWTSVIDResponse r = new();
        JWTSVID s0 = new()
        {
            SpiffeId = workload1,
            Svid = jwt1,
            Hint = hint1,
        };
        JWTSVID s1 = new()
        {
            SpiffeId = workload1,
            Svid = jwt2,
            Hint = hint2,
        };
        r.Svids.Add(s0);
        r.Svids.Add(s1);

        // Parse 2 SVIDs
        List<JwtSvid> svids = Convertor.ParseJwtSvids(r, [workload2]);
        svids.Should().NotBeNull();
        svids.Should().HaveCount(2);
        JwtSvid expected0 = new(
            token: jwt1,
            id: SpiffeId.FromString(workload1),
            audience: [workload2],
            expiry: expiry,
            claims: claims1.ToDictionary(c => c.Type, c => c.Value),
            hint: hint1);
        JwtSvid expected1 = new(
            token: jwt2,
            id: SpiffeId.FromString(workload1),
            audience: [workload2],
            expiry: expiry,
            claims: claims2.ToDictionary(c => c.Type, c => c.Value),
            hint: hint2);
        svids.Should().Equal(expected0, expected1);

        // Parse 2 SVIDs with the same hint -> 1 SVID
        r = new();
        r.Svids.Add(s0);
        r.Svids.Add(new JWTSVID(s1)
        {
            Hint = hint1,
        });
        svids = Convertor.ParseJwtSvids(r, [workload2]);
        svids.Should().NotBeNull();
        svids.Should().ContainSingle();
        svids.Should().Equal(expected0);

        // Null response
        Action f = () => Convertor.ParseJwtSvids(null, []);
        f.Should().Throw<ArgumentNullException>();

        // No SVIDs in response
        JWTSVIDResponse noSvids = new(r);
        noSvids.Svids.Clear();
        f = () => Convertor.ParseJwtSvids(noSvids, null);
        f.Should().Throw<JwtSvidException>().WithMessage("There were no SVIDs in the response");

        // 2 SVIDs with empty hints
        r = new();
        r.Svids.Add(new JWTSVID(s0) { Hint = string.Empty, });
        r.Svids.Add(new JWTSVID(s1) { Hint = string.Empty, });
        svids = Convertor.ParseJwtSvids(r, [workload2]);
        svids.Should().HaveCount(2);
        svids[0].Id.Should().Be(SpiffeId.FromString(s0.SpiffeId));
        svids[1].Id.Should().Be(SpiffeId.FromString(s1.SpiffeId));
        svids[0].Hint.Should().Be(string.Empty);
        svids[1].Hint.Should().Be(string.Empty);
    }

    // Verify ECDsa by verifying signatures, not by PKCS binary:
    // https://github.com/dotnet/runtime/issues/97932
    private static void VerifyEcdsa(ECDsa e1, ECDsa e2)
    {
        ReadOnlySpan<byte> data = "hello"u8;
        HashAlgorithmName ha = HashAlgorithmName.SHA384;
        byte[] s1 = e1.SignData(data, ha);
        byte[] s2 = e2.SignData(data, ha);
        e1.VerifyData(data, s2, ha).Should().BeTrue();
        e2.VerifyData(data, s1, ha).Should().BeTrue();
    }
}
