using Grpc.Net.Client;
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
X509Source x509Source = await X509Source.CreateAsync(workload);

using HttpClient http = new(new SocketsHttpHandler()
{
    SslOptions = SpiffeSslConfig.GetMtlsClientOptions(x509Source, Authorizers.AuthorizeAny()),
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string clientCertificate = x509Source.GetX509Svid().Certificates[0].ToString(true);
app.Logger.LogInformation("Client certificate:\n {Cert}", clientCertificate);

app.MapGet("/", async () =>
{
    HttpResponseMessage r = await http.GetAsync(serverUrl);
    string str = await r.Content.ReadAsStringAsync();
    return str;
});

app.Run(clientUrl);
