using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Tests.Helper;
using Spiffe.Util;
using static Spiffe.Tests.Helper.Certificates;

namespace Spiffe.Tests.Util;

public class TestCrypto
{
    [Fact]
    public void TestGetCertificateWithRsaPrivateKey()
    {
        string certPath = "TestData/X509/good-leaf-only.pem";
        string keyPath = "TestData/X509/key-pkcs8-rsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] rsaPrivateKey = expected.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = FirstFromPemFile(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, rsaPrivateKey.AsSpan());

        actual.RawData.Should().Equal(expected.RawData);
        actual.GetRSAPrivateKey()!.ExportPkcs8PrivateKey().Should().Equal(rsaPrivateKey);
    }

    [Fact]
    public void TestGetCertificateWithEcdsaPrivateKey()
    {
        string certPath = "TestData/X509/good-leaf-and-intermediate.pem";
        string keyPath = "TestData/X509/key-pkcs8-ecdsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] ecdsaPrivateKey = expected.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = FirstFromPemFile(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, ecdsaPrivateKey.AsSpan());

        actual.RawData.Should().Equal(expected.RawData);
        actual.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey().Should().Equal(ecdsaPrivateKey);
    }

    [Fact]
    public void TestGetCertificateWithInvalidPrivateKey()
    {
        string certPath = "TestData/X509/good-leaf-only.pem";

        using X509Certificate2 cert = FirstFromPemFile(certPath);
        byte[] invalidPrivateKey = "not-DER-encoded"u8.ToArray();
        Action a = () => Crypto.GetCertificateWithPrivateKey(cert, invalidPrivateKey.AsSpan());
        a.Should().Throw<Exception>();
    }

    [Fact]
    public void TestParseGood()
    {
        string certAndKeyPath = "TestData/X509/good-cert-and-key.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certAndKeyPath);
        X509Certificate2Collection actual = Crypto.ParseCertificates(expected.RawData);
        actual.Should().ContainSingle();
        actual[0].RawData.Should().Equal(expected.RawData);

        actual[0].Dispose();
    }

    [Fact]
    public void TestParseGoodLeafAndIntermediate()
    {
        string leafAndIntermediatePath = "TestData/X509/good-leaf-and-intermediate.pem";
        X509Certificate2Collection expected = [];
        expected.ImportFromPemFile(leafAndIntermediatePath);

        byte[] c0 = expected[0].RawData;
        byte[] c1 = expected[1].RawData;
        byte[] concat = new byte[c0.Length + c1.Length];
        c0.CopyTo(concat, 0);
        c1.CopyTo(concat, c0.Length);

        X509Certificate2Collection actual = Crypto.ParseCertificates(concat);
        actual.Should().HaveCount(2);
        actual[0].RawData.Should().Equal(c0);
        actual[1].RawData.Should().Equal(c1);

        actual[0].Dispose();
        actual[1].Dispose();
    }

    [Fact]
    public void TestParseCorruptCertificate()
    {
        byte[] corrupt = "not-DER-encoded"u8.ToArray();
        Action a = () => Crypto.ParseCertificates(corrupt);
        a.Should().Throw<Exception>();
    }
}
