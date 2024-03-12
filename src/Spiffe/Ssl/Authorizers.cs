using Spiffe.Id;

namespace Spiffe.Ssl;

/// <summary>
/// Collection of <see cref="IAuthorizer"/> authorizers.
/// </summary>
public static class Authorizers
{
    /// <summary>
    /// Allows any SPIFFE ID.
    /// </summary>
    public static IAuthorizer AuthorizeAny()
    {
        return new Authorizer(_ => true);
    }

    /// <summary>
    /// Allows a specific SPIFFE ID.
    /// </summary>
    public static IAuthorizer AuthorizeId(SpiffeId allowed)
    {
        _ = allowed ?? throw new ArgumentNullException(nameof(allowed));

        return new Authorizer(id => id != null && allowed.Equals(id));
    }

    /// <summary>
    /// Allows any SPIFFE ID in the given list of IDs.
    /// </summary>
    public static IAuthorizer AuthorizeOneOf(IEnumerable<SpiffeId> allowed)
    {
        _ = allowed ?? throw new ArgumentNullException(nameof(allowed));

        HashSet<SpiffeId> allowedSet = [.. allowed];
        return new Authorizer(id => id != null && allowedSet.Contains(id));
    }

    /// <summary>
    /// Allows any SPIFFE ID in the given trust domain.
    /// </summary>
    public static IAuthorizer AuthorizeMemberOf(TrustDomain allowed)
    {
        _ = allowed ?? throw new ArgumentNullException(nameof(allowed));

        return new Authorizer(id => id != null && id.MemberOf(allowed));
    }

    /// <summary>
    /// Allows any SPIFFE ID that matches the given predicate.
    /// </summary>
    public static IAuthorizer UseFunc(Func<SpiffeId, bool> predicate)
    {
        _ = predicate ?? throw new ArgumentNullException(nameof(predicate));

        return new Authorizer(predicate);
    }
}
