using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Spiffe.Grpc;
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
using GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);
using X509Source x509Source = await X509Source.CreateAsync(workload,
                                                           timeoutMillis: 60_000,
                                                           cancellationToken: close.Token);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Any, port: 5000, listenOptions =>
    {
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            OnConnection = ctx => ValueTask.FromResult(
                SpiffeSslConfig.GetMtlsServerOptions(x509Source, Authorizers.AuthorizeAny())),
        });
    });
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);
app.MapGet("/", async (HttpContext ctx) =>
{
    string caller = ctx.Connection.ClientCertificate?.GetNameInfo(X509NameType.UrlName, false) ?? "unknown";
    app.Logger.LogInformation("Request from '{Caller}'", caller);
    await ctx.Response.WriteAsync($"Hello, {caller}");
});

await app.RunAsync();
