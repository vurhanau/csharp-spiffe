using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Grpc.Unix;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

// bind_address = "127.0.0.1"
// bind_port = "8081"
// socket_path = "/tmp/spire-server/private/api.sock"
// trust_domain = "example.org"
using GrpcChannel ch = UnixSocketGrpcChannelFactory.CreateChannel("/tmp/spire-agent/public/api.sock");
SpiffeWorkloadAPIClient c = new(ch);

while (true)
{
    try
    {
        AsyncServerStreamingCall<X509SVIDResponse> reply = c.FetchX509SVID(new X509SVIDRequest(), headers: new()
        {
            {
                "workload.spiffe.io", "true"
            },
        });

        await foreach (var r in reply.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine(r.Svids.FirstOrDefault()?.SpiffeId);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        Thread.Sleep(5000);
    }
}
