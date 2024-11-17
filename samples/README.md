## C# SPIFFE Samples

This directory contains scenarios of SPIFFE usage in .NET projects:
- [GRPC - TLS with X509-SVIDs](#grpc---tls-with-x509-svids)
- [GRPC - mTLS with X509-SVIDs](#grpc---mtls-with-x509-svids)
- [ASP.NET Core - TLS with X509-SVIDs](#aspnet-core---tls-with-x509-svids)
- [ASP.NET Core - mTLS with X509-SVIDs](#aspnet-core---mtls-with-x509-svids)
- [ASP.NET Core - JWT authentication](#aspnet-core---jwt-authentication)

#### [GRPC - TLS with X509-SVIDs](./Spiffe.Sample.Grpc.Tls/)
Start:
```sh
export SAMPLE_DIR=Spiffe.Sample.Grpc.Tls
docker-compose -p spiffe-grpc-tls up -d --build
```
Stop:
```sh
export SAMPLE_DIR=Spiffe.Sample.Grpc.Tls
docker-compose -p spiffe-grpc-tls down
```

#### [GRPC - mTLS with X509-SVIDs](./Spiffe.Sample.Grpc.Mtls/)
Start:
```sh
export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls
docker-compose -p spiffe-grpc-mtls up -d --build
```
Stop:
```sh
export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls
docker-compose -p spiffe-grpc-mtls down
```

#### [ASP.NET Core - TLS with X509-SVIDs](./Spiffe.Sample.AspNetCore.Tls/)
Start:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Tls
docker-compose -p spiffe-aspnetcore-tls up -d --build
```
Stop:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Tls
docker-compose -p spiffe-aspnetcore-tls down
```

#### [ASP.NET Core - mTLS with X509-SVIDs](./Spiffe.Sample.AspNetCore.Mtls/)
Start:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Mtls
docker-compose -p spiffe-aspnetcore-mtls up -d --build
```
Stop:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Mtls
docker-compose -p spiffe-aspnetcore-mtls down
```

#### [ASP.NET Core - JWT authentication](./Spiffe.Sample.AspNetCore.Jwt/)
Start:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Jwt
docker-compose -p spiffe-aspnetcore-jwt up -d --build
```
Stop:
```sh
export SAMPLE_DIR=Spiffe.Sample.AspNetCore.Jwt
docker-compose -p spiffe-aspnetcore-jwt down
```
