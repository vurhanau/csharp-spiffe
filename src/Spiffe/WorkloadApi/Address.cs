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

    private static bool IsHttpOrHttps(Uri uri)
    {
        return Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.Ordinal) ||
                Uri.UriSchemeHttp.Equals(uri.Scheme, StringComparison.Ordinal);
    }

    private static string TrimScheme(Uri uri)
    {
        return Uri.UnescapeDataString((uri.Host + uri.AbsolutePath).TrimEnd('/'));
    }
}
