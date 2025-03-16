using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;

namespace Spiffe.Svid.Jwt;

/// <summary>
///     Parses JWT SVID.
/// </summary>
public static class JwtSvidParser
{
    // Defines the default leeway for matching NotBefore/Expiry claims.
    private static readonly TimeSpan s_leeway = TimeSpan.FromMinutes(1);

    private static readonly JsonWebTokenHandler s_jsonHandler = new();

    /// <summary>
    ///     Parses and validates a JWT-SVID token and returns the JWT-SVID.
    ///     The JWT-SVID signature is verified using the JWT bundle source.
    /// </summary>
    public static async Task<JwtSvid> ParseAsync(string token, IJwtBundleSource bundleSource,
        IEnumerable<string> audience)
    {
        (SpiffeId Id, JsonWebToken Jwt) parsed = ParseValidate(token, audience);
        TokenValidationResult result =
            await ValidateSignature(parsed.Jwt, parsed.Id.TrustDomain, bundleSource, audience)
                .ConfigureAwait(false);
        if (!result.IsValid)
        {
            throw new JwtSvidException("JWT token validation failed", result.Exception);
        }

        return CreateJwtSvid(parsed.Id, parsed.Jwt);
    }

    /// <summary>
    ///     Parses and validates a JWT-SVID token and returns the
    ///     JWT-SVID. The JWT-SVID signature is not verified.
    /// </summary>
    public static JwtSvid ParseInsecure(string token, IEnumerable<string> audience)
    {
        (SpiffeId Id, JsonWebToken Jwt) parsed = ParseValidate(token, audience);
        return CreateJwtSvid(parsed.Id, parsed.Jwt);
    }

    /// <summary>
    ///     Parses and validate JWT-SVID.
    /// </summary>
    public static async Task<TokenValidationResult> ValidateAsync(string token,
        IJwtBundleSource bundleSource,
        IEnumerable<string> validAudience)
    {
        (SpiffeId Id, JsonWebToken Jwt) parsed = ParseValidate(token, validAudience);
        return await ValidateSignature(parsed.Jwt, parsed.Id.TrustDomain, bundleSource, validAudience)
            .ConfigureAwait(false);
    }

    private static (SpiffeId Id, JsonWebToken Jwt) ParseValidate(string token, IEnumerable<string> validAudience)
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

        ValidateLikeJose(jwt, validAudience);

        return (spiffeId, jwt);
    }

    private static async Task<TokenValidationResult> ValidateSignature(JsonWebToken jwt,
        TrustDomain trustDomain,
        IJwtBundleSource bundleSource,
        IEnumerable<string> validAudiences)
    {
        string kid = jwt.Kid;
        if (string.IsNullOrEmpty(kid))
        {
            throw new JwtSvidException("Token header missing key id");
        }

        JwtBundle bundle = bundleSource.GetJwtBundle(trustDomain);
        bool ok = bundle.JwtAuthorities.ContainsKey(kid);
        if (!ok)
        {
            throw new JwtSvidException($"No JWT authority {kid} found for trust domain {trustDomain}");
        }

        SecurityKey key = bundle.JwtAuthorities[kid];
        return await s_jsonHandler.ValidateTokenAsync(jwt,
            new TokenValidationParameters
            {
                ClockSkew = s_leeway,
                ValidateAudience = true,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                ValidateTokenReplay = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ValidAudiences = validAudiences
            }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Checks claims in a token against expected values. A
    ///     custom leeway may be specified for comparing time values. You may pass a
    ///     zero value to check time values with no leeway, but you should note that
    ///     numeric date values are rounded to the nearest second and sub-second
    ///     precision is not supported.
    ///     The leeway gives some extra time to the token from the server's
    ///     point of view. That is, if the token is expired, ValidateWithLeeway
    ///     will still accept the token for 'leeway' amount of time. This fails
    ///     if you're using this function to check if a server will accept your
    ///     token, because it will think the token is valid even after it
    ///     expires. So if you're a client validating if the token is valid to
    ///     be submitted to a server, use leeway lte 0, if you're a server
    ///     validation a token, use leeway gte 0.
    ///     <br />
    ///     See: <seealso href="https://github.com/go-jose/go-jose/blob/v3.0.1/jwt/validation.go#L78" />
    /// </summary>
    private static void ValidateLikeJose(JsonWebToken jwt, IEnumerable<string> expectedAudience)
    {
        // case-sensitive
        HashSet<string> s1 = new(jwt.Audiences, StringComparer.Ordinal);
        HashSet<string> s2 = new(expectedAudience, StringComparer.Ordinal);
        if (!s1.SetEquals(s2))
        {
            string actual = string.Join(", ", s1);
            string expected = string.Join(", ", s2);
            throw new JwtSvidException($"Expected audience is {expected} (audience={actual})");
        }

        DateTime now = DateTime.UtcNow;
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
            throw new JwtSvidException("Validation failed, token issued in the future (iat)");
        }
    }

    /// <summary>
    ///     Json web token have only one header, and it is signed for a supported algorithm
    /// </summary>
    private static void ValidateTokenAlgorithm(JsonWebToken jwt)
    {
        string alg = jwt.Alg;
        bool ok = alg == JwtAlgorithm.Rs256 || alg == JwtAlgorithm.Rs384 || alg == JwtAlgorithm.Rs512 ||
                  alg == JwtAlgorithm.Es256 || alg == JwtAlgorithm.Es384 || alg == JwtAlgorithm.Es512 ||
                  alg == JwtAlgorithm.Ps256 || alg == JwtAlgorithm.Ps384 || alg == JwtAlgorithm.Ps512;

        if (!ok)
        {
            throw new JwtSvidException($"Unsupported token signature algorithm '{alg}'");
        }
    }

    private static JwtSvid CreateJwtSvid(SpiffeId id, JsonWebToken jwt) =>
        new(
            jwt.EncodedToken,
            id,
            jwt.Audiences.ToList(),
            jwt.ValidTo,
            jwt.Claims.ToDictionary(c => c.Type, c => c.Value),
            string.Empty);
}
