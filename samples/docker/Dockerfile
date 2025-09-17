FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ARG SAMPLE_DIR="Spiffe.Sample.AspNetCore.Jwt"
WORKDIR /sample

COPY ${SAMPLE_DIR}/ ./
RUN dotnet restore
RUN dotnet publish Server/Server.csproj -c Release -o server
RUN dotnet publish Client/Client.csproj -c Release -o client

# Build server image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS server
WORKDIR /server
COPY --from=build-env /sample/server .
ENTRYPOINT ["dotnet", "Server.dll"]

# Build client image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS client
WORKDIR /client
COPY --from=build-env /sample/client .
ENTRYPOINT ["dotnet", "Client.dll"]