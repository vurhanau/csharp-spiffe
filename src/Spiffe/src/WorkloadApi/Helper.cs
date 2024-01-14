using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.Util;

namespace Spiffe.WorkloadApi;

internal static class Helper
{
    public static X509Context ToX509Context(X509SVIDResponse response)
    {
        List<X509Svid> svids = [];
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        foreach (X509SVID svid in response.Svids)
        {
            X509Svid model = ToSvidModel(svid);
            svids.Add(model);

            TrustDomain td = model.SpiffeId!.TrustDomain!;
            bundles.Add(td, ToBundleModel(td, svid));
        }

        return new(svids, new X509BundleSet(bundles));
    }

    public static X509BundleSet ToX509BundleSet(X509BundlesResponse response)
    {
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        foreach (KeyValuePair<string, ByteString> bundle in response.Bundles)
        {
            TrustDomain td = TrustDomain.FromString(bundle.Key);
            X509Chain chain = CreateChain(bundle.Value);
            bundles[td] = new X509Bundle(td, chain);
        }

        return new(bundles);
    }

    public static X509Svid ToSvidModel(X509SVID svid)
    {
        SpiffeId id = SpiffeId.FromString(svid.SpiffeId);
        X509Certificate2 root = new(svid.Bundle.Span);
        X509Certificate2Collection intermediateAndLeaf = Crypto.ParseCertificates(svid.X509Svid.Span);
        X509Certificate2 leaf = Crypto.GetCertificateWithPrivateKey(svid.X509Svid.Span, svid.X509SvidKey.Span);

        return new(id, root, intermediateAndLeaf, leaf, svid.Hint);
    }

    public static X509Bundle ToBundleModel(TrustDomain td, X509SVID svid) => new(td, CreateChain(svid.Bundle));
}
