## GRPC - mTLS with X509 SVID

This sample demonstrates the use of [X509-SVID](https://github.com/spiffe/spiffe/blob/main/standards/X509-SVID.md) to establish mTLS connection between .NET GRPC server and client.

## Prerequisites

*   A running SPIRE server and agent. You can use the setup in the `samples/docker-spire` directory.
*   The trust domain configured in SPIRE must be `example.org`.
*   The following SPIRE registration entries are required. Replace placeholders like `<PARENT_ID_FOR_SERVER_AGENT>` and `<SELECTOR_FOR_SERVER>` with appropriate values for your SPIRE agent deployment. For example, if using Docker, a selector might be `docker:label:app:my-grpc-server`. Refer to the [SPIRE documentation on registration](https://spiffe.io/docs/latest/spire/using/registration/) for more details.

    ```bash
    # For the gRPC server (replace <PARENT_ID_FOR_SERVER_AGENT> and <SELECTOR_FOR_SERVER>)
    # The server entry includes a DNS name (my-grpc-server.example.org) which will be part of its X.509 SVID.
    # This DNS name is used by the client to verify the server's identity during the mTLS handshake.
    spire-server entry create \
        -spiffeID spiffe://example.org/my-grpc-server \
        -parentID <PARENT_ID_FOR_SERVER_AGENT> \
        -selector <SELECTOR_FOR_SERVER> \
        -dns my-grpc-server.example.org

    # For the gRPC client (replace <PARENT_ID_FOR_CLIENT_AGENT> and <SELECTOR_FOR_CLIENT>)
    # Example selector for Docker: docker:label:app:my-grpc-client
    spire-server entry create \
        -spiffeID spiffe://example.org/my-grpc-client \
        -parentID <PARENT_ID_FOR_CLIENT_AGENT> \
        -selector <SELECTOR_FOR_CLIENT>
    ```

## Running the Sample

You can run this sample using Docker Compose (recommended for ease of setup with SPIRE) or locally.

### Using Docker Compose

This method runs the sample applications along with a SPIRE server and agent.

1.  Navigate to the main `samples` directory:
    ```bash
    cd ../.. 
    # (Assuming you are currently in samples/Spiffe.Sample.Grpc.Mtls)
    # Or directly: cd <path_to_repo>/samples
    ```

2.  Set the `SAMPLE_DIR` environment variable to this sample's directory:
    ```bash
    export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls
    ```

3.  Run Docker Compose:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-mtls up --build -d
    ```
    This will build the images and start the client, server, and SPIRE services in detached mode.

4.  To view logs:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-mtls logs -f server client
    ```

5.  To stop and remove the services:
    ```bash
    docker-compose -f compose.yaml -p spiffe-grpc-mtls down
    ```

### Running Locally (Example)

This method assumes you have a SPIRE agent running separately and its Workload API is accessible.

1.  **Ensure SPIRE Agent is Running**:
    *   The SPIRE agent must be running and configured.
    *   The `SPIFFE_ENDPOINT_SOCKET` environment variable must be set to point to your agent's Workload API socket.
        *   On Linux/macOS: `export SPIFFE_ENDPOINT_SOCKET=unix:///tmp/spire/agent/public/api.sock` (adjust path if necessary)
        *   On Windows: `set SPIFFE_ENDPOINT_SOCKET=npipe:////./pipe/spire-agent/public/api` (adjust path if necessary)

2.  **Run the Server:**
    *   Navigate to the server's directory:
        ```bash
        cd samples/Spiffe.Sample.Grpc.Mtls/Server
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
        cd samples/Spiffe.Sample.Grpc.Mtls/Client
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
*   It receives requests from the client.
*   During the mTLS handshake, the client's X.509 SVID is validated, and the server logs should show the client's SPIFFE ID.

Example snippet:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Spiffe.Sample.Grpc.Mtls.Server.Services.GreeterService[0]
      Processing 'SayHello' request from caller 'spiffe://example.org/my-grpc-client'
```

### Client Logs

The client logs should show:
*   It successfully fetches an X.509 SVID from the SPIRE Workload API for its own SPIFFE ID (e.g., `spiffe://example.org/my-grpc-client`).
*   It periodically makes gRPC calls to the server (e.g., to `https://my-grpc-server.example.org:7001` or `https://localhost:7001`).
*   It receives successful responses from the server, indicating a successful mTLS connection.

Example snippet:
```
info: Spiffe.Sample.Grpc.Mtls.Client.Worker[0]
      Client SVID: spiffe://example.org/my-grpc-client
info: Spiffe.Sample.Grpc.Mtls.Client.Worker[0]
      Calling gRPC server...
info: Spiffe.Sample.Grpc.Mtls.Client.Worker[0]
      Server response: Hello spiffe://example.org/my-grpc-client
```
(Note: If running locally and not using DNS `my-grpc-server.example.org` for the server, the client might connect to `localhost`, but the server's SVID will still ideally contain `spiffe://example.org/my-grpc-server` and the DNS name `my-grpc-server.example.org` for proper validation if the client is configured to check it.)
