﻿#if !OS_WINDOWS

using System.Net.Sockets;
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
    public static GrpcChannel CreateUnixSocketChannel(string socketPath)
    {
        _ = socketPath ?? throw new ArgumentNullException(nameof(socketPath));

        var socketsHttpHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (ignored, cancellationToken) =>
            {
                var udsEndPoint = new UnixDomainSocketEndPoint(socketPath);
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

    /// <summary>
    /// dfdf.
    /// </summary>
    internal static partial SocketsHttpHandler CreateNativeSocketHandler(string address)
    {
        return new SocketsHttpHandler();
    }
}

#endif
