namespace Spiffe.SpiffeId;

public class SpiffeId
{
  private const string SchemePrefix = "spiffe";

  private static readonly int SchemePrefixLength = SchemePrefix.Length;

  private readonly string id;

	// pathIndex tracks the index to the beginning of the path inside of id. This
	// is used when extracting the trust domain or path portions of the id.
  private readonly int pathIndex;

  private SpiffeId(string id, int pathIndex)
  {
    this.id = id;
    this.pathIndex = pathIndex;
  }

  public string TrustDomain => id.Substring(SchemePrefixLength, pathIndex);

  public string Path => id.Substring(pathIndex);

  public static SpiffeId Parse(string id)
  {
    return new SpiffeId(id, 0);
  }
}