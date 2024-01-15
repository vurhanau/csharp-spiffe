using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Util;
using static Spiffe.Tests.Util.TestData;

namespace Spiffe.Tests.Util;

public class TestCrypto
{
    private const string CertAndKey = "Util/TestData/good-cert-and-key.pem";

    private const string CertEcdsa = "Util/TestData/good-leaf-and-intermediate.pem";

    private const string CorruptedCert = "Util/TestData/corrupt-cert.pem";

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

    // TODO: check windows
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
        byte[] expected = LoadCert(CertAndKey).RawData;
        void AssertParsed(X509Certificate2Collection actual)
        {
            actual.Should().ContainSingle();
            actual[0].RawData.Should().Equal(expected);
        }

        X509Certificate2Collection actual = Crypto.ParseCertificates(expected);
        AssertParsed(actual);

        actual = Crypto.ParseCertificates(expected.AsSpan());
        AssertParsed(actual);
    }

    [Fact]
    public void TestParseGoodLeafAndIntermediate()
    {
        X509Certificate2Collection expected = LoadCerts(CertEcdsa);
        byte[] c0 = expected[0].RawData;
        byte[] c1 = expected[1].RawData;
        byte[] concat = new byte[c0.Length + c1.Length];
        c0.CopyTo(concat, 0);
        c1.CopyTo(concat, c0.Length);

        void AssertParsed(X509Certificate2Collection actual)
        {
            actual.Should().HaveCount(2);
            actual[0].RawData.Should().Equal(c0);
            actual[1].RawData.Should().Equal(c1);
        }

        X509Certificate2Collection actual = Crypto.ParseCertificates(concat);
        AssertParsed(actual);

        actual = Crypto.ParseCertificates(concat.AsSpan());
        AssertParsed(actual);
    }

    [Fact]
    public void TestParseCorruptCertificate()
    {
        byte[] corrupt = File.ReadAllBytes(CorruptedCert);
        Action a = () => Crypto.ParseCertificates(corrupt);
        a.Should().Throw<Exception>();
    }
}
