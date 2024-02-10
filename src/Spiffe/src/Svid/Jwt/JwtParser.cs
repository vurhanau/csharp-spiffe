using System.IdentityModel.Tokens.Jwt;
using Spiffe.Id;

namespace Spiffe.Svid.Jwt;

public static class JwtParser
{
    /// <summary>
    /// ParseAndValidate parses and validates a JWT-SVID token and returns the
    /// JWT-SVID. The JWT-SVID signature is verified using the JWT bundle source.
    /// </summary>
    public static JwtSvid ParseAndValidate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);

        return null;
    }

    private static JwtSvid Parse(string token,
                                 List<string> audience,
                                 Func<JwtSecurityToken, TrustDomain, Dictionary<string, object>> validate)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwt = handler.ReadJwtToken(token);

        // handler.ValidateToken() TODO:!
        ValidateTokenAlgorithm(jwt);

        JwtPayload p = jwt.Payload;
        if (string.IsNullOrEmpty(p.Sub))
        {
            throw new JwtSvidException("Token missing subject claim");
        }

        if (p.Expiration == null)
        {
            throw new JwtSvidException("Token missing exp claim");
        }

        SpiffeId spiffeId;
        try
        {
            spiffeId = SpiffeId.FromString(p.Sub);
        }
        catch
        {
            throw new JwtSvidException($"Token has an invalid subject claim: '{p.Sub}'");
        }

        validate(jwt, spiffeId.TrustDomain);

        return null;
    }

    /// <summary>
    /// Json web token have only one header, and it is signed for a supported algorithm
    /// </summary>
    private static void ValidateTokenAlgorithm(JwtSecurityToken jwt)
    {
        int headers = jwt.Header.Count; // TOOD: check
        if (headers != 1)
        {
            throw new ArgumentException($"expected a single token header; got {headers}");
        }

        string alg = jwt.Header.Alg;
        bool ok = alg == JwtAlgorithm.RS256 || alg == JwtAlgorithm.RS384 || alg == JwtAlgorithm.RS512 ||
                  alg == JwtAlgorithm.ES256 || alg == JwtAlgorithm.ES384 || alg == JwtAlgorithm.ES512 ||
                  alg == JwtAlgorithm.PS256 || alg == JwtAlgorithm.PS384 || alg == JwtAlgorithm.PS512;

        if (!ok)
        {
            throw new JwtSvidException($"Unsupported token signature algorithm '{alg}'");
        }
    }
}
