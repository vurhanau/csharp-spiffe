using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spiffe.Grpc;
using Spiffe.Sample.Grpc.Mtls;
using Spiffe.Ssl;
using Spiffe.WorkloadApi;
using static Spiffe.Sample.Grpc.Mtls.Greeter;

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

logger.LogInformation("Connecting to agent grpc channel");
GrpcChannel workloadChannel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");

logger.LogInformation("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(workloadChannel, logger);

logger.LogDebug("Creating x509 source");
X509Source x509Source = await X509Source.CreateAsync(workload, timeoutMillis: 60000);

using GrpcChannel channel = GrpcChannel.ForAddress("https://server:5000", new GrpcChannelOptions()
{
    HttpHandler = new SocketsHttpHandler()
    {
        SslOptions = SpiffeSslConfig.GetMtlsClientOptions(x509Source, Authorizers.AuthorizeAny()),
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
