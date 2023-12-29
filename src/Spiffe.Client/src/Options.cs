#pragma warning disable SA1402 // File may only contain a single type
using CommandLine;

namespace Spiffe.Client;

internal class Options
{
    [Option('s', "socket", Required = false, HelpText = "Agent socket path.")]
    public string? Socket { get; set; }
}

[Verb("x509", HelpText = "Command to fetch X509 SVID from Workload API.")]
internal class X509Command : Options
{
}

[Verb("bundle", HelpText = "Command to fetch X509 bundle from Workload API.")]
internal class BundleCommand : Options
{
}

[Verb("watch", HelpText = "Command to watch Workload API update stream.")]
internal class WatchCommand : Options
{
}
#pragma warning restore SA1402 // File may only contain a single type
