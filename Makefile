SPIRE_DIR := /Users/avurhanau/Projects/spiffe/spire

.PHONY: build coverage

server:
	cd $(SPIRE_DIR) && ./spire-server run -config conf/server/server.conf

jt:
	cd $(SPIRE_DIR) && ./spire-server token generate -spiffeID spiffe://example.org/myagent

agent:
	cd $(SPIRE_DIR) && ./spire-agent run -config conf/agent/agent.conf -joinToken $(JT)

x509:
	cd $(SPIRE_DIR) && ./spire-agent api fetch x509

policy:
	cd $(SPIRE_DIR) && ./spire-server entry create -parentID spiffe://example.org/myagent \
    -spiffeID spiffe://example.org/myservice -selector unix:uid:$$(id -u)

build:
	@dotnet build

run:
	@dotnet run --project src/Spiffe.Client/

test:
	@dotnet test

coverage:
	@dotnet test --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

coverage-report:
	@reportgenerator \
		-reports:"Spiffe.Tests/TestResults/$(TID)/coverage.cobertura.xml" \
		-targetdir:"Spiffe.Tests/TestResults/$(TID)/coveragereport" \
		-reporttypes:Html

fmt:
	@dotnet format ./Spiffe.sln
