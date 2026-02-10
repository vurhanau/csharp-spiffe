using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Moq;
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

        // Verify both keys represent the same cryptographic material by signing and verifying
        using RSA expectedRsa = expected.GetRSAPrivateKey()!;
        using RSA actualRsa = actual.GetRSAPrivateKey()!;
        byte[] testData = "test-data"u8.ToArray();
        byte[] signature = expectedRsa.SignData(testData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        actualRsa.VerifyData(testData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();
    }

    [Fact]
    public void TestGetCertificateWithEcdsaPrivateKey()
    {
        string certPath = "TestData/X509/good-leaf-and-intermediate.pem";
        string keyPath = "TestData/X509/key-pkcs8-ecdsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        using X509Certificate2 tmp = FirstFromPemFile(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, expected.GetECDsaPrivateKey()!.ExportPkcs8PrivateKey());

        actual.RawData.Should().Equal(expected.RawData);

        // Verify both keys represent the same cryptographic material by signing and verifying
        using ECDsa expectedEcdsa = expected.GetECDsaPrivateKey()!;
        using ECDsa actualEcdsa = actual.GetECDsaPrivateKey()!;
        byte[] testData = "test-data"u8.ToArray();
        byte[] signature = expectedEcdsa.SignData(testData, HashAlgorithmName.SHA256);
        actualEcdsa.VerifyData(testData, signature, HashAlgorithmName.SHA256).Should().BeTrue();
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
    public void TestGetCertificateWithUnsupportedKeyAlgorithm()
    {
        static void AssertFail(string ka)
        {
            var c = new Mock<X509Certificate2>();
            c.Setup(c => c.GetKeyAlgorithm()).Returns(ka);
            Action a = () => Crypto.GetCertificateWithPrivateKey(c.Object, []);
            a.Should().Throw<Exception>().WithMessage($"Unsupported key algorithm: '{ka}'");
        }

        AssertFail("1.3.101.112"); // ED25519
        AssertFail("foo");
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

    [Fact]
    public async Task TestGetCertificateWithPrivateKeySchannelAccess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Load certificate and private key using Crypto.GetCertificateWithPrivateKey
        string certPath = "TestData/X509/good-leaf-only.pem";
        string keyPath = "TestData/X509/key-pkcs8-rsa.pem";
        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] rsaPrivateKey = expected.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = FirstFromPemFile(certPath);
        using X509Certificate2 actual = Crypto.GetCertificateWithPrivateKey(tmp, rsaPrivateKey.AsSpan());

        actual.HasPrivateKey.Should().BeTrue();

        // Create listener to accept a TLS connection using the certificate
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        Exception exception = null;
        bool handshakeSucceeded = false;

        var serverTask = Task.Run(async () =>
        {
            try
            {
                using TcpClient client = await listener.AcceptTcpClientAsync();
                using NetworkStream stream = client.GetStream();
                using SslStream sslStream = new(stream, false);

                await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = actual,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                });

                handshakeSucceeded = sslStream.IsAuthenticated;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        // Create client to connect and complete TLS handshake
        var clientTask = Task.Run(async () =>
        {
            await Task.Delay(50);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            using NetworkStream stream = client.GetStream();
#pragma warning disable CA5359 // Test code accepting self-signed certificate
            using var sslStream = new SslStream(stream, false, (_, _, _, _) => true);
#pragma warning restore CA5359
            await sslStream.AuthenticateAsClientAsync("localhost");
        });

        var timeout = Task.Delay(5000);
        await Task.WhenAny(Task.WhenAll(serverTask, clientTask), timeout);

        listener.Stop();

        if (exception != null)
        {
            Assert.Fail($"Schannel could not access private key: {exception.GetType().Name}: {exception.Message}");
        }

        handshakeSucceeded.Should().BeTrue("Schannel should complete TLS handshake using the private key");
    }

    [Fact]
    public async Task TestGetCertificateWithPrivateKeyCleanupOnDispose()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: Create certificate with private key
        string certPath = "TestData/X509/good-leaf-only.pem";
        string keyPath = "TestData/X509/key-pkcs8-rsa.pem";

        using X509Certificate2 expected = X509Certificate2.CreateFromPemFile(certPath, keyPath);
        byte[] rsaPrivateKey = expected.GetRSAPrivateKey()!.ExportPkcs8PrivateKey();
        using X509Certificate2 tmp = FirstFromPemFile(certPath);
        X509Certificate2 cert = Crypto.GetCertificateWithPrivateKey(tmp, rsaPrivateKey.AsSpan());

        cert.HasPrivateKey.Should().BeTrue();

        // Get the private key to verify it exists
        using RSA privateKey = cert.GetRSAPrivateKey();
        privateKey.Should().NotBeNull("Private key should be accessible before cleanup");

        // Act: Clean up the private key
        Crypto.DeletePrivateKey(cert);

        // Attempt to use key after cleanup - sign data to verify it fails
        byte[] data = "test data"u8.ToArray();
        Action a = () => cert.GetRSAPrivateKey().SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        a.Should().Throw<CryptographicException>("Private key should not be usable after cleanup");
    }
}
