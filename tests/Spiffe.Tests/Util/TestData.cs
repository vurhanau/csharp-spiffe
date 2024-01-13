using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Tests.Util;

internal static class TestData
{
    internal static X509Certificate2 LoadCert(string pemFile)
    {
        return LoadCerts(pemFile)[0];
    }

    internal static byte[] LoadRawCert(string pemFile)
    {
        return LoadCert(pemFile).RawData;
    }

    internal static X509Certificate2Collection LoadCerts(string pemFile)
    {
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c;
    }

    internal static byte[] LoadRawRsaKey(string pemFile)
    {
        return LoadRsaKey(pemFile).ExportPkcs8PrivateKey();
    }

    internal static byte[] LoadRawEcdsaKey(string pemFile)
    {
        return LoadEcdsaKey(pemFile).ExportPkcs8PrivateKey();
    }

    private static RSA LoadRsaKey(string pemFile)
    {
        RSA rsa = RSA.Create();
        string pem = File.ReadAllText(pemFile);
        rsa.ImportFromPem(pem);
        return rsa;
    }

    private static ECDsa LoadEcdsaKey(string pemFile)
    {
        ECDsa ecdsa = ECDsa.Create();
        string pem = File.ReadAllText(pemFile);
        ecdsa.ImportFromPem(pem);
        return ecdsa;
    }
}
