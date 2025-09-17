using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
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

logger.LogInformation("Connecting to agent grpc channel");
GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");

logger.LogInformation("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);

logger.LogInformation("Creating x509 source");
X509Source x509Source = await X509Source.CreateAsync(workload, timeoutMillis: 60_000);

using HttpClient http = new(new SocketsHttpHandler()
{
    SslOptions = SpiffeSslConfig.GetTlsClientOptions(x509Source),
});

while (true)
{
    HttpResponseMessage resp = await http.GetAsync("https://server:5000");
    string str = await resp.Content.ReadAsStringAsync();
    logger.LogInformation("Response: {StatusCode} - {Content}", (int)resp.StatusCode, str);

    await Task.Delay(5000);
}
