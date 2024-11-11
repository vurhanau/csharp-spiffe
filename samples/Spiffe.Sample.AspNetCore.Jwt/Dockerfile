FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:35792ea4ad1db051981f62b313f1be3b46b1f45cadbaa3c288cd0d3056eefb83 AS build-env
WORKDIR /sample

COPY src/ ./src/
COPY *.sln ./
RUN dotnet restore
RUN dotnet publish src/Server/Server.csproj -c Release -o server
RUN dotnet publish src/Client/Client.csproj -c Release -o client

# Build server image
FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:6c4df091e4e531bb93bdbfe7e7f0998e7ced344f54426b7e874116a3dc3233ff AS server
WORKDIR /server
COPY --from=build-env /sample/server .
ENTRYPOINT ["dotnet", "Server.dll"]

# Build client image
FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:6c4df091e4e531bb93bdbfe7e7f0998e7ced344f54426b7e874116a3dc3233ff AS client
WORKDIR /client
COPY --from=build-env /sample/client .
ENTRYPOINT ["dotnet", "Client.dll"]