namespace Spiffe.Id;

/// <summary>
/// Represents the name of a SPIFFE trust domain (e.g. 'example.org').
/// </summary>
public class TrustDomain
{
    internal TrustDomain(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the trust domain name as a string, e.g. example.org.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets SPIFFE ID of the trust domain.
    /// </summary>
    public SpiffeId SpiffeId => SpiffeId.MakeId(this, string.Empty);

    /// <summary>
    /// Returns a new TrustDomain from a string. The string
    /// can either be a trust domain name (e.g. example.org), or a valid SPIFFE ID
    /// URI (e.g. spiffe://example.org), otherwise an error is returned.
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#21-trust-domain"/>.
    /// </summary>
    public static TrustDomain FromString(string idOrName)
    {
        if (string.IsNullOrEmpty(idOrName))
        {
            throw new ArgumentException(Errors.MissingTrustDomain, nameof(idOrName));
        }

        if (idOrName.Contains(":/"))
        {
            // The ID looks like it has something like a scheme separator, let's
            // try to parse as an ID. We use :/ instead of :// since the
            // diagnostics are better for a bad input like spiffe:/trustdomain.
            SpiffeId id = SpiffeId.FromString(idOrName);
            return id.TrustDomain;
        }

        foreach (char c in idOrName)
        {
            if (!IsValidTrustDomainChar(c))
            {
                throw new ArgumentException(Errors.BadTrustDomainChar, nameof(idOrName));
            }
        }

        return new(idOrName);
    }

    /// <summary>
    /// Returns a new TrustDomain from a URI.
    /// The URI must be a valid SPIFFE ID.
    /// The trust domain is extracted from the host field.
    /// </summary>
    public static TrustDomain FromUri(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        SpiffeId id = SpiffeId.FromUri(uri);
        return id.TrustDomain;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TrustDomain td && string.Equals(Name, td.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Name.GetHashCode();

    /// <summary>
    /// Returns the trust domain as a string.
    /// </summary>
    public override string ToString() => Name;

    internal static bool IsValidTrustDomainChar(char c)
    {
        return c switch
        {
            >= 'a' and <= 'z' => true,
            >= '0' and <= '9' => true,
            '-' or '.' or '_' => true,
            _ => false,
        };
    }
}
