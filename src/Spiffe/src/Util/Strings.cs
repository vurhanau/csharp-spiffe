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
    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonFormatter s_protoJson = new(JsonFormatter.Settings.Default.WithIndentation());

    /// <summary>
    /// Gets X509 context string representation.
    /// </summary>
    /// <param name="x509Context">Context</param>
    /// <param name="verbose">Set if certificate output should contain detailed information.</param>
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

    /// <summary>
    /// Gets X509 SVID string representation.
    /// </summary>
    /// <param name="x509Svid">X509 SVID</param>
    /// <param name="verbose">Set if certificate output should contain detailed information.</param>
    public static string ToString(X509Svid x509Svid, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Spiffe ID: {x509Svid.Id?.Id}");
        if (!string.IsNullOrEmpty(x509Svid.Hint))
        {
            sb.AppendLine($"Hint: {x509Svid.Hint}");
        }

        sb.AppendLine(ToString(x509Svid.Certificates, verbose));
        return sb.ToString();
    }

    /// <summary>
    /// Gets X509 bundle set string representation.
    /// </summary>
    /// <param name="x509BundleSet">X509 bundle set</param>
    /// <param name="verbose">Set if certificate output should contain detailed information.</param>
    public static string ToString(X509BundleSet x509BundleSet, bool verbose = false)
    {
        StringBuilder sb = new();
        sb.AppendLine($"Received {x509BundleSet.Bundles.Count} bundle(s)");
        foreach ((TrustDomain td, X509Bundle bundle) in x509BundleSet.Bundles)
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

    internal static string ToString(IMessage proto)
    {
        return s_protoJson.Format(proto);
    }

    private static string? GetKeyUsage(X509Certificate2? certificate)
    {
        return certificate?.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault()?.KeyUsages.ToString();
    }
}
