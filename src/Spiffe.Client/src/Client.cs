using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Client;

internal static class Client
{
    public static async Task Run(string address, bool streaming = false, CancellationToken cancellationToken = default)
    {
        using GrpcChannel ch = GrpcChannelFactory.CreateChannel(address);
        SpiffeWorkloadAPIClient c = new(ch);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                AsyncServerStreamingCall<X509SVIDResponse> reply = c.FetchX509SVID(new X509SVIDRequest(), headers: new()
                {
                    {
                        "workload.spiffe.io", "true"
                    },
                });

                await foreach (var r in reply.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    var svid = r.Svids.FirstOrDefault();
                    Console.WriteLine("Spiffe ID: " + svid?.SpiffeId);
                    if (!streaming)
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(5000);
            }
        }
    }
}
