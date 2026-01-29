using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Spiffe.Sample.Grpc.Mtls.Services;

public class GreetService : Greeter.GreeterBase
{
    private readonly ILogger<GreetService> _logger;

    public GreetService(ILogger<GreetService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        string caller = context.GetHttpContext()
                               .Connection
                               .ClientCertificate?
                               .GetNameInfo(X509NameType.UrlName, false) ?? "unknown";
        _logger.LogInformation("Request from '{Caller}'", caller);

        return Task.FromResult(new HelloReply
        {
            Message = $"Hello, {caller}",
        });
    }
}
