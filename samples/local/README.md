## C# SPIFFE Samples - Local

This sample is using running locally 

1. Install Spire ([doc](https://spiffe.io/docs/latest/deploying/install-server/)):
    ```bash
    wget https://github.com/spiffe/spire/releases/download/v1.12.5/spire-1.12.5-linux-amd64-musl.tar.gz
    tar zvxf spire-1.12.5-linux-amd64-musl.tar.gz
    SPIRE_DIR=$(realpath spire-1.12.5)
    sudo ln -s ${SPIRE_DIR}/bin/spire-server /usr/local/bin/spire-server
    sudo ln -s ${SPIRE_DIR}/bin/spire-agent /usr/local/bin/spire-agent
    ```

2. Start server:
    ```bash
    /usr/local/bin/spire-server run -config conf/server/server.conf
    /usr/local/bin/spire-server entry create \
        -parentID spiffe://example.org/myagent \
        -spiffeID spiffe://example.org/myservice \
        -selector unix:uid:$(id -u)
    ```

3. Start agent:
    ```bash
    join_token=$(/usr/local/bin/spire-server token generate -spiffeID spiffe://example.org/myagent | sed 's/Token: //')
    /usr/local/bin/spire-agent run -config conf/agent/agent.conf -joinToken ${join_token}
    ```

4. Check if X509 certificate is ready:
    ```bash
    /usr/local/bin/spire-agent api fetch x509
    ```

5. Run sample server:
    ```bash
    dotnet run --project Spiffe.Sample.Grpc.Mtls/Server
    ```

6. Run sample client:
    ```bash
    dotnet run --project Spiffe.Sample.Grpc.Mtls/Client
    ```

7. Expected output

    Server:
    ```
    info: Program[0]
        Connecting to agent grpc channel
    info: Program[0]
        Creating workloadapi client
    info: Program[0]
        Creating x509 source
    info: Microsoft.Hosting.Lifetime[14]
        Now listening on: https://0.0.0.0:6000
    info: Microsoft.Hosting.Lifetime[0]
        Application started. Press Ctrl+C to shut down.
    info: Microsoft.Hosting.Lifetime[0]
        Hosting environment: Production
    info: Microsoft.Hosting.Lifetime[0]
        Content root path: /Users/avurganov/Projects/csharp-spiffe/samples/local/Spiffe.Sample.Grpc.Mtls/Server
    info: Spiffe.Sample.Grpc.Mtls.Services.GreetService[0]
        Request from 'spiffe://example.org/myservice'
    ```
    Client:
    ```
    info: Program[0]
        Connecting to agent grpc channel
    info: Program[0]
        Creating workloadapi client
    info: Program[0]
        Response: Hello, spiffe://example.org/myservice
    ```
