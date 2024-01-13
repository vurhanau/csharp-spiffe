using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Spiffe.Util;
using static Tests.Spiffe.Util.TestData;
using CertWithPrivateKeyCase = (
    string Name,
    string KeyPath,
    string CertsPath,
    System.Func<System.Security.Cryptography.X509Certificates.X509Certificate2, byte[]> PrivateKeyFunc,
    byte[] RawCert,
    byte[] RawKey,
    bool Err
);

namespace Tests.Spiffe.Util;

public class TestCrypto
{
    private const string KeyRsa = "Util/TestData/key-pkcs8-rsa.pem";

    private const string CertRsa = "Util/TestData/good-leaf-only.pem";

    private const string KeyEcdsa = "Util/TestData/key-pkcs8-ecdsa.pem";

    private const string CertAndKey = "Util/TestData/good-cert-and-key.pem";

    private const string CertEcdsa = "Util/TestData/good-leaf-and-intermediate.pem";

    private const string CorruptedCert = "Util/TestData/corrupt-cert.pem";

    [Fact]
    public void TestGetCertificateWithPrivateKey()
    {
        CertWithPrivateKeyCase[] testCases =
        [
            (
                Name: "certificate and RSA key must match",
                KeyPath: KeyRsa,
                CertsPath: CertRsa,
                PrivateKeyFunc: cert => cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKey(),
                RawCert: LoadRawCert(CertRsa),
                RawKey: LoadRawRsaKey(KeyRsa),
                Err: false
            ),
#if !OS_WINDOWS
            // TODO: This test fails on Windows.
            // Signatures produced by these keys also don't match.
            (
                Name: "certificate and ECDSA key must match",
                KeyPath: KeyEcdsa,
                CertsPath: CertEcdsa,
                PrivateKeyFunc: cert => cert.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey(),
                RawCert: LoadRawCert(CertEcdsa),
                RawKey: LoadRawEcdsaKey(KeyEcdsa),
                Err: false
            ),
#endif
            (
                Name: "certificate bytes are not DER encoded must fail",
                KeyPath: string.Empty,
                CertsPath: string.Empty,
                PrivateKeyFunc: cert => [],
                RawCert: Encoding.ASCII.GetBytes("not-DER-encoded"),
                RawKey: LoadRawRsaKey(KeyRsa),
                Err: true
            ),
            (
                Name: "key bytes are not DER encoded must fail",
                KeyPath: string.Empty,
                CertsPath: string.Empty,
                PrivateKeyFunc: cert => [],
                RawCert: LoadRawCert(CertRsa),
                RawKey: Encoding.ASCII.GetBytes("not-DER-encoded"),
                Err: true
            ),
        ];

        foreach (var test in testCases)
        {
            if (test.Err)
            {
                Action a = () => Crypto.GetCertificateWithPrivateKey(test.RawCert, test.RawKey);
                a.Should().Throw<Exception>(test.Name);
            }
            else
            {
                X509Certificate2 cert = Crypto.GetCertificateWithPrivateKey(test.RawCert, test.RawKey);
                cert.Should().NotBeNull(test.Name);
                cert.RawData.Should().Equal(test.RawCert, test.Name);
                byte[] privateKey = test.PrivateKeyFunc(cert);
                privateKey.Should().Equal(test.RawKey, test.Name);
            }
        }
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
