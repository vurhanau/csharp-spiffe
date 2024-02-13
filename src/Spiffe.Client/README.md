CSharp implementation of [java-spiffe-helper](https://github.com/spiffe/java-spiffe/tree/main/java-spiffe-helper).

## Usage
Install, build, run Spire ([quickstart](https://spiffe.io/docs/latest/try/getting-started-linux-macos-x/)).

Commands:
- Fetch X509 SVID
    ```
    dotnet run x509svid --address <agent-socket-path>
    ```
- Fetch X509 Bundles
    ```
    dotnet run x509bundle --address <agent-socket-path>
    ```
- Watch X509 SVID update stream
    ```
    dotnet run x509watch --address <agent-socket-path>
    ```
- Fetch JWT SVID
    ```
    dotnet run jwtsvid --address <agent-socket-path> --audience spiffe://example.org/myservice
    ```
- Fetch JWT Bundles
    ```
    dotnet run jwtbundle --address <agent-socket-path>
    ```
- Watch JWT bundle update stream
    ```
    dotnet run jwtwatch --address <agent-socket-path>
    ```
