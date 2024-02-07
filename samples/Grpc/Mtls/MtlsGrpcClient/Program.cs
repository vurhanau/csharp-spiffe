using Grpc.Net.Client;
using GrpcCommon;
using Spiffe.Grpc;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;
using static GrpcCommon.Greeter;

string clientUrl = "http://localhost:5000";
string serverUrl = "https://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
GrpcChannel workloadChannel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(workloadChannel);
X509Source x509Source = await X509Source.CreateAsync(workload);

using GrpcChannel channel = GrpcChannel.ForAddress(serverUrl, new GrpcChannelOptions()
{
    HttpHandler = new SocketsHttpHandler()
    {
        SslOptions = SpiffeSslConfig.GetMtlsClientOptions(x509Source),
    },
    DisposeHttpClient = true,
});

GreeterClient client = new(channel);

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string clientCertificate = x509Source.GetX509Svid().Certificates[0].ToString(true);
app.Logger.LogInformation("Client certificate:\n {}", clientCertificate);

app.MapGet("/", async () =>
{
    HelloReply reply = await client.SayHelloAsync(new HelloRequest { Name = "MtlsGreeterClient" });
    return reply.Message;
});

app.Run(clientUrl);
