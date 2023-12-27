using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.Client;

internal static class Client
{
    public static async Task Run(string address, CancellationToken cancellationToken = default)
    {
#if OS_WINDOWS
        using GrpcChannel ch = GrpcChannelFactory.CreateNamedPipeChannel(address);
#else
        using GrpcChannel ch = GrpcChannelFactory.CreateUnixSocketChannel(address);
#endif
        SpiffeWorkloadAPIClient c = new(ch);
        while (cancellationToken.IsCancellationRequested)
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
                    Console.WriteLine(r.Svids.FirstOrDefault()?.SpiffeId);
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
