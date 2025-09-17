using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spiffe.Bundle.X509;
using Spiffe.Grpc;
using Spiffe.Sample.Grpc.Tls;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;
using static Spiffe.Sample.Grpc.Tls.Greeter;

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

GrpcChannel workloadChannel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");
IWorkloadApiClient workload = WorkloadApiClient.Create(workloadChannel, logger);
IX509BundleSource x509Source = await BundleSource.CreateAsync(workload, timeoutMillis: 60000);

using GrpcChannel channel = GrpcChannel.ForAddress("https://server:5000", new GrpcChannelOptions()
{
    HttpHandler = new SocketsHttpHandler()
    {
        SslOptions = SpiffeSslConfig.GetTlsClientOptions(x509Source),
    },
    DisposeHttpClient = true,
});

GreeterClient client = new(channel);
while (true)
{
    HelloReply reply = await client.SayHelloAsync(new HelloRequest());
    logger.LogInformation("Response: {Message}", reply.Message);
    await Task.Delay(5000);
}
