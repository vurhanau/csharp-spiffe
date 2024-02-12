using CommandLine;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spiffe.Bundle.X509;
using Spiffe.Client;
using Spiffe.Grpc;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;
using Spiffe.Util;
using Spiffe.WorkloadApi;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    IConfigurationRoot c = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json")
                                    .Build();
    builder.AddConfiguration(c.GetSection("Logging"));
    builder.AddSimpleConsole();
});

ILogger logger = loggerFactory.CreateLogger("SPIFFE");
ParserResult<object> parserResult = Parser.Default.ParseArguments<X509SvidCommand, X509BundleCommand, X509WatchCommand,
                                                                  JwtSvidCommand, JwtBundleCommand, JwtWatchCommand>(args);
if (parserResult.Errors.Any())
{
    string err = string.Join(", ", parserResult.Errors);
    logger.LogError("Failed to parse command: {}", err);
    Environment.Exit(1);
    return;
}

object opts = parserResult.Value;
string? address = (opts as Options)?.Address;
if (string.IsNullOrEmpty(address))
{
    logger.LogError("Address must be non-empty");
    Environment.Exit(1);
    return;
}

using CancellationTokenSource appStopped = new();
Console.CancelKeyPress += (_, _) =>
{
    logger.LogInformation("Stopping...");
    appStopped.Cancel();
    Thread.Sleep(3000);
};

using GrpcChannel channel = GrpcChannelFactory.CreateChannel(address);
IWorkloadApiClient client = WorkloadApiClient.Create(channel, logger);
using IX509Source x509Source = await X509Source.CreateAsync(client,
                                                            timeoutMillis: 5000,
                                                            cancellationToken: appStopped.Token);
using IJwtSource jwtSource = await JwtSource.CreateAsync(client,
                                                         timeoutMillis: 5000,
                                                         cancellationToken: appStopped.Token);
if (opts is X509SvidCommand)
{
    X509Context x509Context = await client.FetchX509ContextAsync();
    logger.LogInformation("X509 context:\n{}", Strings.ToString(x509Context));
}
else if (opts is X509BundleCommand)
{
    X509BundleSet x509Bundles = await client.FetchX509BundlesAsync();
    logger.LogInformation("X509 bundle:\n{}", Strings.ToString(x509Bundles));
}
else if (opts is X509WatchCommand)
{
    X509Svid? svid = null;
    while (true)
    {
        X509Svid newSvid = x509Source.GetX509Svid();
        if (svid != newSvid)
        {
            logger.LogInformation("New X509 SVID:\n{}", Strings.ToString(newSvid));
            svid = newSvid;
        }

        await Task.Delay(1000);
    }
}
else if (opts is JwtSvidCommand c)
{
    if (string.IsNullOrEmpty(c.Audience))
    {
        logger.LogError("JWT SVID token audience is null or empty.");
        return;
    }

    JwtSvid jwtSvid = await jwtSource.FetchJwtSvidAsync(new JwtSvidParams(
        audience: c.Audience,
        extraAudiences: [],
        subject: null));
    logger.LogInformation("JWT SVID:\n{}", Strings.ToString(jwtSvid));
}
else
{
    throw new NotSupportedException("Unknown command");
}
