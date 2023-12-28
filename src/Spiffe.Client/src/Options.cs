using CommandLine;

namespace Spiffe.Client;

public class Options
{
    [Option('s', "socket-path", Required = false, HelpText = "Agent unix socket path.")]
    public string? SocketPath { get; set; }
}
