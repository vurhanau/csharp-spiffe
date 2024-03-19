using Grpc.Net.Client;
using Server;
using Tests.Server.IntegrationTests.Helpers;

namespace Tests.Server.IntegrationTests;

public class IntegrationTestBase(GrpcTestFixture<Startup> fixture) : IClassFixture<GrpcTestFixture<Startup>>
{
    private GrpcChannel _channel;

    protected GrpcTestFixture<Startup> Fixture { get; set; } = fixture;

    protected GrpcChannel Channel => _channel ??= CreateChannel();

    protected GrpcChannel CreateChannel()
    {
        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = Fixture.Handler,
        });
    }

    public void Dispose()
    {
        _channel = null;
    }
}
