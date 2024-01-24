using CommandLine;

namespace Spiffe.Client;

internal class Options
{
    [Option("address", Required = false, HelpText = "Agent API address.")]
    public string? Address { get; set; }
}

[Verb("x509svid", HelpText = "Command to fetch X509 SVID from Workload API.")]
internal class X509Command : Options
{
}

[Verb("x509bundle", HelpText = "Command to fetch X509 bundles from Workload API.")]
internal class X509BundleCommand : Options
{
}

[Verb("x509watch", HelpText = "Command to watch Workload API X509 SVID update stream.")]
internal class WatchCommand : Options
{
}
