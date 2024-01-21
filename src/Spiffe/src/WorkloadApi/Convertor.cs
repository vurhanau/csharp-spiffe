using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
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
        foreach (X509SVID svid in response.Svids)
        {
            X509Svid model = ToSvidModel(svid);
            svids.Add(model);

            TrustDomain td = model.SpiffeId!.TrustDomain!;
            X509Bundle bundle = ToBundleModel(td, svid);
            bundles.Add(td, bundle);
        }

        return new(svids, new X509BundleSet(bundles));
    }

    public static X509BundleSet ParseX509BundleSet(X509BundlesResponse response)
    {
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        foreach (KeyValuePair<string, ByteString> bundle in response.Bundles)
        {
            TrustDomain td = TrustDomain.FromString(bundle.Key);
            X509Certificate2Collection authorities = Crypto.ParseCertificates(bundle.Value.Span);
            bundles[td] = new X509Bundle(td, authorities);
        }

        return new(bundles);
    }

    public static X509Svid ToSvidModel(X509SVID svid)
    {
        SpiffeId spiffeId = SpiffeId.FromString(svid.SpiffeId);
        X509Certificate2Collection certificates = Crypto.ParseCertificates(svid.X509Svid.Span);
        certificates[0] = Crypto.GetCertificateWithPrivateKey(certificates[0], svid.X509SvidKey.Span);

        return new(spiffeId, certificates, svid.Hint);
    }

    public static X509Bundle ToBundleModel(TrustDomain td, X509SVID svid)
    {
        X509Certificate2Collection authorities = Crypto.ParseCertificates(svid.Bundle.Span);
        return new X509Bundle(td, authorities);
    }
}
