using System.Net;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Spiffe.Grpc;
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
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            // Configure mTLS server options
            OnConnection = ctx => ValueTask.FromResult(
                SpiffeSslConfig.GetMtlsServerOptions(x509Source)),
        });
    });
});

builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate(opts =>
                {
                    opts.RevocationMode = X509RevocationMode.NoCheck;
                    opts.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
                    var td = x509Source.GetX509Svid().Id.TrustDomain;
                    opts.CustomTrustStore = x509Source.GetX509Bundle(td).X509Authorities;
                });

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string backendCertificate = x509Source.GetX509Svid().Certificates[0].ToString(true);
app.Logger.LogInformation("Backend certificate:\n {}", backendCertificate);

app.UseAuthentication();

app.MapGet("/", () => "Hello World!");

app.Run();
