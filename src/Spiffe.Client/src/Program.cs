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

var socket = (parserResult.Value as Options)?.Socket;
if (string.IsNullOrEmpty(socket))
{
    Console.WriteLine($"Socket must be non-empty");
    Environment.Exit(1);
    return;
}

using var c = Client.GetClient(socket);

await (parserResult.Value switch
{
    X509Command => c.FetchX509Context(),
    BundleCommand => c.FetchX509Bundles(),
    WatchCommand => Task.WhenAll(c.WatchX509Context(), c.WatchX509Bundles()),
    _ => throw new NotSupportedException("Unknown command"),
});
