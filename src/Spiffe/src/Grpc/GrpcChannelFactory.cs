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
    public static GrpcChannel CreateChannel(string address)
    {
        _ = address ?? throw new ArgumentNullException(nameof(address));

        var socketsHttpHandler = CreateNativeSocketHandler(address);

        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = socketsHttpHandler,
            UnsafeUseInsecureChannelCallCredentials = true,
        });
    }

    /// <summary>
    /// Creates a platform specific socket handler.
    /// </summary>
    internal static partial SocketsHttpHandler CreateNativeSocketHandler(string address);
}
