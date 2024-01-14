using CommandLine;
using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Client;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

var parserResult = Parser.Default.ParseArguments<X509Command, BundleCommand>(args);
if (parserResult.Errors.Any())
{
    var err = string.Join(", ", parserResult.Errors);
    Console.WriteLine($"Failed to parse command: {err}");
    Environment.Exit(1);
    return;
}

var opts = parserResult.Value;
var address = (opts as Options)?.Address;
if (string.IsNullOrEmpty(address))
{
    Console.WriteLine($"Address must be non-empty");
    Environment.Exit(1);
    return;
}

using GrpcChannel channel = GrpcChannelFactory.CreateChannel(address);
IWorkloadApiClient client = WorkloadApiClient.Create(channel);
if (opts is X509Command)
{
    X509Context x509Context = await client.FetchX509ContextAsync();
    Printer.Print(x509Context);
}
else if (opts is BundleCommand)
{
    X509BundleSet x509Bundles = await client.FetchX509BundlesAsync();
    Printer.Print(x509Bundles);
}
else
{
    throw new NotSupportedException("Unknown command");
}
