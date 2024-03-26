using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Claims = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Spiffe.Tests.Common;

internal static class Jwt
{
    internal static List<Claim> GetClaims(string sub, IEnumerable<string> aud, DateTime expiry)
    {
        DateTime now = DateTime.UtcNow;
        string iat = ToNumericDate(now);
        string exp = ToNumericDate(expiry);
        string iss = "FAKECA";
        List<Claim> claims = [
            new(Claims.Sub, sub),
            new(Claims.Iss, iss),
            new(Claims.Iat, iat),
            new(Claims.Exp, exp),
        ];
        claims.AddRange(aud.Select(a => new Claim(Claims.Aud, a)));
        return claims;
    }

    internal static string Generate(IEnumerable<Claim> claims, ECDsa signingKey, string kid = null)
    {
        ECDsaSecurityKey securityKey = new(signingKey)
        {
            KeyId = kid,
        };
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.EcdsaSha256);
        JwtHeader header = new(credentials);
        JwtPayload payload = new(claims);
        JwtSecurityToken jwt = new(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    internal static string ToNumericDate(DateTime d)
    {
        return new DateTimeOffset(d).ToUnixTimeSeconds().ToString();
    }
}
