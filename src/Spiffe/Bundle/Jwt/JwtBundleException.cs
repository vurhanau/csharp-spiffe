namespace Spiffe.Bundle.Jwt;

/// <summary>
/// JWT bundle errors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JwtBundleException"/> class.
/// </remarks>
public class JwtBundleException(string message, Exception inner) : Exception(message, inner);
