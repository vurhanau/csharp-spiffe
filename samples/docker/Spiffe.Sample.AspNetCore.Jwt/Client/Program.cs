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
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));
ILogger logger = factory.CreateLogger<Program>();

logger.LogInformation("Connecting to agent grpc channel");
GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/spire/agent/public/api.sock");

logger.LogInformation("Creating workloadapi client");
IWorkloadApiClient workload = WorkloadApiClient.Create(channel, logger);

logger.LogInformation("Creating jwt source");
JwtSource jwtSource = await JwtSource.CreateAsync(workload, timeoutMillis: 60_000);

using HttpClient http = new();

while (true)
{
    try
    {
        List<JwtSvid> svids = await jwtSource.FetchJwtSvidsAsync(new JwtSvidParams(
                                                            audience: "spiffe://example.org/server",
                                                            extraAudiences: [],
                                                            subject: null));

        JwtSvid svid = svids[0];
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
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred");
    }
    finally
    {
        await Task.Delay(5000);
    }
}
