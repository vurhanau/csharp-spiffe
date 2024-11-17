using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Spiffe.Sample.Grpc.Tls.Services;

public class GreetService : Greeter.GreeterBase
{
    private readonly ILogger<GreetService> _logger;

	public GreetService(ILogger<GreetService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Request from caller");

        return Task.FromResult(new HelloReply
        {
            Message = "Hello",
        });
    }
}
