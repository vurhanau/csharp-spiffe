using System.Linq;
using System.Security.Claims;
using System.Threading;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spiffe.AspNetCore.Jwt;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
using CancellationTokenSource close = new();

logger.LogDebug("Connecting to agent grpc channel");
using GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");

logger.LogDebug("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);

logger.LogDebug("Creating jwt source");
using JwtSource jwtSource = await JwtSource.CreateAsync(workload,
                                                        timeoutMillis: 60_000,
                                                        cancellationToken: close.Token);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.TokenHandlers.Clear();
    o.TokenHandlers.Add(new JwtSvidTokenHandler(jwtSource, "spiffe://example.org/server"));
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

app.MapGet("/", (ClaimsPrincipal principal) =>
{
    string sub = principal?
                    .Claims?
                    .FirstOrDefault(c => c.Type == "sub")?
                    .Value ?? "unknown";
    app.Logger.LogInformation("Request from '{Sub}'", sub);
    return $"Hello, {sub}";
}).RequireAuthorization();

await app.RunAsync("http://0.0.0.0:5000");
