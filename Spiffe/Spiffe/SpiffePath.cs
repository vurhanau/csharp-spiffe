using System.Security.Cryptography;
using System.Text;

namespace Spiffe;

public static class SpiffePath
{
  /// <summary>
  /// FormatPath builds a path by formatting the given formatting string with
  /// the given args (i.e. fmt.Sprintf). The resulting path must be valid or
  /// an error is returned.
  /// </summary>
  public static string FormatPath(string format, params object[] args)
  {
    _ = format ?? throw new ArgumentNullException(nameof(format));
    _ = args ?? throw new ArgumentNullException(nameof(args));

    string path = string.Format(format, args);
    ValidatePath(path);

    return path;
  }

  /// <summary>
  /// JoinPathSegments joins one or more path segments into a slash separated
  /// path. Segments cannot contain slashes. The resulting path must be valid or
  /// an error is returned. If no segments are provided, an empty string is
  /// returned.
  /// </summary>
  /// <returns></returns>
  public static string JoinPathSegments(params string[] segments)
  {
    _ = segments ?? throw new ArgumentNullException(nameof(segments));

    StringBuilder builder = new();
    foreach (string segment in segments)
    {
      ValidatePathSegment(segment);

      builder.Append('/');
      builder.Append(segment);
    }

    return builder.ToString();
  }

  /// <summary>
  /// ValidatePath validates that a path string is a conformant path for a SPIFFE ID.
  /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  /// <exception cref="ArgumentException"></exception>
  public static void ValidatePath(string path)
  {
    if (string.IsNullOrEmpty(path))
    {
      return;
    }

    if (path[0] != '/')
    {
      throw new ArgumentException(Errors.NoLeadingSlash, nameof(path));
    }

    int segmentStart = 0;
    int segmentEnd = 0;
    for (; segmentEnd < path.Length; segmentEnd++)
    {
      char c = path[segmentEnd];
      if (c == '/')
      {
        string s = path[segmentStart..segmentEnd];
        if (s == "/")
        {
          throw new ArgumentException(Errors.EmptySegment, nameof(path));
        }

        if (s == "/." || s == "/..")
        {
          throw new ArgumentException(Errors.DotSegment, nameof(path));
        }

        segmentStart = segmentEnd;
        continue;
      }

      if (!IsValidPathSegmentChar(c))
      {
        throw new ArgumentException(Errors.BadPathSegmentChar, nameof(path));
      }
    }

    string segment = path[segmentStart..segmentEnd];
    if (segment == "/")
    {
      throw new ArgumentException(Errors.TrailingSlash, nameof(path));
    }

    if (segment == "/." || segment == "/..")
    {
      throw new ArgumentException(Errors.DotSegment, nameof(path));
    }
  }

  /// <summary>
  /// ValidatePathSegment validates that a string is a conformant segment for
  /// inclusion in the path for a SPIFFE ID.
  /// Reference: <seealso cref="https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE-ID.md#22-path"/>
  /// </summary>
  public static void ValidatePathSegment(string segment)
  {
    if (string.IsNullOrEmpty(segment))
    {
      throw new ArgumentException(Errors.EmptySegment, nameof(segment));
    }

    if (segment == "." || segment == "..")
    {
      throw new ArgumentException(Errors.DotSegment, nameof(segment));
    }

    for (int i = 0; i < segment.Length; i++)
    {
      if (!IsValidPathSegmentChar(segment[i]))
      {
        throw new ArgumentException(Errors.BadPathSegmentChar, nameof(segment));
      }
    }
  }

  private static bool IsValidPathSegmentChar(char c)
  {
    if (c >= 'a' && c <= 'z')
    {
      return true;
    }

    if (c >= 'A' && c <= 'Z')
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