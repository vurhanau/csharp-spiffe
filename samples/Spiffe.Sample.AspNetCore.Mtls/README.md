## ASP.NET Core - mTLS with X509 SVID

This sample demonstrates the use of [X509-SVID](https://github.com/spiffe/spiffe/blob/main/standards/X509-SVID.md) to establish mTLS connection between ASP.NET Core server and .NET HTTP client.

## Prerequisites

*   A running SPIRE server and agent. You can use the setup in the `samples/docker-spire` directory.
*   The trust domain configured in SPIRE must be `example.org`.
*   The following SPIRE registration entries are required. Replace placeholders like `<PARENT_ID_FOR_SERVER_AGENT>` and `<SELECTOR_FOR_SERVER>` with appropriate values for your SPIRE agent deployment. For example, if using Docker, a selector might be `docker:label:app:my-server`. Refer to the [SPIRE documentation on registration](https://spiffe.io/docs/latest/spire/using/registration/) for more details.

    ```bash
    # For the server (replace <PARENT_ID_FOR_SERVER_AGENT> and <SELECTOR_FOR_SERVER>)
    # The server entry includes a DNS name (my-server.example.org) which will be part of its X.509 SVID.
    # This DNS name is used by the client to verify the server's identity.
    spire-server entry create \
        -spiffeID spiffe://example.org/my-server \
        -parentID <PARENT_ID_FOR_SERVER_AGENT> \
        -selector <SELECTOR_FOR_SERVER> \
        -dns my-server.example.org

    # For the client (replace <PARENT_ID_FOR_CLIENT_AGENT> and <SELECTOR_FOR_CLIENT>)
    # Example selector for Docker: docker:label:app:my-client
    spire-server entry create \
        -spiffeID spiffe://example.org/my-client \
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
    # (Assuming you are currently in samples/Spiffe.Sample.AspNetCore.Mtls)
    # Or directly: cd <path_to_repo>/samples
    ```

2.  Set the `SAMPLE_DIR` environment variable to this sample's directory:
    ```bash
    export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Mtls
    ```

3.  Run Docker Compose:
    ```bash
    docker-compose -f compose.yaml -p spiffe-aspnetcore-mtls up --build -d
    ```
    This will build the images and start the client, server, and SPIRE services in detached mode.

4.  To view logs:
    ```bash
    docker-compose -f compose.yaml -p spiffe-aspnetcore-mtls logs -f server client
    ```

5.  To stop and remove the services:
    ```bash
    docker-compose -f compose.yaml -p spiffe-aspnetcore-mtls down
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
        cd samples/Spiffe.Sample.AspNetCore.Mtls/Server
        ```
    *   Run the server application:
        ```bash
        dotnet run
        ```
    The server will start listening for requests, typically on `https://localhost:5001` or `https://my-server.example.org:5001` if your local host resolves that name.

3.  **Run the Client:**
    *   Open a new terminal.
    *   Ensure `SPIFFE_ENDPOINT_SOCKET` is set as in step 1.
    *   Navigate to the client's directory:
        ```bash
        cd samples/Spiffe.Sample.AspNetCore.Mtls/Client
        ```
    *   Run the client application:
        ```bash
        dotnet run
        ```

## Expected Output

When running the sample, observe the logs from both the server and client applications.

### Server Logs

The server logs should indicate:
*   It has started and is listening for incoming HTTPS connections (e.g., on `https://localhost:5001`).
*   It receives requests from the client.
*   During the mTLS handshake, the client's X.509 SVID is validated, and the server logs should show the client's SPIFFE ID.

Example snippet:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/2 GET https://localhost:5001/ - null null
info: Spiffe.Sample.AspNetCore.Mtls.Server[0]
      Request from 'spiffe://example.org/my-client'
```

### Client Logs

The client logs should show:
*   It successfully fetches an X.509 SVID from the SPIRE Workload API for its own SPIFFE ID (e.g., `spiffe://example.org/my-client`).
*   It periodically makes HTTPS requests to the server (e.g., `https://my-server.example.org:5001` or `https://localhost:5001`).
*   It receives successful responses from the server, indicating a successful mTLS connection.

Example snippet:
```
info: Spiffe.Sample.AspNetCore.Mtls.Client.Worker[0]
      Client SVID: spiffe://example.org/my-client
info: Spiffe.Sample.AspNetCore.Mtls.Client.Worker[0]
      Calling server...
info: Spiffe.Sample.AspNetCore.Mtls.Client.Worker[0]
      Server response: Hello, spiffe://example.org/my-client
```
(Note: If running locally and not using DNS `my-server.example.org` for the server, the client might connect to `localhost`, but the server's SVID will still ideally contain `spiffe://example.org/my-server` and the DNS name `my-server.example.org` for proper validation if the client is configured to check it.)
