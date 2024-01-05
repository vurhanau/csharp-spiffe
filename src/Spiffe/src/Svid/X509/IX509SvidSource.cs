namespace Spiffe.Svid.X509;

/// <summary>
/// Represents a source of X.509 SVIDs.
/// </summary>
public interface IX509SvidSource
{
    /// <summary>
    /// Gets the X.509 SVID in the source.
    /// </summary>
    X509Svid X509Svid { get; }
}
