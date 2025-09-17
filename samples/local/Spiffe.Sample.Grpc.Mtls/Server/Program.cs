using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spiffe.Grpc;
using Spiffe.Sample.Grpc.Mtls.Services;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

using CancellationTokenSource close = new();

logger.LogInformation("Connecting to agent grpc channel");
using GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire-agent/public/api.sock");

logger.LogInformation("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);

logger.LogInformation("Creating x509 source");
using X509Source x509Source = await X509Source.CreateAsync(workload,
                                                           timeoutMillis: 60000,
                                                           cancellationToken: close.Token);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Any, port: 6000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            OnConnection = ctx => ValueTask.FromResult(
                SpiffeSslConfig.GetMtlsServerOptions(x509Source, Authorizers.AuthorizeAny())),
        });
    });
});
builder.Services.AddLogging();
builder.Services.AddGrpc();

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);
app.MapGrpcService<GreetService>();

await app.RunAsync();
