.PHONY: server client curl

server:
	@dotnet run --project MtlsGrpcServer/

client:
	@dotnet run --project MtlsGrpcClient/

curl:
	@curl -vvv http://localhost:5000/
