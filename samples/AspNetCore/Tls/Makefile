.PHONY: server client curl

server:
	@dotnet run --project TlsServer/

client:
	@dotnet run --project TlsClient/

curl:
	@curl -vvv http://localhost:5000/
