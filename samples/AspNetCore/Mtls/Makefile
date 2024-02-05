.PHONY: server client curl

server:
	@dotnet run --project MtlsServer/

client:
	@dotnet run --project MtlsClient/

curl:
	@curl -vvv http://localhost:5000/
