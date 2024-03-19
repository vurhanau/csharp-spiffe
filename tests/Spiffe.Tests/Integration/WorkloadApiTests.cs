using FluentAssertions;
using Grpc.Core;
using Server;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;
using Tests.Server.IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace Tests.Server.IntegrationTests;

public class TestWorkloadApiIntegration(GrpcTestFixture<Startup> fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task TestFetchJWTSVID()
    {
        var c = WorkloadApiClient.Create(Channel);

        var ret = await c.FetchJwtSvidsAsync(new JwtSvidParams(
            audience: "foo",
            extraAudiences: [],
            subject: null));

        ret.Should().ContainSingle();
    }
}
