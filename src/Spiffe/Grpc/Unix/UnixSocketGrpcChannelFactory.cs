using System.Net.Sockets;
using Grpc.Net.Client;

namespace Spiffe.Grpc.Unix;

public class UnixSocketGrpcChannelFactory
{
    public static GrpcChannel CreateChannel(string socketPath)
    {
        _ = socketPath ?? throw new ArgumentNullException(nameof(socketPath));

        var udsEndPoint = new UnixDomainSocketEndPoint(socketPath);
        var connectionFactory = new UnixSocketConnectionFactory(udsEndPoint);
        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync
        };

        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
            UnsafeUseInsecureChannelCallCredentials = true,
        });
    }
}