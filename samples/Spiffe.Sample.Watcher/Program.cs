using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spiffe.Bundle.Jwt;
using Spiffe.Grpc;
using Spiffe.Id;
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

string? address = "unix:///tmp/spire-agent/public/api.sock";

using CancellationTokenSource appStopped = new();
Console.CancelKeyPress += (_, _) =>
{
    logger.LogInformation("Stopping...");
    appStopped.Cancel();
    Thread.Sleep(3000);
};

using GrpcChannel channel = GrpcChannelFactory.CreateChannel(address);
IWorkloadApiClient client = WorkloadApiClient.Create(channel, logger);

Task watchX509 = Task.Run(() => client.WatchX509ContextAsync(new Watcher<X509Context>(
    x509Context =>
    {
        foreach (X509Svid svid in x509Context.X509Svids)
        {
            string svidString = Strings.ToString(svid);
            logger.LogInformation("SVID updated for '{Id}':\n{Svid}\n", svid.Id, svidString);
        }
    },
    err =>
    {
        logger.LogWarning(err, "X509 context watch error");
    }),
    appStopped.Token));
Task watchJwt = Task.Run(() => client.WatchJwtBundlesAsync(new Watcher<JwtBundleSet>(
    jwtBundles =>
    {
        foreach ((TrustDomain td, JwtBundle b) in jwtBundles.Bundles)
        {
            string bundleString = Strings.ToString(b);
            logger.LogInformation("JWT bundle updated '{Td}': {Bundle}", b.TrustDomain, bundleString);
        }
    },
    err =>
    {
        logger.LogWarning(err, "JWT bundle watch error");
    }),
    appStopped.Token));

await Task.WhenAll(watchX509, watchJwt);
