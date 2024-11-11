using Microsoft.IdentityModel.Tokens;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;

namespace Spiffe.AspNetCore.Server;

internal class JwtSvidTokenHandler : TokenHandler
{
    private readonly IEnumerable<string> _audience;

    private readonly IJwtSource _jwtSource;

    public JwtSvidTokenHandler(IJwtSource jwtSource, string audience)
    {
        _jwtSource = jwtSource ?? throw new ArgumentNullException(nameof(jwtSource));
        _ = audience ?? throw new ArgumentNullException(nameof(audience));
        _audience = [audience];
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        return await JwtSvidParser.ValidateAsync(token, _jwtSource, _audience);
    }
}
