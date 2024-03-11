using static Spiffe.Id.SpiffePath;
using static Spiffe.Id.TrustDomain;

namespace Spiffe.Id;

/// <summary>
/// Represents a SPIFFE ID as defined in the SPIFFE standard.
/// See <seealso href="https://github.com/spiffe/spiffe/blob/master/standards/SPIFFE-ID.md">https://github.com/spiffe/spiffe/blob/master/standards/SPIFFE-ID.md</seealso>
/// </summary>
public class SpiffeId
{
    private const string SchemePrefix = "spiffe://";

    private static readonly int s_schemePrefixLength = SchemePrefix.Length;

    // pathIndex tracks the index to the beginning of the path inside of id. This
    // is used when extracting the trust domain or path portions of the id.
    private readonly int _pathIndex;

    private SpiffeId(string id, int pathIndex)
    {
        Id = id;
        _pathIndex = pathIndex;
    }

    /// <summary>
    /// Gets string representation of the SPIFFE ID, e.g.,
    /// "spiffe://example.org/foo/bar".
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the trust domain of the SPIFFE ID.
    /// </summary>
    public TrustDomain TrustDomain => new(Id[s_schemePrefixLength.._pathIndex]);

    /// <summary>
    /// Gets the path of the SPIFFE ID inside the trust domain.
    /// </summary>
    public string Path => Id[_pathIndex..];

    /// <summary>
    /// True if the SPIFFE ID is a member of the given trust domain.
    /// </summary>
    public bool MemberOf(TrustDomain td) => TrustDomain.Equals(td);

    /// <summary>
    /// Returns an ID with the appended path.
    /// The path to append must be a valid absolute path according to
    /// the SPIFFE specification.
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
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
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
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
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId ReplacePath(string path) => FromPath(TrustDomain, path);

    /// <summary>
    /// Returns an ID with the joined path segments in the same
    /// trust domain. It will fail if called on a zero value. The path segments must
    /// be valid according to the SPIFFE specification and must not contain path
    /// separators.
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
    /// </summary>
    public SpiffeId ReplaceSegments(params string[] segments) => FromSegments(TrustDomain, segments);

    /// <summary>
    /// Returns the string representation of the SPIFFE ID, concatenating schema, trust domain,
    /// and path segments (e.g. 'spiffe://example.org/path1/path2')
    /// </summary>
    public override string ToString() => Id;

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SpiffeId spiffeId && string.Equals(Id, spiffeId.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// URI for SPIFFE ID.
    /// </summary>
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
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>.
    /// </summary>
    public static SpiffeId FromPath(TrustDomain td, string path)
    {
        ValidatePath(path);
        return MakeId(td, path);
    }

    /// <summary>
    /// FromSegments returns a new SPIFFE ID in the given trust domain with joined
    /// path segments. The path segments must be valid according to the SPIFFE
    /// specification and must not contain path separators.
    /// See <seealso href="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>.
    /// </summary>
    public static SpiffeId FromSegments(TrustDomain td, params string[] segments)
    {
        _ = td ?? throw new ArgumentNullException(nameof(td));
        _ = segments ?? throw new ArgumentNullException(nameof(segments));

        string path = JoinPathSegments(segments);
        return MakeId(td, path);
    }

    /// <summary>
    /// Parses a SPIFFE ID from a string.
    /// </summary>
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

        int pathIndex = s_schemePrefixLength;
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

        if (pathIndex == s_schemePrefixLength)
        {
            throw new ArgumentException(Errors.MissingTrustDomain, nameof(id));
        }

        ValidatePath(id[pathIndex..]);

        return new SpiffeId(id, pathIndex);
    }

    /// <summary>
    /// Parses a SPIFFE ID from a URI.
    /// </summary>
    public static SpiffeId FromUri(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        // Root uri has extra trailing slash
        return FromString(uri.ToString().TrimEnd('/'));
    }

    internal static SpiffeId MakeId(TrustDomain td, string path)
    {
        if (string.IsNullOrEmpty(td.Name))
        {
            throw new ArgumentException("Trust domain is empty", nameof(td));
        }

        string id = SchemePrefix + td.Name + path;
        int pathIndex = s_schemePrefixLength + td.Name.Length;
        return new SpiffeId(id, pathIndex);
    }
}
