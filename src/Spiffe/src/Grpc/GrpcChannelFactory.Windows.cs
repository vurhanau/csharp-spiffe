#if OS_WINDOWS

using System.IO.Pipes;
using System.Security.Principal;
using Spiffe.WorkloadApi;

namespace Spiffe.Grpc;

/// <summary>
/// Constructs GRPC channels.
/// </summary>
public static partial class GrpcChannelFactory
{
    internal static partial SocketsHttpHandler CreateNativeSocketHandler(string address)
    {

    }

    /// <summary>
    /// Creates a socket handler backed by Windows named pipe.
    /// See <seealso href="https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-namedpipes?view=aspnetcore-8.0"/>
    /// </summary>
    internal static partial SocketsHttpHandler CreateNativeSocketHandler(string address)
    {
        string pipeName = Address.ParseNamedPipeTarget(address);
        return new SocketsHttpHandler
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
            },
        };
    }
}

#endif
