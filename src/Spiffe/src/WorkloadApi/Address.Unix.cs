#if !OS_WINDOWS
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Spiffe.Tests")]

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
internal static partial class Address
{
    /// <summary>
    /// Parses the endpoint address and returns a gRPC Unix domain socket target.
    /// </summary>
    internal static string ParseUnixSocketTarget(string address)
    {
        Uri uri = new(address);
        if (!IsUnixSocket(uri))
        {
            throw new ArgumentException("Workload endpoint socket URI must have a supported scheme");
        }

        if (IsOpaque(uri))
        {
            throw new ArgumentException("Workload endpoint unix socket URI must not be opaque");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Workload endpoint unix socket URI must not include user info");
        }

        if (string.IsNullOrEmpty(uri.Host) && uri.PathAndQuery == "/")
        {
            throw new ArgumentException("Workload endpoint unix socket URI must include a path");
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException("Workload endpoint unix socket URI must not include query values");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Workload endpoint unix socket URI must not include a fragment");
        }

        return TrimScheme(uri);
    }

    private static bool IsUnixSocket(Uri uri)
    {
        return "unix".Equals(uri.Scheme, StringComparison.Ordinal);
    }
}

#endif
