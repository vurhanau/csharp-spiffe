using Grpc.Core;

namespace GrpcCommon;

public class GreetService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = $"Hello {request.Name}!",
        });
    }
}
