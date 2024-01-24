using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Tests.Util;

internal static class CertLoader
{
    internal static X509Certificate2 FromPemFile(string pemFile)
    {
        X509Certificate2Collection c = [];
        c.ImportFromPemFile(pemFile);
        return c[0];
    }
}
