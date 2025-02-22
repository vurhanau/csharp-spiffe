using Spiffe.Id;

namespace Spiffe.Ssl;

/// <summary>
///     Authorizes an X509-SVID given the SPIFFE ID.
/// </summary>
public interface IAuthorizer
{
    // Verified chain passing is not supported.

    /// <summary>
    ///     Authorizes an X509-SVID given the SPIFFE ID.
    /// </summary>
    bool Authorize(SpiffeId id);
}
