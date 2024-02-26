using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;

namespace Spiffe.Tests.Helper;

internal static class JwtBundles
{
    public static byte[] Serialize(JwtBundle bundle)
    {
        JsonWebKeySet jwks = new();
        foreach ((string _, JsonWebKey jwk) in bundle.JwtAuthorities)
        {
            jwks.Keys.Add(jwk);
        }

        string json = JsonSerializer.Serialize(jwks);
        return Encoding.UTF8.GetBytes(json);
    }
}
