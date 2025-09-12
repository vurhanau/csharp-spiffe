#!/bin/bash

export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls.Debug
pushd ../ && make pack && popd
mkdir -p ${SAMPLE_DIR}/LocalFeed
cp ../nupkg/Spiffe.*-dev.nupkg ${SAMPLE_DIR}/LocalFeed
docker-compose -f compose-debug.yaml -p spiffe-grpc-mtls-debug build --no-cache
docker-compose -f compose-debug.yaml -p spiffe-grpc-mtls-debug up -d
