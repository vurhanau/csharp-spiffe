namespace Spiffe.Svid.Jwt;

/// <summary>
/// Represents a source of JWT-SVIDs.
/// </summary>
public interface IJwtSvidSource
{
    /// <summary>
    /// Gets current SVID.
    /// </summary>
    JwtSvid FetchJwtSvid(JwtSvidParams param, CancellationToken cancellationToken = default);
}
