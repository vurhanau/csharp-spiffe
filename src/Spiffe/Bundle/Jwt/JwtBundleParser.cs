using System.Text;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
///     Parses JWT bundles.
/// </summary>
internal static class JwtBundleParser
{
    /// <summary>
    ///     Parses a bundle from bytes. The data must be a standard RFC 7517 JWKS document.
    /// </summary>
    public static JwtBundle Parse(TrustDomain td, ReadOnlySpan<byte> bundleBytes)
    {
        _ = td ?? throw new ArgumentNullException(nameof(td));

        string json = Encoding.UTF8.GetString(bundleBytes);
        JsonWebKeySet jwks;
        try
        {
            jwks = JsonWebKeySet.Create(json);
        }
        catch (Exception e)
        {
            throw new JwtBundleException("Unable to parse JWKS", e);
        }

        Dictionary<string, JsonWebKey> d = [];
        foreach (JsonWebKey key in jwks.Keys)
        {
            d[key.Kid] = key;
        }

        return new JwtBundle(td, d);
    }
}
