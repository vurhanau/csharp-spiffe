using System.ComponentModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Tests.Util;

internal static class CertUtil
{
    /// <summary>
    /// <see cref="X509Certificate2.CreateFromPemFile"/> fails to load certs from PEM:
    /// - Intermediate + leaf
    /// - Leaf only
    /// TODO: why?
    /// </summary>
    internal static X509Certificate2 FirstFromPemFile(string pemFile)
    {
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c[0];
    }

    internal static byte[] GetCertBytes(string pemFile)
    {
        return FirstFromPemFile(pemFile).RawData;
    }

    // internal static byte[] GetEcdsaBytesFromPemFile(string pemFile)
    // {
    //     using ECDsa ecdsa = ECDsa.Create();
    //     ecdsa.ImportFromPem(File.ReadAllText(pemFile));
    //     return ecdsa.ExportPkcs8PrivateKey();
    // }

    // internal static byte[] GetRsaBytesFromPemFile(string pemFile)
    // {
    //     using RSA rsa = RSA.Create();
    //     rsa.ImportFromPem(File.ReadAllText(pemFile));
    //     return rsa.ExportPkcs8PrivateKey();
    // }

    internal static byte[] Concat(params X509Certificate2[] certs)
    {
        return Concat(certs.Select(c => c.RawData).ToArray());
    }

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
