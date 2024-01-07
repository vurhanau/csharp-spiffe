namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
internal static partial class Address
{
    internal static bool IsHttpOrHttps(string address)
    {
        return IsHttpOrHttps(new Uri(address));
    }

    internal static bool IsHttpOrHttps(Uri uri)
    {
        return Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.Ordinal) ||
                Uri.UriSchemeHttp.Equals(uri.Scheme, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tells whether or not this URI is opaque.
    ///
    /// <p> A URI is opaque if, and only if, it is absolute and its
    /// scheme-specific part does not begin with a slash character ('/').
    /// An opaque URI has a scheme, a scheme-specific part, and possibly
    /// a fragment; all other components are undefined. </p>
    /// See <seealso href="https://pkg.go.dev/net/url#URL.Opaque"/> and
    /// <seealso href="https://docs.oracle.com/en/java/javase/17/docs/api/java.base/java/net/URI.html#isOpaque()"/>
    /// </summary>
    /// <returns>True if, and only if, this URI is opaque</returns>
    private static bool IsOpaque(Uri uri)
    {
        return string.IsNullOrEmpty(uri.Host) && !uri.PathAndQuery.StartsWith('/');
    }

    private static string TrimScheme(Uri uri)
    {
        return Uri.UnescapeDataString((uri.Host + uri.AbsolutePath).TrimEnd('/'));
    }
}
