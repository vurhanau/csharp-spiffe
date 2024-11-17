using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Spiffe.Grpc;
using Spiffe.Svid.Jwt;
using Spiffe.WorkloadApi;

using ILoggerFactory factory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

logger.LogDebug("Connecting to agent grpc channel");
GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");

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
        audience: "spiffe://example.org/server",
        extraAudiences: [],
        subject: null));

    JwtSvid svid = svids[0];
    logger.LogDebug("Jwt svid: '{Svid}'", Spiffe.Util.Strings.ToString(svid));

    HttpResponseMessage resp = await http.SendAsync(new()
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri("http://server:5000"),
        Headers =
        {
            Authorization = new AuthenticationHeaderValue("Bearer", svid.Token),
        },
    });

    string str = await resp.Content.ReadAsStringAsync();
    logger.LogInformation("Response: {StatusCode} - {Content}", (int)resp.StatusCode, str);

    await Task.Delay(5000);
}
