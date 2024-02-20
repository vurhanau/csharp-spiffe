SPIRE_DIR := $(HOME)/Projects/spiffe/spire
AGENT_SOCKET := --address unix:///tmp/spire-agent/public/api.sock
RUN := @dotnet run --project src/Spiffe.Client/

server:
	@cd $(SPIRE_DIR) && ./spire-server run -config conf/server/server.conf

jt:
	@cd $(SPIRE_DIR) && ./spire-server token generate -spiffeID spiffe://example.org/myagent

agent:
	@cd $(SPIRE_DIR) && ./spire-agent run -config conf/agent/agent.conf -joinToken $(JT)

fetch:
	@cd $(SPIRE_DIR) && ./spire-agent api fetch x509

policy:
	@cd $(SPIRE_DIR) && ./spire-server entry create \
		-parentID spiffe://example.org/myagent \
		-spiffeID spiffe://example.org/myservice \
		-selector unix:uid:$$(id -u)

curl:
	@curl -vvv http://localhost:5000/

restore:
	@dotnet restore

build: restore
	@dotnet build

build-samples:
	@cd samples/Spiffe.Sample.AspNetCore.Jwt && dotnet build
	@cd samples/Spiffe.Sample.AspNetCore.Mtls && dotnet build
	@cd samples/Spiffe.Sample.AspNetCore.Tls && dotnet build
	@cd samples/Spiffe.Sample.Grpc.Mtls && dotnet build
	@cd samples/Spiffe.Sample.Grpc.Tls && dotnet build
	@cd samples/Spiffe.Sample.Watcher && dotnet build

watch:
	@cd samples/Spiffe.Sample.Watcher && dotnet run

test: restore
	@dotnet test

coverage:
	@dotnet test --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

coverage-report:
	@reportgenerator \
		-reports:"Spiffe.Tests/TestResults/$(TID)/coverage.cobertura.xml" \
		-targetdir:"Spiffe.Tests/TestResults/$(TID)/coveragereport" \
		-reporttypes:Html

fmt:
	@dotnet format Spiffe.sln

lint:
	@jb inspectcode Spiffe.sln -o=jb.xml --build
# 	@jb cleanupcode Spiffe.sln

dependabot:
	@dependadotnet . > .github/dependabot.yml

toolchain:
	@dotnet tool install -g dependadotnet
	@dotnet tool install -g dotnet-reportgenerator-globaltool
	@dotnet tool install -g JetBrains.ReSharper.GlobalTools
