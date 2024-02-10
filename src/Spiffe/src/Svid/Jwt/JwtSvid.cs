using Spiffe.Id;

namespace Spiffe.Svid.Jwt;

/// <summary>
/// Represents a JWT-SVID.
/// </summary>
public class JwtSvid
{
    /// <summary>
    /// Constructor
    /// </summary>
    public JwtSvid(SpiffeId id,
                   List<string> audience,
                   DateTime expiry,
                   Dictionary<string, object> claims,
                   string hint)
    {
        _ = id ?? throw new ArgumentNullException(nameof(id));
        _ = audience ?? throw new ArgumentNullException(nameof(audience));
        _ = claims ?? throw new ArgumentNullException(nameof(claims));
        _ = hint ?? throw new ArgumentNullException(nameof(hint));

        Id = id;
        Audience = new List<string>(audience);
        Expiry = expiry;
        Claims = new Dictionary<string, object>(claims);
        Hint = hint;
    }

    /// <summary>
    /// SPIFFE ID of the JWT-SVID as present in the 'sub' claim
    /// </summary>
    public SpiffeId Id { get; }

    /// <summary>
    /// Intended recipients of JWT-SVID as present in the 'aud' claim
    /// </summary>
    public List<string> Audience { get; }

    /// <summary>
    /// Expiry is the expiration time of JWT-SVID as present in 'exp' claim
    /// </summary>
    public DateTime Expiry { get; }

    /// <summary>
    /// Claims is the parsed claims from token
    /// </summary>
    public Dictionary<string, object> Claims { get; }

    /// <summary>
    /// Operator-specified string used to provide guidance on how this
    /// identity should be used by a workload when more than one SVID is returned.
    /// </summary>
    public string Hint { get; }
}
