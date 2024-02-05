using Grpc.Net.Client;
using GrpcCommon;
using static GrpcCommon.Greeter;

using GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:7000");
GreeterClient client = new(channel);
HelloReply reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });
Console.WriteLine($"Reply: {reply.Message}");
