using System.Net.Http.Headers;
using Grpc.Net.Client;
using Spiffe.Grpc;
using Spiffe.Svid.Jwt;
using Spiffe.Util;
using Spiffe.WorkloadApi;

string clientUrl = "http://localhost:5000";
string serverUrl = "http://localhost:5001";
string spiffeAddress = "unix:///tmp/spire-agent/public/api.sock";
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Spiffe client
using CancellationTokenSource close = new();
GrpcChannel channel = GrpcChannelFactory.CreateChannel(spiffeAddress);
IWorkloadApiClient workload = WorkloadApiClient.Create(channel);
JwtSource jwtSource = await JwtSource.CreateAsync(workload);

using HttpClient http = new();

WebApplication app = builder.Build();
app.Lifetime.ApplicationStopped.Register(close.Cancel);

string audience = "spiffe://example.org/myservice";
app.MapGet("/", async () =>
{
    List<JwtSvid> svids = await jwtSource.FetchJwtSvidsAsync(new JwtSvidParams(
        audience: audience,
        extraAudiences: [],
        subject: null));

    JwtSvid svid = svids[0];
    HttpRequestMessage req = new()
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(serverUrl),
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", svid.Token);
    app.Logger.LogInformation("JWT SVID:\n{Svid}", Strings.ToString(svid));
    HttpResponseMessage resp = await http.SendAsync(req);
    string str = await resp.Content.ReadAsStringAsync();
    return Results.Text(str, statusCode: (int)resp.StatusCode);
});

await app.RunAsync(clientUrl);
