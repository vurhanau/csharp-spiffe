#if !OS_WINDOWS

using System.Net.Sockets;
using Spiffe.WorkloadApi;

namespace Spiffe.Grpc;

/// <summary>
/// Constructs GRPC channels.
/// </summary>
public static partial class GrpcChannelFactory
{
    /// <summary>
    /// Creates a socket handler backed by Unix domain socket.
    /// See <seealso href="https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-uds?view=aspnetcore-8.0"/>
    /// </summary>
    /// <param name="address">Socket path URI (ex: unix:///tmp/api.sock)</param>
    private static partial SocketsHttpHandler CreateNativeSocketHandler(string address)
    {
        string socketPath = Address.ParseUnixSocketTarget(address);
        return new SocketsHttpHandler
        {
            ConnectCallback = async (_, cancellationToken) =>
            {
                UnixDomainSocketEndPoint udsEndPoint = new(socketPath);
                Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(udsEndPoint, cancellationToken)
                        .ConfigureAwait(false);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
    }
}

#endif
