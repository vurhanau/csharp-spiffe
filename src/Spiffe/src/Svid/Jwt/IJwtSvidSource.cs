namespace Spiffe.Svid.Jwt;

/// <summary>
/// Represents a source of JWT-SVIDs.
/// </summary>
public interface IJwtSvidSource
{
    /// <summary>
    /// Fetches a JWT-SVID from the source with the given parameters.
    /// </summary>
    Task<JwtSvid> FetchJwtSvidAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all JWT-SVIDs from the source with the given parameters.
    /// </summary>
    Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default);
}
