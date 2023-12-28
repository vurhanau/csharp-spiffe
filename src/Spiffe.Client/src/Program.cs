using System.Text.Json;
using CommandLine;
using Spiffe.Client;

ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
if (!parserResult.Errors.Any() && parserResult.Value != null)
{
    Options options = parserResult.Value;
    LogOptions(options);

    await Client.Run(options.SocketPath!);
}

static void LogOptions(Options options)
{
    string json = JsonSerializer.Serialize(options, new JsonSerializerOptions()
    {
        WriteIndented = true,
    });
    Console.WriteLine($"Options: {json}");
}
