using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Util;
using Spiffe.WorkloadApi;

internal class JwtSvidTokenHandler : TokenHandler
{
    private readonly IJwtSource _jwtSource;

    private readonly string _audience;

    public JwtSvidTokenHandler(IJwtSource jwtSource, string audience)
    {
        _jwtSource = jwtSource ?? throw new ArgumentNullException(nameof(jwtSource));
        _audience = audience ?? throw new ArgumentNullException(nameof(audience));
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        JwtSvid svid = await JwtSvidParser.Parse(token, _jwtSource, [_audience]);
        // TODO: remove validation logic duplication
        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        TrustDomain td = svid.Id.TrustDomain;
        JwtBundle bundle = _jwtSource.GetJwtBundle(td);
        bool ok = bundle.JwtAuthorities.ContainsKey(jwt.Kid);
        if (!ok)
        {
            throw new JwtSvidException($"No JWT authority {jwt.Kid} found for trust domain {td}");
        }

        SecurityKey key = bundle.JwtAuthorities[jwt.Kid];
        TokenValidationResult result = await JwtSvidParser.ValidateTokenAsync(jwt, key, [_audience]);
        return result;
    }
}
