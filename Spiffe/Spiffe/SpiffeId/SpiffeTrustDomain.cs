namespace Spiffe.SpiffeId;

/**
 * Represents the name of a SPIFFE trust domain (e.g. 'domain.test').
 */
public class SpiffeTrustDomain
{
  public string Name { get; }

  internal SpiffeTrustDomain(string name)
  {
    Name = name;
  }

  /// <summary>
  /// True if the trust domain is the zero value.
  /// </summary>
  public bool IsZero => Name == string.Empty;

  /// <summary>
  /// The trust domain name as a string, e.g. example.org.
  /// </summary>
  public string String => Name;

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
}
