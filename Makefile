-include .env

.DEFAULT_GOAL := help

.PHONY: help
help:     ## Shows this help
	@egrep -h '\s##\s' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m  %-30s\033[0m %s\n", $$1, $$2}'

############################################################################
# Build and test
############################################################################

os1=$(shell uname -s)
ifeq ($(os1),Darwin)
OPEN=open
else ifeq ($(os1),Linux)
OPEN=xdg-open
else
OPEN=$(error unsupported OS: $(os1))
endif

.PHONY: restore
restore: ## Restores project dependencies
	@dotnet restore

.PHONY: build
build: restore  ## Builds the library
	@dotnet build

.PHONY: build-samples
build-samples: samples/*  ## Builds the samples
	@for file in $^ ; do \
		! [[ "$${file}" =~ "WatcherNuget" ]] && dotnet build "$${file}" || true; \
	done

.PHONY: test
test: restore ## Runs unit, integration tests
	@dotnet test

.PHONY: coverage
coverage: ## Generates code coverage report
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

.PHONY: fmt
fmt: ## Formats the code
	@dotnet format Spiffe.sln


############################################################################
# Release
############################################################################

SPIFFE_VERSION := $$(grep "<SpiffeVersion>" Directory.Packages.props | sed 's/\s*<.*>\(.*\)<.*>/\1/' | awk '{$$1=$$1};1')

.PHONY: version
version: ## Prints the current version
	@echo $(SPIFFE_VERSION)

.PHONY: release
release: ## Creates a new release
	@SPIFFE_VERSION=$(shell echo $(SPIFFE_VERSION) | sed 's/-dev//') && \
	sed -i '' "s/<SpiffeVersion>.*<\/SpiffeVersion>/<SpiffeVersion>$${SPIFFE_VERSION}<\/SpiffeVersion>/" Directory.Packages.props && \
	echo "Release version: $${SPIFFE_VERSION}" && \
	git checkout -b release/$(SPIFFE_VERSION) && \
	git add Directory.Packages.props && \
	git commit -m "Bump version to $(SPIFFE_VERSION)" && \
	git tag $(SPIFFE_VERSION) && \
	git push origin release/$(SPIFFE_VERSION) && \
	git push origin $(SPIFFE_VERSION)

.PHONY: next-patch
next-patch: ## Sets dev version with incremented patch version
	@SPIFFE_VERSION=$(shell echo $(SPIFFE_VERSION) | awk -F. -v OFS=. '{$$NF = $$NF + 1; print}')-dev && \
	sed -i '' "s/<SpiffeVersion>.*<\/SpiffeVersion>/<SpiffeVersion>$${SPIFFE_VERSION}<\/SpiffeVersion>/" Directory.Packages.props && \
	echo "Version set to $${SPIFFE_VERSION}"

.PHONY: pkg
pkg: ## Builds the nuget package and runs the sample to test the artifact
	@rm -rf nupkg/*
	@dotnet pack src/Spiffe/Spiffe.csproj \
		--configuration Release \
		--output nupkg \
		-p:IncludeSymbols=true \
		-p:SymbolPackageFormat=snupkg

.PHONY: pkg-test
pkg-test: pkg ## Tests the nuget package
	@unzip -l nupkg/Spiffe.$(SPIFFE_VERSION).nupkg
	@cd samples/Spiffe.Sample.WatcherNuget && \
		dotnet clean && \
		dotnet restore -s ../../nupkg -s https://api.nuget.org/ && \
		dotnet run

.PHONY: push
push: ## Pushes the nuget package to the nuget.org
	@dotnet nuget push nupkg/Spiffe.$(SPIFFE_VERSION).nupkg --api-key $(ENV_NUGET_API_KEY) --source https://api.nuget.org/v3/index.json


############################################################################
# End-to-end test
############################################################################

SPIRE_DIR := $(HOME)/Projects/spiffe/spire

.PHONY: e2e-server
e2e-server: ## Starts the Spire server located at $(SPIRE_DIR)
	@cd $(SPIRE_DIR) && ./spire-server run -config conf/server/server.conf

.PHONY: e2e-agent
e2e-agent: ## Starts the Spire agent located at $(SPIRE_DIR)
	cd $(SPIRE_DIR) && \
	./spire-agent run \
	-config conf/agent/agent.conf \
	-joinToken $(shell cd $(SPIRE_DIR) && ./spire-server token generate -spiffeID spiffe://example.org/myagent | sed 's/Token: //') 

.PHONY: e2e-policy
e2e-policy: ## Creates a policy for the workload
	@cd $(SPIRE_DIR) && ./spire-server entry create \
		-parentID spiffe://example.org/myagent \
		-spiffeID spiffe://example.org/myservice \
		-selector unix:uid:$$(id -u)

.PHONY: e2e-workload
e2e-workload: ## Starts the sample watcher workload
	@cd samples/Spiffe.Sample.Watcher && dotnet run


############################################################################
# Toolchain and utilities
############################################################################

.PHONY: toolchain
toolchain: ## Installs the required tools
	@dotnet tool install -g dependadotnet
	@dotnet tool install -g dotnet-reportgenerator-globaltool

.PHONY: dependabot
dependabot:
	@dependadotnet . > .github/dependabot.yml
