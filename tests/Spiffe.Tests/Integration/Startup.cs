using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Spiffe.Tests.Integration.Server;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc(o => o.EnableDetailedErrors = true);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<WorkloadApiServer>();
        });
    }
}
