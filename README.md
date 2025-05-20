# C# SPIFFE Library
[![NuGet](https://img.shields.io/nuget/v/Spiffe.svg)](https://www.nuget.org/packages/Spiffe)
[![Alert Status](https://sonarcloud.io/api/project_badges/measure?project=vurhanau_csharp-spiffe&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=vurhanau_csharp-spiffe)
[![codecov](https://codecov.io/gh/vurhanau/csharp-spiffe/branch/main/graph/badge.svg?token=7T5FW25DYR)](https://codecov.io/gh/vurhanau/csharp-spiffe)

## Overview

The C# SPIFFE library provides functionality to interact with the Workload API to fetch X.509 and JWT SVIDs and Bundles.

C# implementation of [spiffe/go-spiffe](https://github.com/spiffe/go-spiffe).

This library requires .NET 8.0 or higher.

[NuGet Package](https://www.nuget.org/packages/Spiffe/)

## Quick Start

Start [SPIRE](https://spiffe.io/spire/) or another SPIFFE Workload API
   implementation.

To create an mTLS Kestrel server:

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();
using GrpcChannel channel = GrpcChannelFactory.CreateChannel("unix:///tmp/agent.sock");
IWorkloadApiClient client = WorkloadApiClient.Create(channel);
using X509Source x509Source = await X509Source.CreateAsync(client);
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
IWorkloadApiClient client = WorkloadApiClient.Create(channel);
X509Source x509Source = await X509Source.CreateAsync(client);
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

## API Documentation

It is highly recommended to generate HTML documentation from the XML comments in the source code using a tool like [DocFX](https://dotnet.github.io/docfx/). This improves the discoverability and usability of the API for library consumers. The generated documentation can be hosted on GitHub Pages for easy access.

Here's a high-level outline of the steps involved:

1.  **Install DocFX**: Install DocFX as a .NET tool:
    ```bash
    dotnet tool install -g docfx
    ```
2.  **Initialize DocFX Project**: Initialize a new DocFX project in your repository:
    ```bash
    docfx init -q
    ```
3.  **Configure `docfx.json`**: Modify the `docfx.json` file to specify the source code files (e.g., `src/**/*.csproj`) and any other project-specific configurations.
4.  **Build Documentation**: Build the documentation:
    ```bash
    docfx build docfx.json
    ```
5.  **Automate with GitHub Actions**: Set up a GitHub Action to automate the documentation build and deployment to the `gh-pages` branch upon pushes to the main branch.

## Examples

The [samples](https://github.com/vurhanau/csharp-spiffe/tree/main/samples) directory contains examples for a variety of circumstances.
