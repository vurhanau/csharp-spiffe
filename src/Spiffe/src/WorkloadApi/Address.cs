using System.Net;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Class to operate with Workload API address.
/// </summary>
public static partial class Address
{
    private const string SchemeTcp = "tcp";

    /// <summary>
    /// ValidateAddress validates that the provided address
    /// can be parsed to a gRPC target string for dialing
    /// a Workload API endpoint exposed as either a Unix
    /// Domain Socket or TCP socket.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="address"/> is not a valid GRPC target</exception>
    public static void Validate(string address)
    {
        _ = ParseTarget(address);
    }

    /// <summary>
    /// Parses the endpoint address and returns a gRPC target string for dialing.
    /// <br/>
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="address"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="address"/> is not a valid GRPC target</exception>
    internal static string ParseTarget(string address)
    {
        _ = address ?? throw new ArgumentNullException(nameof(address));

        Uri uri;
        try
        {
            uri = new(address);
        }
        catch (UriFormatException e)
        {
            throw new ArgumentException("Workload endpoint tcp socket URI format could not be determined", e);
        }

        if (!SchemeTcp.Equals(uri.Scheme, StringComparison.Ordinal))
        {
            return ParseNativeTarget(uri);
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include user info");
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must include a host");
        }

        if (!string.IsNullOrEmpty(uri.PathAndQuery) && uri.PathAndQuery != "/")
        {
            throw new ArgumentException("Workload endpoint tcp socket URI must not include path and query values");
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
    private static partial string ParseNativeTarget(Uri uri);

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
