using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Spiffe.Client;

internal static class Extensions
{
    private static readonly JsonSerializerOptions s_jsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    internal static string ToString(X509Certificate2? certificate)
    {
        return JsonSerializer.Serialize(new
        {
            certificate?.Subject,
            certificate?.Issuer,
            certificate?.Thumbprint,
            certificate?.HasPrivateKey,
            certificate?.NotBefore,
            certificate?.NotAfter,
            UrlName = certificate?.GetNameInfo(X509NameType.UrlName, false),
            KeyUsage = GetKeyUsage(certificate),
        },
        s_jsonOpts);
    }

    internal static string ToString(X509Certificate2Collection certificates)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Certificate collection: {certificates.Count} item(s)");
        for (int i = 0; i < certificates.Count; i++)
        {
            sb.AppendLine($"[{i + 1}]:");
            sb.AppendLine(ToString(certificates[i]));
        }

        return sb.ToString();
    }

    private static string? GetKeyUsage(X509Certificate2? certificate)
    {
        return certificate?.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages.ToString();
    }
}
