using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

string serverUrl = "http://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
using GrpcChannel channel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(channel);
using JwtSource jwtSource = await JwtSource.CreateAsync(workload);

string audience = "spiffe://example.org/myservice";
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.TokenHandlers.Clear();
    o.TokenHandlers.Add(new JwtSvidTokenHandler(jwtSource, audience));
});

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

app.MapGet("/", (ClaimsPrincipal principal) =>
{
    string claimString = string.Join("\n", principal.Claims.Select(c => c.ToString()));
    app.Logger.LogInformation("Principal:\n{}", claimString);
    return "Hello world!";
}).RequireAuthorization();

app.Run(serverUrl);
