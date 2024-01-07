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

build:
	@dotnet restore --locked-mode --force-evaluate
	@dotnet build

x509:
	$(RUN) x509 $(AGENT_SOCKET)

bundle:
	$(RUN) bundle $(AGENT_SOCKET)

watch:
	$(RUN) watch $(AGENT_SOCKET)

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

# dotnet tool install -g dependadotnet
dependabot:
	dependadotnet . > .github/dependabot.yml