using System.Net;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Spiffe.Grpc;
using Spiffe.Sample.Grpc.Tls.Services;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;

int port = 5001;
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
using GrpcChannel channel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(channel);
using X509Source x509Source = await X509Source.CreateAsync(workload);

builder.WebHost.UseKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Any, port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            // Configure mTLS server options
            OnConnection = ctx => ValueTask.FromResult(
                SpiffeSslConfig.GetTlsServerOptions(x509Source)),
        });
    });
});

builder.Services.AddGrpc();

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string serverCertificate = x509Source.GetX509Svid().Certificates[0].ToString(true);
app.Logger.LogInformation("Server certificate:\n {}", serverCertificate);

app.MapGrpcService<GreetService>();

app.Run();
