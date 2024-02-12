using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;

namespace Spiffe.Svid.Jwt;

internal static class JwtSvidParser
{
    // Defines the default leeway for matching NotBefore/Expiry claims.
    private static readonly TimeSpan s_leeway = TimeSpan.FromMinutes(1);

    private static readonly JsonWebTokenHandler s_jsonHandler = new();

    /// <summary>
    /// ParseAndValidate parses and validates a JWT-SVID token and returns the
    /// JWT-SVID. The JWT-SVID signature is verified using the JWT bundle source.
    /// </summary>
    public static async Task<JwtSvid> Parse(string token, IJwtBundleSource bundleSource, List<string> audience)
    {
        return await Parse(token, audience, async (jwt, td) =>
        {
            string kid = jwt.Kid;
            if (string.IsNullOrEmpty(kid))
            {
                throw new JwtSvidException("Token header missing key id");
            }

            JwtBundle bundle = bundleSource.GetJwtBundle(td);
            bool ok = bundle.JwtAuthorities.ContainsKey(kid);
            if (!ok)
            {
                throw new JwtSvidException($"No JWT authority {kid} found for trust domain {td}");
            }

            X509Certificate2 authority = bundle.JwtAuthorities[kid];
            X509SecurityKey key = new(authority);
            TokenValidationResult result = await s_jsonHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
            {
                ClockSkew = s_leeway,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateTokenReplay = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ValidAudiences = audience,
            });

            if (!result.IsValid)
            {
                throw new JwtSvidException("JWT token validation failed");
            }
        });
    }

    /// <summary>
    /// Parses and validates a JWT-SVID token and returns the
    /// JWT-SVID. The JWT-SVID signature is not verified.
    /// </summary>
    internal static async Task<JwtSvid> ParseInsecure(string token, List<string> audience)
    {
        return await Parse(token, audience, (jwt, td) =>
        {
            // no signature verification
            return Task.CompletedTask;
        });
    }

    private static async Task<JwtSvid> Parse(string token,
                                             List<string> audience,
                                             Func<JsonWebToken, TrustDomain, Task> validateAsync)
    {
        JsonWebToken jwt = s_jsonHandler.ReadJsonWebToken(token);

        ValidateTokenAlgorithm(jwt);

        if (string.IsNullOrEmpty(jwt.Subject))
        {
            throw new JwtSvidException("Token missing sub claim");
        }

        if (jwt.ValidTo == DateTime.MinValue)
        {
            throw new JwtSvidException("Token missing exp claim");
        }

        SpiffeId spiffeId;
        try
        {
            spiffeId = SpiffeId.FromString(jwt.Subject);
        }
        catch
        {
            throw new JwtSvidException($"Token has an invalid subject claim: '{jwt.Subject}'");
        }

        await validateAsync(jwt, spiffeId.TrustDomain);

        ValidateLikeJose(jwt, audience);

        return new JwtSvid(
            id: spiffeId,
            audience: jwt.Audiences.ToList(),
            expiry: jwt.ValidTo,
            claims: jwt.Claims.ToDictionary(c => c.Type, c => c.Value),
            hint: string.Empty);
    }

    /// <summary>
    /// Checks claims in a token against expected values. A
    /// custom leeway may be specified for comparing time values. You may pass a
    /// zero value to check time values with no leeway, but you should note that
    /// numeric date values are rounded to the nearest second and sub-second
    /// precision is not supported.
    ///
    /// The leeway gives some extra time to the token from the server's
    /// point of view. That is, if the token is expired, ValidateWithLeeway
    /// will still accept the token for 'leeway' amount of time. This fails
    /// if you're using this function to check if a server will accept your
    /// token, because it will think the token is valid even after it
    /// expires. So if you're a client validating if the token is valid to
    /// be submitted to a server, use leeway lte 0, if you're a server
    /// validation a token, use leeway gte 0.
    /// <br/>
    /// See: <seealso href="https://github.com/go-jose/go-jose/blob/v3.0.1/jwt/validation.go#L78"/>
    /// </summary>
    private static void ValidateLikeJose(JsonWebToken jwt, List<string> expectedAudience)
    {
        // case-sensitive
        HashSet<string> s1 = new(jwt.Audiences, StringComparer.Ordinal);
        HashSet<string> s2 = new(expectedAudience, StringComparer.Ordinal);
        if (!s1.SetEquals(s2))
        {
            string actual = string.Join(", ", s1);
            string expected = string.Join(", ", s2);
            throw new JwtSvidException($"Expected audience in ${expected} (audience=${actual})");
        }

        DateTime now = DateTime.Now;
        if (jwt.ValidFrom > now.Add(s_leeway))
        {
            throw new JwtSvidException("Validation failed, token not valid yet (nbf)");
        }

        if (jwt.ValidTo < now.Add(-s_leeway))
        {
            throw new JwtSvidException("Validation failed, token is expired (exp)");
        }

        // IssuedAt is optional but cannot be in the future. This is not required by the RFC, but
        // something is misconfigured if this happens and we should not trust it.
        if (jwt.IssuedAt != DateTime.MinValue && now.Add(s_leeway) < jwt.IssuedAt)
        {
            throw new JwtSvidException("validation field, token issued in the future (iat)");
        }
    }

    /// <summary>
    /// Json web token have only one header, and it is signed for a supported algorithm
    /// </summary>
    private static void ValidateTokenAlgorithm(JsonWebToken jwt)
    {
        string alg = jwt.Alg;
        bool ok = alg == JwtAlgorithm.RS256 || alg == JwtAlgorithm.RS384 || alg == JwtAlgorithm.RS512 ||
                  alg == JwtAlgorithm.ES256 || alg == JwtAlgorithm.ES384 || alg == JwtAlgorithm.ES512 ||
                  alg == JwtAlgorithm.PS256 || alg == JwtAlgorithm.PS384 || alg == JwtAlgorithm.PS512;

        if (!ok)
        {
            throw new JwtSvidException($"Unsupported token signature algorithm '{alg}'");
        }
    }
}
