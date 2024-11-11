using System.Security.Claims;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Spiffe.AspNetCore.Server;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

string serverUrl = Environment.GetEnvironmentVariable("SERVER_URL")
                                        ?? throw new ArgumentException("SERVER_URL must be set", nameof(args));
string serverAudience = Environment.GetEnvironmentVariable("SERVER_AUDIENCE")
                                        ?? throw new ArgumentException("SERVER_AUDIENCE must be set", nameof(args));
string agentAddress = Environment.GetEnvironmentVariable("SPIRE_AGENT_ADDRESS")
                                        ?? throw new ArgumentException("SPIRE_AGENT_ADDRESS must be set", nameof(args));
string log = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
if (!Enum.TryParse(log, true, out LogLevel logLevel))
{
    logLevel = LogLevel.Information;
}

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(logLevel));
ILogger logger = factory.CreateLogger<Program>();
logger.LogInformation(@"
    Configuration:
        SERVER_URL={ServerUrl}
        SPIRE_AGENT_ADDRESS={AgentAddress}
        SERVER_AUDIENCE={ServerAudience}
        LOG_LEVEL={LogLevel}",
        serverUrl,
        agentAddress,
        serverAudience,
        log);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
using CancellationTokenSource close = new();

logger.LogDebug("Connecting to agent grpc channel");
using GrpcChannel channel = GrpcChannelFactory.CreateChannel(agentAddress);

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
    o.TokenHandlers.Add(new JwtSvidTokenHandler(jwtSource, serverAudience));
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

await app.RunAsync(serverUrl);
