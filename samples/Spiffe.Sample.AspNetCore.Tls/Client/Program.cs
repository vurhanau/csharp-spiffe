using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Grpc;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;

string clientUrl = "http://localhost:5000";
string serverUrl = "https://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
GrpcChannel channel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(channel);
IX509BundleSource x509BundleSource = await BundleSource.CreateAsync(workload);

using HttpClient http = new(new SocketsHttpHandler()
{
    SslOptions = SpiffeSslConfig.GetTlsClientOptions(x509BundleSource, Authorizers.AuthorizeAny()),
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

app.MapGet("/", async () =>
{
    HttpResponseMessage r = await http.GetAsync(serverUrl);
    string str = await r.Content.ReadAsStringAsync();
    return str;
});

app.Run(clientUrl);
