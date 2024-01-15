using Spiffe.Bundle.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Client;

internal static class Printer
{
    internal static void Print(X509Context x509Context)
    {
        var svids = x509Context.X509Svids;
        if (svids == null)
        {
            Console.WriteLine("X509 context is empty");
            return;
        }

        Console.WriteLine($"Received {svids.Count} svid(s)");
        Console.WriteLine();
        foreach (var svid in svids)
        {
            PrintField("SPIFFE ID", svid.SpiffeId?.Id);
            if (!string.IsNullOrEmpty(svid.Hint))
            {
                PrintField("Hint", svid.Hint);
            }

            // TODO:
            // PrintField($"Certificate", svid.Certificates?.ToString(true), false);
            // PrintField("Bundle", svid.Chain?.ToDisplayString(), false);
        }
    }

    internal static void Print(X509BundleSet x509BundleSet)
    {
        if (x509BundleSet.Bundles == null)
        {
            Console.WriteLine("X509 bundle set is empty");
            return;
        }

        Console.WriteLine("[Bundles]");
        foreach (var tdBundle in x509BundleSet.Bundles)
        {
            Console.WriteLine($"Trust domain: {tdBundle.Key}");
            X509Bundle bundle = tdBundle.Value;
            Console.WriteLine($"X509 certificate chain:");

            // TODO:
            // Console.WriteLine(bundle.Chain?.ToDisplayString());
        }
    }

    private static void PrintField(string key, string? value, bool indent = true)
    {
        var tab = indent ? "  " : string.Empty;
        Console.WriteLine($"[{key}]");
        Console.WriteLine($"{tab}{value}");
        Console.WriteLine();
    }
}
