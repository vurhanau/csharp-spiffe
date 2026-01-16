using System.Runtime.InteropServices;
using Grpc.Net.Client;
using Spiffe.WorkloadApi;

namespace Spiffe.Grpc;

/// <summary>
/// Constructs GRPC channels.
/// </summary>
public static partial class GrpcChannelFactory
{
    /// <summary>
    /// Creates GRPC channel.
    /// </summary>
    /// <param name="address">
    /// GRPC target address.
    /// <br/>
    /// <list type="bullet">
    /// <item><description>HTTP/HTTPS (example: <a href="https://1.2.3.4:5"/>)</description></item>
    /// <item><description>Unix domain socket (example: unix:///tmp/agent.sock)</description></item>
    /// <item><description>Named pipe (example: npipe:agent)</description></item>
    /// </list>
    /// </param>
    /// <param name="configureOptions">GRPC channel options configurer</param>
    public static GrpcChannel CreateChannel(string address, Action<GrpcChannelOptions>? configureOptions = null)
    {
        GrpcChannelOptions options = new();
        if (!Address.IsHttpOrHttps(address))
        {
            options.HttpHandler = CreateNativeSocketHandler(address, OSPlatformSupport.CurrentPlatform);
            address = "http://localhost";
        }

        configureOptions?.Invoke(options);

        return GrpcChannel.ForAddress(address, options);
    }

    // visible for testing
    internal static SocketsHttpHandler CreateNativeSocketHandler(string address, OSPlatform os)
    {
        if (os == OSPlatform.Windows)
        {
            return CreateNamedPipeHandler(address);
        }

        if (os == OSPlatform.Linux || os == OSPlatform.OSX || os == OSPlatform.FreeBSD)
        {
            return CreateUnixDomainSocketHandler(address);
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }
}
