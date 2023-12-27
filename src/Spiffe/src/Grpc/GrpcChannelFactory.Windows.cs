#if OS_WINDOWS

using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Principal;
using Grpc.Net.Client;

namespace Spiffe.Grpc;

/// <summary>
/// Constructs GRPC channels.
/// </summary>
public static partial class GrpcChannelFactory
{
    /// <summary>
    /// Creates GRPC channel over unix domain socket.
    /// </summary>
    public static GrpcChannel CreateNamedPipeChannel(string pipeName)
    {
        _ = pipeName ?? throw new ArgumentNullException(nameof(pipeName));

        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (ignored, cancellationToken) =>
            {
                // TODO: dispose
                var clientStream = new NamedPipeClientStream(
                    serverName: ".",
                    pipeName: pipeName,
                    direction: PipeDirection.InOut,
                    options: PipeOptions.WriteThrough | PipeOptions.Asynchronous,
                    impersonationLevel: TokenImpersonationLevel.Anonymous);

                try
                {
                    await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    return clientStream;
                }
                catch
                {
                    clientStream.Dispose();
                    throw;
                }
            }
        };

        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler
        });
    }
}
#endif
