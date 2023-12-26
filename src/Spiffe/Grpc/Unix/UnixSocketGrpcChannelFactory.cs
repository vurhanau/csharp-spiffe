#pragma warning disable SA1633 // File should have header
using System.Net.Sockets;
#pragma warning restore SA1633 // File should have header
using Grpc.Net.Client;

namespace Spiffe.Grpc.Unix;

public static class UnixSocketGrpcChannelFactory
{
    public static GrpcChannel CreateChannel(string socketPath)
    {
        _ = socketPath ?? throw new ArgumentNullException(nameof(socketPath));

        var udsEndPoint = new UnixDomainSocketEndPoint(socketPath);
        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (ignored, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(udsEndPoint, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };

        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
            UnsafeUseInsecureChannelCallCredentials = true,
        });
    }
}
