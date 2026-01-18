.DEFAULT_GOAL := help
help: ## Shows this help
	@egrep -h '\s##\s' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m  %-30s\033[0m %s\n", $$1, $$2}'

gtu: ## GrpcTlsUp
	export SAMPLE_DIR=Spiffe.Sample.Grpc.Tls && docker compose -p spiffe-grpc-tls up -d --build

gtd: ## GrpcTlsDown
	export SAMPLE_DIR=Spiffe.Sample.Grpc.Tls && docker compose -p spiffe-grpc-tls down

gmu: ## GrpcMtlsUp
	export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls && docker compose -p spiffe-grpc-mtls up -d --build

gmd: ## GrpcMtlsDown
	export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls && docker compose -p spiffe-grpc-mtls down

atu: ## AspNetCoreTlsUp
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Tls && docker compose -p spiffe-aspnetcore-tls up -d --build

atd: ## AspNetCoreTlsDown
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Tls && docker compose -p spiffe-aspnetcore-tls down

amu: ## AspNetCoreMtlsUp
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Mtls && docker compose -p spiffe-aspnetcore-mtls up -d --build

amd: ## AspNetCoreMtlsDown
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Mtls && docker compose -p spiffe-aspnetcore-mtls down

aju: ## AspNetCoreJwtUp
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Jwt && docker compose -p spiffe-aspnetcore-jwt up -d --build

ajd: ## AspNetCoreJwtDown
	export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Jwt && docker compose -p spiffe-aspnetcore-jwt down
