#if !OS_WINDOWS

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
public static partial class Address
{
    private const string SchemeUnixSocket = "unix";

    /// <summary>
    /// Parses the endpoint address and returns a gRPC Unix domain socket target.
    /// </summary>
    internal static partial string ParseNativeTarget(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (!SchemeUnixSocket.Equals(uri.Scheme, StringComparison.Ordinal))
        {
            throw new ArgumentException("Workload endpoint socket URI must have a \"tcp\" or \"unix\" scheme");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Workload endpoint unix socket URI must not include user info");
        }

        if (string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.AbsolutePath))
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

        return uri.ToString();
    }
}

#endif
