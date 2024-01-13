# CSharp SPIFFE Library

## Overview

The CSharp SPIFFE library provides functionality to interact with the Workload API to fetch X.509 and JWT SVIDs and Bundles. 

This library contains two modules:

* [Spiffe](src/Spiffe/README.md): Core functionality to interact with the Workload API, and to process and validate 
X.509 and JWT SVIDs and bundles.

* [Spiffe.Client](src/Spiffe.Client/README.md): Client to fetch X.509 SVIDs, Bundles and store them on disk.

Requires .NET8.