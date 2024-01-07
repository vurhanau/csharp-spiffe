CSharp implementation of [java-spiffe-helper](https://github.com/spiffe/java-spiffe/tree/main/java-spiffe-helper).

## Usage
1. Install, build, run Spire ([quickstart](https://spiffe.io/docs/latest/try/getting-started-linux-macos-x/)).
2. Run this command to get an SVID:
    ```
    dotnet run --address /tmp/spire-agent/public/api.sock
    ```
    Expected output:
    ```
    Options: {
        "Address": "/tmp/spire-agent/public/api.sock"
    }
    Spiffe ID: spiffe://example.org/myservice
    ```
