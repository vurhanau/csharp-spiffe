namespace Spiffe.Svid.X509;

/// <summary>
///     Represents a source of X509-SVIDs.
/// </summary>
public interface IX509SvidSource
{
    /// <summary>
    ///     Gets current SVID.
    /// </summary>
    X509Svid GetX509Svid();
}
