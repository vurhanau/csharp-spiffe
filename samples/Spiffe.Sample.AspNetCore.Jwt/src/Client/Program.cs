using System.Net.Http.Headers;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spiffe.Grpc;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;

string serverUrl = Environment.GetEnvironmentVariable("SERVER_URL")
                                    ?? throw new ArgumentException("SERVER_URL must be set", nameof(args));
string serverAudience = Environment.GetEnvironmentVariable("SERVER_AUDIENCE")
                                    ?? throw new ArgumentException("SERVER_AUDIENCE must be set", nameof(args));
string agentAddress = Environment.GetEnvironmentVariable("SPIRE_AGENT_ADDRESS")
                                    ?? throw new ArgumentException("SPIRE_AGENT_ADDRESS must be set", nameof(args));
string log = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
if (!Enum.TryParse(log, true, out LogLevel logLevel))
{
    logLevel = LogLevel.Information;
}

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(logLevel));
ILogger logger = factory.CreateLogger<Program>();
logger.LogInformation(@"
    Configuration:
        SERVER_URL={ServerUrl}
        SPIRE_AGENT_ADDRESS={AgentAddress}
        SERVER_AUDIENCE={ServerAudience}
        LOG_LEVEL={LogLevel}",
        serverUrl,
        agentAddress,
        serverAudience,
        log);

logger.LogDebug("Connecting to agent grpc channel");
GrpcChannel channel = GrpcChannelFactory.CreateChannel(agentAddress);

logger.LogDebug("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);

logger.LogDebug("Creating jwt source");
JwtSource jwtSource = await JwtSource.CreateAsync(workload, timeoutMillis: 60_000);

using HttpClient http = new();

while (true)
{
    logger.LogDebug("Fetching jwt svid");
    List<JwtSvid> svids = [];
    svids = await jwtSource.FetchJwtSvidsAsync(new JwtSvidParams(
        audience: serverAudience,
        extraAudiences: [],
        subject: null));

    JwtSvid svid = svids[0];
    logger.LogDebug("Jwt svid: '{Svid}'", Spiffe.Util.Strings.ToString(svid));

    HttpResponseMessage resp = await http.SendAsync(new()
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(serverUrl),
        Headers =
        {
            Authorization = new AuthenticationHeaderValue("Bearer", svid.Token),
        },
    });

    string str = await resp.Content.ReadAsStringAsync();
    logger.LogInformation("Response: {StatusCode} - {Content}", (int)resp.StatusCode, str);

    await Task.Delay(5000);
}
