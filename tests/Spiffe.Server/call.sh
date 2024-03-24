#!/bin/bash

grpcurl -plaintext -d @ localhost:5001 SpiffeWorkloadAPI/FetchJWTSVID <<EOM
{
    "audience": ["foo", "bar"],
    "spiffe_id": "spiffe://example.org/grpcurl"
}
EOM
