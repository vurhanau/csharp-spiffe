using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Spiffe.Tests.Server;

if (args.Length != 1)
{
    Console.WriteLine("No address specified");
    Environment.Exit(1);
    return;
}

string addressRaw = args[0];
Uri address = new(addressRaw);
Action<KestrelServerOptions> configureKestrel = null;
if (address.Scheme == Uri.UriSchemeHttp)
{
    int port = address.Port;
    configureKestrel = k => k.Listen(IPAddress.Any, port, opts => opts.Protocols = HttpProtocols.Http2);
}
else if (address.Scheme == "unix")
{
    string unixSocketPath = address.PathAndQuery;
    Console.WriteLine($"Unix socket path is '{unixSocketPath}'");
    if (File.Exists(unixSocketPath))
    {
        File.Delete(unixSocketPath);
    }

    configureKestrel = k => k.ListenUnixSocket(unixSocketPath, opts => opts.Protocols = HttpProtocols.Http2);
}
else if (address.Scheme == "npipe")
{
    string namedPipe = address.PathAndQuery;
    Console.WriteLine($"Named pipe path is '{namedPipe}'");
    configureKestrel = k => k.ListenNamedPipe(namedPipe, opts => opts.Protocols = HttpProtocols.Http2);
}
else
{
    Console.WriteLine($"Unsupported address scheme: '{address.Scheme}'");
    Environment.Exit(1);
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(configureKestrel);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

WebApplication app = builder.Build();
app.MapGrpcService<WorkloadApiService>();
app.MapGrpcReflectionService();
app.Run();
