using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Spiffe.Tests.Server;

int port = 5001;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(k => k.Listen(IPAddress.Any, port, opts => opts.Protocols = HttpProtocols.Http2));
builder.Services.AddGrpc();
WebApplication app = builder.Build();
app.MapGrpcService<WorkloadApiService>();
app.Run();
