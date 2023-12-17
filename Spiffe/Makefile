SPIRE_DIR := /Users/avurhanau/Projects/spiffe/spire

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

run:
	@dotnet run --project Spiffe.Client/

test:
	@dotnet test Spiffe.Tests/
