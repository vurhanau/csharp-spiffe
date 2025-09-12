#!/bin/bash

export SAMPLE_DIR=Spiffe.Sample.Grpc.Mtls.Debug
rm -rf ${SAMPLE_DIR}/LocalFeed
docker-compose -f compose-debug.yaml -p spiffe-grpc-mtls-debug down --rmi all --volumes
