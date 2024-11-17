FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /sample

COPY src/ ./src/
COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
RUN dotnet restore
RUN dotnet publish src/Server/Server.csproj -c Release -o server
RUN dotnet publish src/Client/Client.csproj -c Release -o client

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