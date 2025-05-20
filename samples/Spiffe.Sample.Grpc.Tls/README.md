## GRPC - TLS with X509 SVID

This sample demonstrates the use of [X509-SVID](https://github.com/spiffe/spiffe/blob/main/standards/X509-SVID.md) to establish TLS connection between .NET GRPC server and client.

## Prerequisites

*   A running SPIRE server and agent. You can use the setup in the `samples/docker-spire` directory.
*   The trust domain configured in SPIRE must be `example.org`.
*   The following SPIRE registration entry is required for the gRPC server. Replace placeholders like `<PARENT_ID_FOR_SERVER_AGENT>` and `<SELECTOR_FOR_SERVER>` with appropriate values for your SPIRE agent deployment. For example, if using Docker, a selector might be `docker:label:app:my-grpc-server`. Refer to the [SPIRE documentation on registration](https://spiffe.io/docs/latest/spire/using/registration/) for more details.
    *   The client in this scenario does not require its own SPIFFE ID for the TLS connection itself (it acts as a standard TLS gRPC client) but relies on the X.509 bundle fetched from the Workload API to validate the server's SVID.

    ```bash
    # For the gRPC server (replace <PARENT_ID_FOR_SERVER_AGENT> and <SELECTOR_FOR_SERVER>)
    # The server entry includes a DNS name (my-grpc-server.example.org) which will be part of its X.509 SVID.
    # This DNS name is used by the client to verify the server's identity.
    spire-server entry create \
        -spiffeID spiffe://example.org/my-grpc-server \
        -parentID <PARENT_ID_FOR_SERVER_AGENT> \
        -selector <SELECTOR_FOR_SERVER> \
        -dns my-grpc-server.example.org
    ```

## Running the Sample

You can run this sample using Docker Compose (recommended for ease of setup with SPIRE) or locally.

### Using Docker Compose

This method runs the sample applications along with a SPIRE server and agent.

1.  Navigate to the main `samples` directory:
    ```bash
    cd ../.. 
    # (Assuming you are currently in samples/Spiffe.Sample.Grpc.Tls)
    # Or directly: cd <path_to_repo>/samples
    ```

2.  Set the `SAMPLE_DIR` environment variable to this sample's directory:
    ```bash
    export SAMPLE_DIR=Spiffe.Sample.Grpc.Tls
    ```

3.  Run Docker Compose:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-tls up --build -d
    ```
    This will build the images and start the client, server, and SPIRE services in detached mode.

4.  To view logs:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-tls logs -f server client
    ```

5.  To stop and remove the services:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-tls down
    ```

### Running Locally (Example)

This method assumes you have a SPIRE agent running separately and its Workload API is accessible.

1.  **Ensure SPIRE Agent is Running**:
    *   The SPIRE agent must be running and configured.
    *   The `SPIFFE_ENDPOINT_SOCKET` environment variable must be set to point to your agent's Workload API socket (for both client and server to fetch bundle information, and for the server to fetch its SVID).
        *   On Linux/macOS: `export SPIFFE_ENDPOINT_SOCKET=unix:///tmp/spire/agent/public/api.sock` (adjust path if necessary)
        *   On Windows: `set SPIFFE_ENDPOINT_SOCKET=npipe:////./pipe/spire-agent/public/api` (adjust path if necessary)

2.  **Run the Server:**
    *   Navigate to the server's directory:
        ```bash
        cd samples/Spiffe.Sample.Grpc.Tls/Server
        ```
    *   Run the server application:
        ```bash
        dotnet run
        ```
    The server will start listening for requests, typically on `https://localhost:7001` or `https://my-grpc-server.example.org:7001` if your local host resolves that name.

3.  **Run the Client:**
    *   Open a new terminal.
    *   Ensure `SPIFFE_ENDPOINT_SOCKET` is set as in step 1.
    *   Navigate to the client's directory:
        ```bash
        cd samples/Spiffe.Sample.Grpc.Tls/Client
        ```
    *   Run the client application:
        ```bash
        dotnet run
        ```

## Expected Output

When running the sample, observe the logs from both the server and client applications.

### Server Logs

The server logs should indicate:
*   It has started and is listening for incoming gRPC connections (e.g., on `https://localhost:7001`).
*   It receives requests from the client. Since this is a standard TLS connection (not mTLS), the client's identity is not cryptographically verified by its own SVID at the TLS layer. The server will likely log it as an unauthenticated client.

Example snippet:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Spiffe.Sample.Grpc.Tls.Server.Services.GreeterService[0]
      Processing 'SayHello' request from an unauthenticated client.
```

### Client Logs

The client logs should show:
*   It successfully fetches the X.509 bundle from the SPIRE Workload API to trust the server's SVID.
*   It periodically makes gRPC calls to the server (e.g., to `https://my-grpc-server.example.org:7001` or `https://localhost:7001`).
*   It validates the server's X.509 SVID against the fetched bundle, ensuring the server is authentic.
*   It receives successful responses from the server.

Example snippet:
```
info: Spiffe.Sample.Grpc.Tls.Client.Worker[0]
      Client is configured to trust bundles from SPIFFE Workload API to validate the server.
info: Spiffe.Sample.Grpc.Tls.Client.Worker[0]
      Calling gRPC server...
info: Spiffe.Sample.Grpc.Tls.Client.Worker[0]
      Server response: Hello Unauthenticated Caller, from server spiffe://example.org/my-grpc-server
```
(Note: If running locally and not using DNS `my-grpc-server.example.org` for the server, the client might connect to `localhost`. The server's SVID should still ideally contain the DNS name `my-grpc-server.example.org` and SPIFFE ID `spiffe://example.org/my-grpc-server`, which the client will validate.)
