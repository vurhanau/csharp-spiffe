namespace Spiffe.Svid.Jwt;

/// <summary>
///     Represents a source of JWT-SVIDs.
/// </summary>
public interface IJwtSvidSource
{
    /// <summary>
    ///     Fetches all JWT-SVIDs from the source with the given parameters.
    /// </summary>
    Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default);
}
