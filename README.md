# C# SPIFFE Library
[![codecov](https://codecov.io/gh/vurhanau/csharp-spiffe/graph/badge.svg?token=7T5FW25DYR)](https://codecov.io/gh/vurhanau/csharp-spiffe)

## Overview

The C# SPIFFE library provides functionality to interact with the Workload API to fetch X.509 and JWT SVIDs and Bundles.

C# implementation of [spiffe/go-spiffe](https://github.com/spiffe/go-spiffe).

Requires .NET8.

[NuGet Package](https://www.nuget.org/packages/Spiffe/)

> [!IMPORTANT]
>  This is a prerelease version and it's not ready for use in production.

## Quick Start

Start [SPIRE](https://spiffe.io/spire/) or another SPIFFE Workload API
   implementation.

To create an mTLS Kestrel server:

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();
using GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/agent.sock");
IWorkloadApiClient client = WorkloadApiClient.Create(client);
using X509Source x509Source = await X509Source.CreateAsync(workload);
builder.WebHost.UseKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Any, 8443, listenOptions =>
    {
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            // Configure mTLS server options
            OnConnection = ctx => ValueTask.FromResult(
                SpiffeSslConfig.GetMtlsServerOptions(x509Source, Authorizers.AuthorizeAny())),
        });
    });
});
```

To dial an mTLS server:

```csharp
GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/agent.sock");
IWorkloadApiClient client = WorkloadApiClient.Create(client);
X509Source x509Source = await X509Source.CreateAsync(workload);
HttpClient http = new(new SocketsHttpHandler()
{
    // Configure mTLS client options
    SslOptions = SpiffeSslConfig.GetMtlsClientOptions(x509Source, Authorizers.AuthorizeAny()),
});
```

The client and server obtain
[X509-SVIDs](https://github.com/spiffe/spiffe/blob/main/standards/X509-SVID.md)
and X.509 bundles from the [SPIFFE Workload
API](https://github.com/spiffe/spiffe/blob/main/standards/SPIFFE_Workload_API.md).
The X509-SVIDs are presented by each peer and authenticated against the X.509
bundles. Both sides continue to be updated with X509-SVIDs and X.509 bundles
streamed from the Workload API (e.g. secret rotation).

## Examples

The [samples](./samples/) directory contains examples for a variety of circumstances.
