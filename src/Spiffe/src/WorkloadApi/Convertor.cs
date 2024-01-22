using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Util;

namespace Spiffe.WorkloadApi;

internal static class Convertor
{
    public static X509Context ParseX509Context(X509SVIDResponse response)
    {
        List<X509Svid> svids = [];
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        HashSet<string> hints = [];
        foreach (X509SVID svid in response.Svids)
        {
            // In the event of more than one X509SVID message with the same hint value set, then the first message in the
            // list SHOULD be selected.
            if (!string.IsNullOrEmpty(svid.Hint))
            {
                if (hints.Contains(svid.Hint))
                {
                    continue;
                }

                hints.Add(svid.Hint);
            }

            X509Svid model = ParseSvid(svid);
            svids.Add(model);

            TrustDomain td = model.SpiffeId!.TrustDomain!;
            X509Bundle bundle = ParseBundle(td, svid.Bundle);
            bundles.Add(td, bundle);
        }

        ParseBundles(response.FederatedBundles, bundles);

        return new(svids, new X509BundleSet(bundles));
    }

    public static X509BundleSet ParseX509BundleSet(X509BundlesResponse response)
    {
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        ParseBundles(response.Bundles, bundles);

        return new(bundles);
    }

    public static X509Svid ParseSvid(X509SVID svid)
    {
        SpiffeId spiffeId = SpiffeId.FromString(svid.SpiffeId);
        X509Certificate2Collection certificates = Crypto.ParseCertificates(svid.X509Svid.Span);
        certificates[0] = Crypto.GetCertificateWithPrivateKey(certificates[0], svid.X509SvidKey.Span);

        return new(spiffeId, certificates, svid.Hint);
    }

    public static X509Bundle ParseBundle(TrustDomain td, ByteString bundle)
    {
        X509Certificate2Collection authorities = Crypto.ParseCertificates(bundle.Span);
        return new X509Bundle(td, authorities);
    }

    private static void ParseBundles(MapField<string, ByteString> src, Dictionary<TrustDomain, X509Bundle> dest)
    {
        foreach (KeyValuePair<string, ByteString> bundle in src)
        {
            TrustDomain td = TrustDomain.FromString(bundle.Key);
            dest[td] = ParseBundle(td, bundle.Value);
        }
    }
}
