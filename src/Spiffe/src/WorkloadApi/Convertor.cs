using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;
using Spiffe.Util;

namespace Spiffe.WorkloadApi;

internal static class Convertor
{
    public static X509Context ParseX509Context(X509SVIDResponse response)
    {
        _ = response ?? throw new ArgumentNullException(nameof(response));

        List<X509Svid> svids = [];
        Dictionary<TrustDomain, X509Bundle> bundles = [];
        HashSet<string> hints = [];
        foreach (X509SVID from in response.Svids ?? [])
        {
            // In the event of more than one X509SVID message with the same hint value set, then the first message in the
            // list SHOULD be selected.
            if (!string.IsNullOrEmpty(from.Hint) && !hints.Add(from.Hint))
            {
                continue;
            }

            X509Svid to = ParseX509Svid(from);
            svids.Add(to);

            TrustDomain td = to.Id.TrustDomain;
            X509Bundle bundle = ParseX509Bundle(td, from.Bundle);
            bundles[td] = bundle;
        }

        ParseX50Bundles(response.FederatedBundles, bundles);

        return new(svids, new X509BundleSet(bundles));
    }

    public static X509BundleSet ParseX509BundleSet(X509BundlesResponse response)
    {
        _ = response ?? throw new ArgumentNullException(nameof(response));

        Dictionary<TrustDomain, X509Bundle> bundles = [];
        ParseX50Bundles(response.Bundles, bundles);

        return new(bundles);
    }

    public static async Task<List<JwtSvid>> ParseJwtSvidsAsync(JWTSVIDResponse response, List<string> audience, int n = -1)
    {
        _ = response ?? throw new ArgumentNullException(nameof(response));
        if (response.Svids.Count == 0)
        {
            throw new JwtSvidException("There were no SVIDs in the response");
        }

        HashSet<string> hints = [];
        List<JwtSvid> svids = [];
        n = n == -1 ? response.Svids.Count : n;
        for (int i = 0; i < n; i++)
        {
            JWTSVID from = response.Svids[i];

            // In the event of more than one X509SVID message with the same hint value set, then the first message in the
            // list SHOULD be selected.
            if (!string.IsNullOrEmpty(from.Hint) && !hints.Add(from.Hint))
            {
                continue;
            }

            JwtSvid to = await JwtSvidParser.ParseInsecure(from.Svid, audience);
            to = new(
                id: to.Id,
                audience: to.Audience,
                expiry: to.Expiry,
                claims: to.Claims,
                hint: from.Hint);
            svids.Add(to);
        }

        return svids;
    }

    public static JwtBundleSet ParseJwtSvidBundles(JWTBundlesResponse response)
    {
        _ = response ?? throw new ArgumentNullException(nameof(response));

        Dictionary<TrustDomain, JwtBundle> bundles = [];
        foreach (KeyValuePair<string, ByteString> bundle in response.Bundles)
        {
            TrustDomain td = TrustDomain.FromString(bundle.Key);
            bundles[td] = JwtBundleParser.Parse(td, bundle.Value.Span);
        }

        return new JwtBundleSet(bundles);
    }

    private static X509Svid ParseX509Svid(X509SVID svid)
    {
        SpiffeId spiffeId = SpiffeId.FromString(svid.SpiffeId);
        X509Certificate2Collection certificates = Crypto.ParseCertificates(svid.X509Svid.Span);
        certificates[0] = Crypto.GetCertificateWithPrivateKey(certificates[0], svid.X509SvidKey.Span);

        return new(spiffeId, certificates, svid.Hint);
    }

    private static X509Bundle ParseX509Bundle(TrustDomain td, ByteString bundle)
    {
        X509Certificate2Collection authorities = Crypto.ParseCertificates(bundle.Span);
        return new X509Bundle(td, authorities);
    }

    private static void ParseX50Bundles(IEnumerable<KeyValuePair<string, ByteString>> src, Dictionary<TrustDomain, X509Bundle> dest)
    {
        foreach (KeyValuePair<string, ByteString> bundle in src ?? [])
        {
            TrustDomain td = TrustDomain.FromString(bundle.Key);
            dest[td] = ParseX509Bundle(td, bundle.Value);
        }
    }
}
