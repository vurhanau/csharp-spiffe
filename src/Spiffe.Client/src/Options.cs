using CommandLine;

namespace Spiffe.Client;

public class Options
{
    [Option("address", Required = false, HelpText = "Agent API address.")]
    public string? Address { get; set; }
}
