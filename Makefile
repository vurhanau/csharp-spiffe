SPIRE_DIR := $(HOME)/Projects/spiffe/spire
AGENT_SOCKET := --address unix:///tmp/spire-agent/public/api.sock
RUN := @dotnet run --project src/Spiffe.Client/

os1=$(shell uname -s)
ifeq ($(os1),Darwin)
OPEN=open
else ifeq ($(os1),Linux)
OPEN=xdg-open
else
OPEN=$(error unsupported OS: $(os1))
endif

.PHONY: coverage

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

build-samples: samples/*
	@for file in $^ ; do \
		dotnet build $${file} ; \
	done

watch:
	@cd samples/Spiffe.Sample.Watcher && dotnet run

test: restore
	@dotnet test

coverage:
	@rm -rf coverage/* && \
	dotnet test --verbosity normal \
		--collect:"XPlat Code Coverage" \
		--results-directory ./coverage \
		--settings coverlet.runsettings && \
	cd coverage/* && \
	reportgenerator \
		-reports:"coverage.cobertura.xml" \
		-targetdir:"coveragereport" \
		-reporttypes:Html && \
	$(OPEN) coveragereport/index.html

fmt:
	@dotnet format Spiffe.sln

lint:
	@jb inspectcode Spiffe.sln -o=jb.xml --build

dependabot:
	@dependadotnet . > .github/dependabot.yml

toolchain:
	@dotnet tool install -g dependadotnet
	@dotnet tool install -g dotnet-reportgenerator-globaltool
	@dotnet tool install -g JetBrains.ReSharper.GlobalTools
