using CommandLine;
using Spiffe.Client;

Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
{
    Console.WriteLine($"Arguments: {o}");
});
