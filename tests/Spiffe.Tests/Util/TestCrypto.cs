using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Util;

namespace Spiffe.Tests.Util;

public class TestCrypto
{
    [Fact]
    public void TestGetCertificateWithRsaPrivateKey()
    {
        string certPath = "Util/TestData/good-leaf-only.pem";
        string keyPath = "Util/TestData/key-pkcs8-rsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] rsaPrivateKey = expected.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = LoadCert(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, rsaPrivateKey.AsSpan());

        expected.RawData.Should().Equal(actual.RawData);
    }

    [Fact]
    public void TestGetCertificateWithEcdsaPrivateKey()
    {
        string certPath = "Util/TestData/good-leaf-and-intermediate.pem";
        string keyPath = "Util/TestData/key-pkcs8-ecdsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] ecdsaPrivateKey = expected.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = LoadCert(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, ecdsaPrivateKey.AsSpan());

        expected.RawData.Should().Equal(actual.RawData);
    }

    [Fact]
    public void TestGetCertificateWithInvalidPrivateKey()
    {
        string certPath = "Util/TestData/good-leaf-only.pem";

        using X509Certificate2 cert = LoadCert(certPath);
        byte[] invalidPrivateKey = "not-DER-encoded"u8.ToArray();
        Action a = () => Crypto.GetCertificateWithPrivateKey(cert, invalidPrivateKey.AsSpan());
        a.Should().Throw<Exception>();
    }

    [Fact]
    public void TestParseGood()
    {
        string certAndKeyPath = "Util/TestData/good-cert-and-key.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certAndKeyPath);
        X509Certificate2Collection actual = Crypto.ParseCertificates(expected.RawData);
        actual.Should().ContainSingle();
        actual[0].RawData.Should().Equal(expected.RawData);

        actual[0].Dispose();
    }

    [Fact]
    public void TestParseGoodLeafAndIntermediate()
    {
        string leafAndIntermediatePath = "Util/TestData/good-leaf-and-intermediate.pem";
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

    private static X509Certificate2 LoadCert(string pemFile)
    {
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c[0];
    }
}
