namespace Spiffe.Error;

/// <summary>
/// Thrown to indicate that a Bundle could not be found in the Bundle Source.
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
public class BundleNotFoundException(string message) : Exception(message)
{
}
