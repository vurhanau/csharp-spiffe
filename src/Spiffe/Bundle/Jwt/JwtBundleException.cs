namespace Spiffe.Bundle.Jwt;

/// <summary>
/// JWT bundle errors.
/// </summary>
public class JwtBundleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBundleException"/> class.
    /// </summary>
    public JwtBundleException(string message)
    : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBundleException"/> class.
    /// </summary>
    public JwtBundleException(string message, Exception inner)
    : base(message, inner)
    {
    }
}
