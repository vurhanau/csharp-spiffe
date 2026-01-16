using System.Runtime.InteropServices;
using Spiffe.Grpc;

namespace Spiffe.Tests.Grpc;

public class TestGrpcChannelFactory
{
    [Theory]
    [InlineData("npipe:workload-api", nameof(OSPlatform.Linux))]
    [InlineData("npipe:workload-api", nameof(OSPlatform.FreeBSD))]
    [InlineData("npipe:workload-api", nameof(OSPlatform.OSX))]
    [InlineData("unix:/tmp/socket.sock", nameof(OSPlatform.Windows))]
    public void TestCreateNativeSocketHandlerThrowsArgumentException(string address, string osPlatformName)
    {
        OSPlatform os = OSPlatform.Create(osPlatformName);
        ArgumentException err = Assert.Throws<ArgumentException>(() =>
            GrpcChannelFactory.CreateNativeSocketHandler(address, os));
        Assert.Equal("Workload endpoint socket URI must have a supported scheme", err.Message);
    }

    [Theory]
    [InlineData("npipe:workload-api")]
    [InlineData("unix:/tmp/socket.sock")]
    public void TestCreateNativeSocketHandlerThrowsPlatformNotSupported(string address)
    {
        var os = OSPlatform.Create("MyOS");
        Assert.Throws<PlatformNotSupportedException>(() =>
            GrpcChannelFactory.CreateNativeSocketHandler(address, os));
    }
}
