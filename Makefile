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
build: restore  ## Builds the project
	@dotnet build --no-restore

.PHONY: test
test: ## Runs unit, integration tests and generates code coverage report
	@rm -rf coverage/* && \
	dotnet test \
		--no-build \
		--verbosity normal \
		--collect:"XPlat Code Coverage" \
		--results-directory ./coverage \
		--settings coverlet.runsettings

.PHONY: report
report: coverage ## Shows code coverage report
	@cd coverage/* && \
	reportgenerator \
		-reports:"coverage.cobertura.xml" \
		-targetdir:"coveragereport" \
		-reporttypes:Html && \
	$(OPEN) coveragereport/index.html

.PHONY: fmt
fmt: ## Formats the code
	@dotnet format Spiffe.sln

.PHONY: clean
clean: ## Cleans the project
	@dotnet clean && \
	rm -rf coverage && \
	rm -rf nupkg/* && \
	rm -rf src/Spiffe/bin && \
	rm -rf src/Spiffe/obj

.PHONY: build-samples
build-samples: samples/local/* samples/docker/*  ## Builds the samples
	@for file in $^ ; do \
		[[ "$${file}" =~ "Spiffe.Sample." ]] && dotnet restore "$${file}" && dotnet build "$${file}" --no-restore || true; \
	done

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
	git push origin release/$(SPIFFE_VERSION)

tag:
	@git checkout main && \
	git pull origin main && \
	git tag v$(SPIFFE_VERSION) && \
	git push origin v$(SPIFFE_VERSION)

.PHONY: next-patch
next-patch: ## Sets dev version with incremented patch version
	@SPIFFE_VERSION=$(shell echo $(SPIFFE_VERSION) | awk -F. -v OFS=. '{$$NF = $$NF + 1; print}')-dev && \
	sed -i '' "s/<SpiffeVersion>.*<\/SpiffeVersion>/<SpiffeVersion>$${SPIFFE_VERSION}<\/SpiffeVersion>/" Directory.Packages.props && \
	echo "Version set to $${SPIFFE_VERSION}" && \
	git checkout -b dev/$(SPIFFE_VERSION) && \
	git add Directory.Packages.props && \
	git commit -m "Bump version to $${SPIFFE_VERSION}" && \
	git push origin dev/$(SPIFFE_VERSION)

.PHONY: pack
pack: ## Builds the nuget package and runs the sample to test the artifact
	@rm -rf nupkg/*
	@dotnet pack src/Spiffe/Spiffe.csproj \
		--configuration Release \
		--output nupkg \
		-p:IncludeSymbols=true \
		-p:SymbolPackageFormat=snupkg

.PHONY: push
push: ## Pushes the nuget package to the nuget.org
	@dotnet nuget push nupkg/Spiffe.$(SPIFFE_VERSION).nupkg --api-key $(ENV_NUGET_API_KEY) --source https://api.nuget.org/v3/index.json


############################################################################
# Toolchain and utilities
############################################################################

.PHONY: toolchain
toolchain: ## Installs the required tools
	@dotnet tool install -g dependadotnet
	@dotnet tool install -g dotnet-reportgenerator-globaltool
