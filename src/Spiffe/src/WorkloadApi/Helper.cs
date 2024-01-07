using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;

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

    public static X509Svid ToSvidModel(X509SVID svid ) => new(SpiffeId.FromString(svid.SpiffeId),
                                                              CreateCertificate(svid),
                                                              CreateChain(svid.Bundle),
                                                              svid.Hint);

    public static X509Bundle ToBundleModel(TrustDomain td, X509SVID svid) => new(td, CreateChain(svid.Bundle));

    internal static X509Chain CreateChain(ByteString bundle)
    {
        byte[] bytes = bundle.ToByteArray();
        var rootCertificate = new X509Certificate2(bytes);
        var chain = new X509Chain();
        X509ChainPolicy chainPolicy = chain.ChainPolicy;
        chainPolicy.CustomTrustStore.Add(rootCertificate);
        chainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        chainPolicy.DisableCertificateDownloads = true;

        return chain;
    }

    internal static X509Certificate2 CreateCertificate(X509SVID svid)
    {
        var publicKey = svid.X509Svid.ToByteArray();
        var privateKey = svid.X509SvidKey.ToByteArray();
        using var cert = new X509Certificate2(publicKey);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKey, out int _);

        return cert.CopyWithPrivateKey(ecdsa);
    }
}
