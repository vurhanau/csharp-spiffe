using System.Text.Json;
using CommandLine;

namespace Spiffe.Client;

public class Options
{
    [Option('s', "socket-path", Required = false, HelpText = "Agent unix socket path.")]
    public string? SocketPath { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this);
}
