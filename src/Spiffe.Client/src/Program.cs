using CommandLine;
using Spiffe.Client;

var parserResult = Parser.Default.ParseArguments<X509Command, BundleCommand>(args);
if (parserResult.Errors.Any())
{
    var err = string.Join(", ", parserResult.Errors);
    Console.WriteLine($"Failed to parse command: {err}");
    Environment.Exit(1);
    return;
}

var address = (parserResult.Value as Options)?.Address;
if (string.IsNullOrEmpty(address))
{
    Console.WriteLine($"Address must be non-empty");
    Environment.Exit(1);
    return;
}

using var c = Client.GetClient(address);

await (parserResult.Value switch
{
    X509Command => c.FetchX509Context(),
    BundleCommand => c.FetchX509Bundles(),
    WatchCommand => Task.WhenAll(c.WatchX509Context(), c.WatchX509Bundles()),
    _ => throw new NotSupportedException("Unknown command"),
});
