using Grpc.Core;

namespace Spiffe.Sample.Grpc.Mtls.Services;

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
