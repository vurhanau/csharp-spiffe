using System.Text;

namespace Spiffe;

/**
 * Represents the name of a SPIFFE trust domain (e.g. 'example.org').
 */
public class SpiffeTrustDomain : IComparable<SpiffeTrustDomain>
{
  /// <summary>
  /// The trust domain name as a string, e.g. example.org.
  /// </summary>
  public string Name { get; }

  internal SpiffeTrustDomain(string name)
  {
    Name = name;
  }

  /// <summary>
  /// True if the trust domain is the zero value.
  /// </summary>
  public bool IsZero => string.IsNullOrEmpty(Name);

  /// <summary>
  /// The trust domain name as a string, e.g. example.org.
  /// </summary>
  public string String => Name;

  /// <summary>
  /// SPIFFE ID of the trust domain.
  /// </summary>
  public SpiffeId Id => SpiffeId.MakeId(this, string.Empty);

  /// <summary>
  /// String representation of the the SPIFFE ID of the trust
  /// domain, e.g. "spiffe://example.org".
  /// </summary>
  public string IdString => Id.String;

  /// <summary>
  /// Returns a new TrustDomain from a string. The string
  /// can either be a trust domain name (e.g. example.org), or a valid SPIFFE ID
  /// URI (e.g. spiffe://example.org), otherwise an error is returned.
  /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#21-trust-domain"/>.
  /// </summary>
  public static SpiffeTrustDomain FromString(string idOrName)
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

    for (int i = 0; i < idOrName.Length; i++)
    {
      if (!IsValidTrustDomainChar(idOrName[i]))
      {
        throw new ArgumentException(Errors.BadTrustDomainChar, nameof(idOrName));
      }
    }

    return new(idOrName);
  }

  public byte[] MarshallText()
  {
    return IsZero ? [] : Encoding.ASCII.GetBytes(Name);
  }

  public static SpiffeTrustDomain UnmarshallText(byte[] bytes)
  {
    _ = bytes ?? throw new ArgumentNullException(nameof(bytes));

    string name = Encoding.ASCII.GetString(bytes);
    return new(name);
  }

  public static SpiffeTrustDomain FromUri(Uri uri)
  {
    _ = uri ?? throw new ArgumentNullException(nameof(uri));

    SpiffeId id = SpiffeId.FromUri(uri);
    return id.TrustDomain;
  }

  internal static bool IsValidTrustDomainChar(char c)
  {
    if (c >= 'a' && c <= 'z')
    {
      return true;
    }

    if (c >= '0' && c <= '9')
    {
      return true;
    }

    if (c == '-' || c == '.' || c == '_')
    {
      return true;
    }

    return false;
  }

  public override bool Equals(object? other)
  {
    if (other is not SpiffeTrustDomain)
    {
      return false;
    }

    string otherName = (other as SpiffeTrustDomain)!.Name;
    return string.Equals(Name, otherName, StringComparison.Ordinal);
  }

  public override int GetHashCode() => Name.GetHashCode();

  public override string ToString() => Name;

  /// <summary>
  /// Returns an integer comparing the trust domain to another
  /// lexicographically. The result will be 0 if td==other, -1 if td < other, and
  /// +1 if td > other.
  /// </summary>
  public int CompareTo(SpiffeTrustDomain? other)
  {
    return string.Compare(Name, other?.Name);
  }
}
