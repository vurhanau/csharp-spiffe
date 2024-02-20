using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Grpc;
using Spiffe.Sample.Grpc.Tls;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;
using static Spiffe.Sample.Grpc.Tls.Greeter;

string clientUrl = "http://localhost:5000";
string serverUrl = "https://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
GrpcChannel workloadChannel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(workloadChannel);
IX509BundleSource x509Source = await BundleSource.CreateAsync(workload);

using GrpcChannel channel = GrpcChannel.ForAddress(serverUrl, new GrpcChannelOptions()
{
    HttpHandler = new SocketsHttpHandler()
    {
        SslOptions = SpiffeSslConfig.GetTlsClientOptions(x509Source),
    },
    DisposeHttpClient = true,
});

GreeterClient client = new(channel);

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

app.MapGet("/", async () =>
{
    HelloReply reply = await client.SayHelloAsync(new HelloRequest { Name = "TlsGreeterClient" });
    return reply.Message;
});

app.Run(clientUrl);
