using Grpc.Net.Client;
using Spiffe.Grpc;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;

string frontendUrl = "http://localhost:5000";
string backendUrl = "https://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
GrpcChannel channel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(channel);
X509Source x509Source = await X509Source.CreateAsync(workload);

using HttpClient http = new(new SocketsHttpHandler()
{
    SslOptions = SpiffeSslConfig.GetMtlsClientOptions(x509Source),
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string backendCertificate = x509Source.GetX509Svid().Certificates[0].ToString(true);
app.Logger.LogInformation("Frontend certificate:\n {}", backendCertificate);

app.MapGet("/", async () =>
{
    HttpResponseMessage r = await http.GetAsync(backendUrl);
    string str = await r.Content.ReadAsStringAsync();
    return str;
});

app.Run(frontendUrl);
