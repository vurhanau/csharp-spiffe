namespace Spiffe.Id;

using static Spiffe.Id.SpiffePath;
using static Spiffe.Id.SpiffeTrustDomain;

public class SpiffeId
{
    private const string SchemePrefix = "spiffe://";

    private static readonly int SchemePrefixLength = SchemePrefix.Length;

    // pathIndex tracks the index to the beginning of the path inside of id. This
    // is used when extracting the trust domain or path portions of the id.
    private readonly int _pathIndex;

    /// <summary>
    /// String representation of the SPIFFE ID, e.g.,
    /// "spiffe://example.org/foo/bar".
    /// </summary>
    public string Id { get; }

    private SpiffeId(string id, int pathIndex)
    {
        Id = id;
        _pathIndex = pathIndex;
    }

    /// <summary>
    /// The trust domain of the SPIFFE ID.
    /// </summary>
    public SpiffeTrustDomain TrustDomain => new(Id[SchemePrefixLength.._pathIndex]);

    /// <summary>
    /// The path of the SPIFFE ID inside the trust domain.
    /// </summary>
    public string Path => Id[_pathIndex..];

    /// <summary>
    /// True if the SPIFFE ID is a member of the given trust domain.
    /// </summary>
    public bool MemberOf(SpiffeTrustDomain td) => TrustDomain.Equals(td);

    public override string ToString() => Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? other)
    {
        if (other is not SpiffeId)
        {
            return false;
        }

        string otherId = (other as SpiffeId)!.Id;
        return string.Equals(Id, otherId, StringComparison.Ordinal);
    }

    /// <summary>
    /// URI for SPIFFE ID.
    /// </summary>
    /// <returns></returns>
    public Uri ToUri()
    {
        UriBuilder builder = new()
        {
            Scheme = SchemePrefix,
            Host = TrustDomain.Name,
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
    /// FromSegments returns a new SPIFFE ID in the given trust domain with joined
    /// path segments. The path segments must be valid according to the SPIFFE
    /// specification and must not contain path separators.
    /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public static SpiffeId FromSegments(SpiffeTrustDomain td, params string[] segments)
    {
        _ = td ?? throw new ArgumentNullException(nameof(td));
        _ = segments ?? throw new ArgumentNullException(nameof(segments));

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
    /// Parses a SPIFFE ID from a URI.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static SpiffeId FromUri(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        // Root uri has extra trailing slash
        return FromString(uri.ToString().TrimEnd('/'));
    }

    /// <summary>
    /// Returns an ID with the appended path.
    /// The path to append must be a valid absolute path according to
    /// the SPIFFE specification.
    /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId AppendPath(string path)
    {
        ValidatePath(path);
        return new(Id + path, _pathIndex);
    }

    /// <summary>
    /// Returns an ID with the appended joined path segments.  It
    /// will fail if called on a zero value. The path segments must be valid
    /// according to the SPIFFE specification and must not contain path separators.
    /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId AppendSegments(params string[] segments)
    {
        string path = JoinPathSegments(segments);
        return new(Id + path, _pathIndex);
    }

    /// <summary>
    /// Returns an ID with the given path in the same trust domain. It
    /// will fail if called on a zero value. The given path must be a valid absolute
    /// path according to the SPIFFE specification.
    /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId ReplacePath(string path) => FromPath(TrustDomain, path);

    /// <summary>
    /// Returns an ID with the joined path segments in the same
    /// trust domain. It will fail if called on a zero value. The path segments must
    /// be valid according to the SPIFFE specification and must not contain path
    /// separators.
    /// See <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId ReplaceSegments(params string[] segments) => FromSegments(TrustDomain, segments);

    internal static SpiffeId MakeId(SpiffeTrustDomain td, string path)
    {
        if (string.IsNullOrEmpty(td.Name))
        {
            throw new ArgumentException("Trust domain is empty", nameof(td));
        }

        string id = SchemePrefix + td.Name + path;
        int pathIndex = SchemePrefixLength + td.Name.Length;
        return new SpiffeId(id, pathIndex);
    }
}