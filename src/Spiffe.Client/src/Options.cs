using CommandLine;

namespace Spiffe.Client;

internal class Options
{
    [Option("address", Required = false, HelpText = "Agent API address.")]
    public string? Address { get; set; }
}

[Verb("x509svid", HelpText = "Command to fetch X509 SVID from Workload API.")]
internal class X509SvidCommand : Options
{
}

[Verb("x509bundle", HelpText = "Command to fetch X509 bundles from Workload API.")]
internal class X509BundleCommand : Options
{
}

[Verb("x509watch", HelpText = "Command to watch Workload API X509 SVID update stream.")]
internal class X509WatchCommand : Options
{
}

[Verb("jwtsvid", HelpText = "Command to fetch JWT SVID from Workload API.")]
internal class JwtSvidCommand : Options
{
    [Option("audience", Required = true, HelpText = "Token audience.")]
    public string? Audience { get; set; }

    [Option("subject", Required = false, HelpText = "Token subject.")]
    public string? Subject { get; set; }
}

[Verb("jwtbundle", HelpText = "Command to fetch JWT bundles from Workload API.")]
internal class JwtBundleCommand : Options
{
}

[Verb("jwtwatch", HelpText = "Command to watch Workload API JWT bundle update stream.")]
internal class JwtWatchCommand : Options
{
    [Option("trustdomain", Required = true, HelpText = "Bundle trust domain.")]
    public string? TrustDomain { get; set; }
}
