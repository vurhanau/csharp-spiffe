.PHONY: build
build:
	@dotnet build

.PHONY: restore
restore:
	@dotnet restore

.PHONY: docker
docker:
	@rm -rf nupkg
	@docker build --target server -t spiffe/aspnet-jwt-server -f Dockerfile . --progress=plain
	@docker build --target client -t spiffe/aspnet-jwt-client -f Dockerfile . --progress=plain

.PHONY: up
up:
	@MODE='' docker-compose up -d --remove-orphans --build

.PHONY: down
down:
	@MODE='' docker-compose down

.PHONY: docker-dev
docker-dev:
	@rm -rf nupkg && mkdir -p nupkg && cp ../../nupkg/*.nupkg nupkg
	@docker build --target server -t spiffe/aspnet-jwt-server-dev -f Dockerfile.dev . --progress=plain
	@docker build --target client -t spiffe/aspnet-jwt-client-dev -f Dockerfile.dev . --progress=plain

.PHONY: up-dev
up-dev:
	@MODE=-dev docker-compose up -d --remove-orphans --build

.PHONY: down-dev
down-dev:
	@MODE=-dev docker-compose down

.PHONY: agent-cert
agent-cert:
	@openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:secp384r1 -days 3650 \
		-nodes -keyout dummy_agent.key -out dummy_agent.crt -subj "/CN=agent.example.com" \
		-addext "subjectAltName=DNS:agent.example.com" \
		-addext "basicConstraints=CA:FALSE" \
		-addext "keyUsage=nonRepudiation,digitalSignature,keyEncipherment"
