using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Id;

namespace Spiffe.Bundle.Jwt;

/// <summary>
/// Parses JWT bundles.
/// </summary>
internal static class JwtBundleParser
{
    /// <summary>
    /// Parses a bundle from bytes. The data must be a standard RFC 7517 JWKS document.
    /// </summary>
    public static JwtBundle Parse(TrustDomain td, ReadOnlySpan<byte> bundleBytes)
    {
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

        Dictionary<string, X509Certificate2> d = [];
        for (int i = 0; i < jwks.Keys.Count; i++)
        {
            JsonWebKey key = jwks.Keys[i];
            try
            {
                if (key.X5c.Count == 0)
                {
                    continue;
                }

                byte[] b = Convert.FromBase64String(key.X5c[0]);
                d[key.Kid] = new X509Certificate2(b);
            }
            catch (Exception e)
            {
                throw new JwtBundleException($"Error adding authority {i} of JWKS", e);
            }
        }

        return new JwtBundle(td, d);
    }
}
