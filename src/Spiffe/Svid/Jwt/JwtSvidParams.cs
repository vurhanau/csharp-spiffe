using Spiffe.Id;

namespace Spiffe.Svid.Jwt;

/// <summary>
///     JWT-SVID parameters used when fetching a new JWT-SVID.
/// </summary>
public class JwtSvidParams
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public JwtSvidParams(string audience,
        List<string> extraAudiences,
        SpiffeId? subject)
    {
        _ = audience ?? throw new ArgumentNullException(nameof(audience));
        _ = extraAudiences ?? throw new ArgumentNullException(nameof(extraAudiences));

        Audience = audience;
        ExtraAudiences = new List<string>(extraAudiences);
        Subject = subject;
    }

    /// <summary>
    ///     Intended recipients of JWT-SVID as present in the 'aud' claim. Required.
    ///     <br />
    /// </summary>
    public string Audience { get; }

    /// <summary>
    ///     Extra recipients of JWT-SVID
    /// </summary>
    public List<string> ExtraAudiences { get; }

    /// <summary>
    ///     SPIFFE ID of the JWT-SVID as present in the 'sub' claim. Optional.
    /// </summary>
    public SpiffeId? Subject { get; }
}
