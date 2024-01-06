#if OS_WINDOWS

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
public static partial class Address
{
    private const string SchemeNamedPipe = "npipe";

    /// <summary>
    /// Parses the endpoint address and returns a gRPC named pipe target.
    /// </summary>
    private static partial string ParseNativeTarget(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (!SchemeNamedPipe.Equals(uri.Scheme, StringComparison.Ordinal))
        {
            throw new ArgumentException("Workload endpoint socket URI must have a \"tcp\" or \"npipe\" scheme");
        }

        // https://pkg.go.dev/net/url#URL.Opaque
        if (!string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException("Workload endpoint named pipe URI must be opaque");
        }

        if (!uri.Segments.Any())
        {
            throw new ArgumentException("Workload endpoint named pipe URI must include an opaque part");
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException("Workload endpoint named pipe URI must not include query values");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Workload endpoint named pipe URI must not include a fragment");
        }

        return GetNamedPipeTarget(uri.ToString());
    }

    private static string GetNamedPipeTarget(string pipeName)
    {
        return @"\\.\" + Path.Join("pipe", pipeName);
    }
}

#endif
