namespace Spiffe.Error;

/// <summary>
/// Thrown to indicate that a Bundle could not be found in the Bundle Source.
/// </summary>
public class BundleNotFoundException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    public BundleNotFoundException()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public BundleNotFoundException(string message)
    : base(message)
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public BundleNotFoundException(string message, Exception inner)
    : base(message, inner)
    {
    }
}
