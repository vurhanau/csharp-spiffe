using Grpc.Core;
using Spiffe.Grpc.Unix;
using Spiffe.Workload.Grpc;

// bind_address = "127.0.0.1"
// bind_port = "8081"
// socket_path = "/tmp/spire-server/private/api.sock"
// trust_domain = "example.org"
using var ch = UnixSocketGrpcChannelFactory.CreateChannel("/tmp/spire-agent/public/api.sock");
var c = new SpiffeWorkloadAPI.SpiffeWorkloadAPIClient(ch);

while (true)
{
  try
  {
    var reply = c.FetchX509SVID(new X509SVIDRequest(), headers: new ()
    {{
        "workload.spiffe.io", "true"
    }});

    await foreach (var r in reply.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine(r.Svids.First().SpiffeId);
    }
  }
  catch (Exception e)
  {
    Console.WriteLine(e);
    Thread.Sleep(5000);
  }
}