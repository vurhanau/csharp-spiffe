using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Tests.Helper;

internal static class Certificates
{
    internal static X509Certificate2 FirstFromPemFile(string pemFile)
    {
        // X509Certificate2.CreateFromPemFile fails to load plain cert from PEM.
        // It expects a private key to be a part of PEM if key file is unspecified.
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c[0];
    }

    internal static byte[][] GetCertBytes(string pemFile)
    {
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c.Select(ci => ci.RawData).ToArray();
    }

    internal static ECDsa GetEcdsaFromPemFile(string pemFile)
    {
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(File.ReadAllText(pemFile));
        return ecdsa;
    }

    internal static byte[] GetRsaBytesFromPemFile(string pemFile)
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(pemFile));
        return rsa.ExportPkcs8PrivateKey();
    }

    internal static byte[] Concat(params X509Certificate2[] certs) => Concat(certs.Select(c => c.RawData).ToArray());

    internal static byte[] Concat(params byte[][] certs)
    {
        byte[] b = new byte[certs.Sum(c => c.Length)];
        int offset = 0;
        foreach (byte[] cert in certs)
        {
            byte[] chunk = cert;
            Buffer.BlockCopy(chunk, 0, b, offset, chunk.Length);
            offset += chunk.Length;
        }

        return b;
    }
}
