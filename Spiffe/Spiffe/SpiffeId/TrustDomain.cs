namespace Spiffe.SpiffeId;

/**
 * Represents the name of a SPIFFE trust domain (e.g. 'domain.test').
 */
public class TrustDomain
{
  private readonly string name;

  private TrustDomain(string name)
  {
    this.name = name;
  }

  public static TrustDomain Parse(string trustDomain)
  {
    return new TrustDomain(trustDomain);
  }
}
