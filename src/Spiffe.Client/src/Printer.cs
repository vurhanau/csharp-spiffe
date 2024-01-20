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

        PrintBundles(x509Context.X509Bundles);

        Console.WriteLine($"Received {svids.Count} svid(s)");
        foreach (var svid in svids)
        {
            Console.WriteLine($"Spiffe ID: {svid.SpiffeId?.Id}");
            if (!string.IsNullOrEmpty(svid.Hint))
            {
                Console.WriteLine($"Hint: {svid.Hint}");
            }

            Console.WriteLine(Extensions.ToString(svid.Certificates));
        }
    }

    internal static void Print(X509BundleSet x509BundleSet)
    {
        if (x509BundleSet.Bundles == null)
        {
            Console.WriteLine("X509 bundle set is empty");
            return;
        }

        PrintBundles(x509BundleSet);
    }

    private static void PrintBundles(X509BundleSet x509BundleSet)
    {
        Console.WriteLine($"Received {x509BundleSet.Bundles.Count} bundle(s)");
        foreach (var bundle in from bundlePair in x509BundleSet.Bundles
                               select bundlePair.Value)
        {
            Console.WriteLine($"Trust domain: {bundle.TrustDomain}");
            Console.WriteLine(Extensions.ToString(bundle.X509Authorities));
        }
    }
}
