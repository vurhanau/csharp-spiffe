#if OS_WINDOWS
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Spiffe.Tests")]

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
internal static partial class Address
{
    internal static bool IsNamedPipe(Uri uri)
    {
        return "npipe".Equals(uri.Scheme, StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses the endpoint address and returns a gRPC named pipe target.
    /// </summary>
    internal static string ParseNamedPipeTarget(string address)
    {
        Uri uri = new(address);
        if (!IsNamedPipe(uri))
        {
            throw new ArgumentException("Workload endpoint socket URI must have a supported scheme");
        }

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

        return GetNamedPipeTarget(TrimScheme(uri));
    }

    private static string GetNamedPipeTarget(string pipeName)
    {
        return @"\\.\" + Path.Combine("pipe", pipeName);
    }
}

#endif
