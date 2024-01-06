using System.Net;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
public static partial class Address
{
    private const string SchemeTcp = "tcp";

    /// <summary>
    /// Parses the endpoint address and returns a gRPC target string for dialing.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="uri"/> is null
    /// </exception>
    public static string ParseTarget(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (SchemeTcp.Equals(uri.Scheme, StringComparison.Ordinal))
        {
            return ParseNativeTarget(uri);
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include user info");
        }

        if (!string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must include a host");
        }

        if (!string.IsNullOrEmpty(uri.AbsolutePath))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include a path");
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include query values");
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include a fragment");
        }

        bool isValidIp = IPAddress.TryParse(uri.Host, out IPAddress? ip);
        if (!isValidIp)
        {
            throw new ArgumentException("Workload endpoint tcp socket URI host component must be an IP:port");
        }

        int port = uri.Port;
        if (port == -1)
        {
            throw new ArgumentException("Workload endpoint tcp socket URI host component must include a port");
        }

        return JoinHostPort(ip!.ToString(), port);
    }

    /// <summary>
    /// Parses the endpoint address and returns a gRPC OS specific target.
    /// </summary>
    internal static partial string ParseNativeTarget(Uri uri);

    /// <summary>
    /// JoinHostPort combines host and port into a network address of the
    /// form "host:port". If host contains a colon, as found in literal
    /// IPv6 addresses, then JoinHostPort returns "[host]:port".
    /// <br/>
    /// See <seealso href="https://github.com/golang/go/blob/1fde99cd6eff725f5cc13748a43b4aef3de557c8/src/net/ipsock.go#L235"/>
    /// </summary>
    private static string JoinHostPort(string host, int port)
    {
        // Assuming that host is a literal IPv6 address if host has colons.
        if (host.Contains(':'))
        {
            return "[" + host + "]:" + port;
        }

        return host + ":" + port;
    }
}
