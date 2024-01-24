using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Tests.Util;

internal static class CertLoader
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
}
