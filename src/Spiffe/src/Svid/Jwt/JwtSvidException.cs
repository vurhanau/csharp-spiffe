namespace Spiffe.Svid.Jwt;

/// <summary>
/// JWT-SVID errors.
/// </summary>
public class JwtSvidException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtSvidException"/> class.
    /// </summary>
    public JwtSvidException(string message)
    : base(message)
    {
    }
}
