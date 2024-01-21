using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Util;

/// <summary>
/// String helper
/// </summary>
public static class Strings
{
    private static readonly JsonSerializerOptions s_jsonOpts = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    private static readonly JsonFormatter s_protoJson = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation());

    public static string ToString(X509Context x509Context, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine(ToString(x509Context.X509Bundles, verbose));

        List<X509Svid> svids = x509Context.X509Svids;
        sb.AppendLine($"SVIDs: {svids.Count} item(s)");
        foreach (X509Svid svid in svids)
        {
            sb.AppendLine(ToString(svid, verbose));
        }

        return sb.ToString();
    }

    public static string ToString(X509Svid svid, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Spiffe ID: {svid.SpiffeId?.Id}");
        if (!string.IsNullOrEmpty(svid.Hint))
        {
            sb.AppendLine($"Hint: {svid.Hint}");
        }

        sb.AppendLine(ToString(svid.Certificates, verbose));
        return sb.ToString();
    }

    public static string ToString(X509BundleSet set, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Received {set.Bundles.Count} bundle(s)");
        foreach ((TrustDomain td, X509Bundle bundle) in set.Bundles)
        {
            sb.AppendLine($"Trust domain: {td}");
            sb.AppendLine(ToString(bundle.X509Authorities, verbose));
        }

        return sb.ToString();
    }

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

    internal static string ToString(X509Certificate2Collection certificates, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Certificate collection: {certificates.Count} item(s)");
        for (int i = 0; i < certificates.Count; i++)
        {
            sb.AppendLine($"[{i + 1}]:");
            if (verbose)
            {
                sb.AppendLine(certificates[i].ToString(true));
            }
            else
            {
                sb.AppendLine(ToString(certificates[i]));
            }
        }

        return sb.ToString();
    }

    internal static string ToString(X509SVIDResponse x509SVIDResponse)
    {
        return s_protoJson.Format(x509SVIDResponse);
    }

    private static string? GetKeyUsage(X509Certificate2? certificate)
    {
        return certificate?.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages.ToString();
    }
}
