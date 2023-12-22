namespace Spiffe.SpiffeId;

using System.Security.Cryptography;
using System.Text;
using static Spiffe.SpiffeId.SpiffePath;
using static Spiffe.SpiffeId.SpiffeTrustDomain;

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

  /// <summary>
  /// The trust domain of the SPIFFE ID.
  /// </summary>
  public SpiffeTrustDomain TrustDomain => 
    IsZero 
      ? new(string.Empty)
      : new(id[SchemePrefixLength..pathIndex]);

  /// <summary>
  /// The path of the SPIFFE ID inside the trust domain.
  /// </summary>
  public string Path => id[pathIndex..];

  public bool IsZero => id == string.Empty;

  /// <summary>
  /// True if the SPIFFE ID is a member of the given trust domain.
  /// </summary>
  public bool MemberOf(SpiffeTrustDomain td) => TrustDomain.Equals(td);

  /// <summary>
  /// String returns the string representation of the SPIFFE ID, e.g.,
  /// "spiffe://example.org/foo/bar".
  /// </summary>
  public string String => id;

  /// <summary>
  /// URI for SPIFFE ID.
  /// </summary>
  /// <returns></returns>
  public Uri ToUri()
  {
    UriBuilder builder = new()
    {
      Scheme = SchemePrefix,
      Host = TrustDomain.String,
      Path = Path,
    };

    return builder.Uri;
  }

  /// <summary>
  /// FromPath returns a new SPIFFE ID in the given trust domain and with the
  /// given path. The supplied path must be a valid absolute path according to the
  /// SPIFFE specification.
  /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
    public static SpiffeId FromPath(SpiffeTrustDomain td, string path)
  {
    ValidatePath(path);
    return MakeId(td, path);
  }

  /// <summary>
  /// FromPathf returns a new SPIFFE ID from the formatted path in the given trust
  /// domain. The formatted path must be a valid absolute path according to the
  /// SPIFFE specification.
  /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public static SpiffeId FromPathf(SpiffeTrustDomain td, string format, params object[] args)
  {
    string path = FormatPath(format, args);
    return MakeId(td, path);
  }

  /// <summary>
  /// FromSegments returns a new SPIFFE ID in the given trust domain with joined
  /// path segments. The path segments must be valid according to the SPIFFE
  /// specification and must not contain path separators.
  /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public static SpiffeId FromSegments(SpiffeTrustDomain td, params string[] segments)
  {
    string path = JoinPathSegments(segments);
    return MakeId(td, path);
  }

  /// <summary>
  /// Parses a SPIFFE ID from a string.
  /// </summary>
  /// <exception cref="ArgumentException"></exception>
  public static SpiffeId FromString(string id)
  {
    if (string.IsNullOrEmpty(id))
    {
      throw new ArgumentException(Errors.Empty, nameof(id));
    }

    if (!id.StartsWith(SchemePrefix))
    {
      throw new ArgumentException(Errors.WrongScheme, nameof(id));
    }

    int pathIndex = SchemePrefixLength;
    for (; pathIndex < id.Length; pathIndex++)
    {
      char c = id[pathIndex];
      if (c == '/')
      {
        break;
      }

      if (!IsValidTrustDomainChar(c))
      {
        throw new ArgumentException(Errors.BadTrustDomainChar, nameof(id));
      }
    }

    if (pathIndex == SchemePrefixLength)
    {
      throw new ArgumentException(Errors.MissingTrustDomain, nameof(id));
    }

    ValidatePath(id[pathIndex..]);

    return new SpiffeId(id, pathIndex);
  }

  /// <summary>
  /// Parses a SPIFFE ID from a formatted string.
  /// </summary>
  public static SpiffeId FromStringf(string format, params object[] args)
  {
	  return FromString(string.Format(format, args));
  }

  /// <summary>
  /// Parses a SPIFFE ID from a URI.
  /// </summary>
  /// <exception cref="ArgumentNullException"></exception>
  public static SpiffeId FromUri(Uri uri)
  {
    _ = uri ?? throw new ArgumentNullException(nameof(uri));

    return FromString(uri.ToString());
  }

  /// <summary>
  /// AppendPath returns an ID with the appended path. It will fail if called on a
  /// zero value. The path to append must be a valid absolute path according to
  /// the SPIFFE specification.
  /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public SpiffeId AppendPath(string path)
  {
    if (IsZero)
    {
      throw new InvalidOperationException("Cannot append path on a zero ID value");
    }

    ValidatePath(path);

    return new(id + path, pathIndex);
  }

  /// <summary>
  /// AppendPathf returns an ID with the appended formatted path. It will fail if
  /// called on a zero value. The formatted path must be a valid absolute path
  /// according to the SPIFFE specification.
  /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public SpiffeId AppendPathf(string format, params object[] args)
  {
    if (IsZero)
    {
      throw new InvalidOperationException("Cannot append path on a zero ID value");
    }

    string path = FormatPath(format, args);
    return new(id + path, pathIndex);
  }

  /// <summary>
  /// AppendSegments returns an ID with the appended joined path segments.  It
  /// will fail if called on a zero value. The path segments must be valid
  /// according to the SPIFFE specification and must not contain path separators.
  /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public SpiffeId AppendSegments(params string[] segments)
  {
    if (IsZero)
    {
      throw new InvalidOperationException("Cannot append path on a zero ID value");
    }

    string path = JoinPathSegments(segments);
    return new(id + path, pathIndex);
  }

  /// <summary>
  /// ReplaceSegments returns an ID with the joined path segments in the same
  /// trust domain. It will fail if called on a zero value. The path segments must
  /// be valid according to the SPIFFE specification and must not contain path
  /// separators.
  /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public SpiffeId ReplaceSegments(params string[] segments)
  {
    if (IsZero)
    {
      throw new InvalidOperationException("Cannot replace path segments on a zero ID value");
    }

    return FromSegments(TrustDomain, segments);
  }

  /// <summary>
  /// MarshalText returns a text representation of the ID. If the ID is the zero
  /// value, nil is returned.
  /// </summary>
  public byte[] MarshalText()
  {
    if (IsZero)
    {
      return [];
    }

    return Encoding.ASCII.GetBytes(id);
  }

  /// <summary>
  /// UnmarshalText decodes a text representation of the ID. If the text is empty,
  /// the ID is set to the zero value.
  /// </summary>
  /// <exception cref="ArgumentNullException"></exception>
  public static SpiffeId UnmarshallText(byte[] bytes)
  {
    _ = bytes ?? throw new ArgumentNullException(nameof(bytes));

    if (bytes.Length == 0)
    {
      return new(string.Empty, 0);
    }

    string id = Encoding.ASCII.GetString(bytes);
    return FromString(id);
  }

  internal static SpiffeId MakeId(SpiffeTrustDomain td, string path)
  {
    if (td.IsZero)
    {
      throw new ArgumentException("Trust domain is empty", nameof(td));
    }

    string id = SchemePrefix + td.Name + path;
    int pathIndex = SchemePrefixLength + td.Name.Length;
    return new SpiffeId(id, pathIndex);
  }
}