# Azure Cosmos DB - Request Units Tester

Sample console application for getting real data about how many request units are consumed.

More details and documentation can be found at [How to correctly calculate the Request Units in Azure Cosmos DB](https://azure-development.com/2018/12/11/calculating-request-units-in-azure-cosmos-db/);

## Getting started

1. define documents to test
2. define queries to test and their test data
3. define index policies to test
4. configure Azure Cosmos DB account endpoint and key
5. RUN IT!
6. Analyze the results

## Define documents to test

Any number of JSON documents can be put into the folder 'scenarios'.

Each one needs be named like 'document.NAME.json'.  
This can be used to test different document types or to test different JSON layouts for the same document type.

## Define queries and test data

Any number of queries can be put into the folder 'scenarios'.

Each one needs be named like 'query.NAME.sql'. 
For having test data for the queries inside the database, one or multiple 'querydata.NAME.json' files can be used.

## Define index policies

Any number of indexing policies can be put into the folder 'scenarios'.

Each one needs be named like 'indexpolicy.NAME.json'.  
This can be used to test the impact of indexing for the various document operations.

## Configure Azure Cosmos DB account endpoint and key

A file 'config.json', which contains the Azure Cosmos DB account endpoint and key, needs to be available inside the root folder.

The file 'config.template.json' can be used as a blueprint.

## RUN IT!

This project can be used run with 'dotnet run' executed inside the root.  
.NET Core needs to be installed.

What happens then?

The console application tests all combinations of

- Consistency Level
- the defined Indexing Policies
- the defined Documents & Queries

For each document and the possible operation the following is tracked:

- original document size
- document size inside CosmosDB (with metadata, without formatting)
- request units charge for each operation

For each query the following is tracked:

- count of items returned
- total size of all items returned
- request units charge

After that it generates a 'results.csv' with all the results.

## Analyze the results

Open the 'results.csv' file in Excel or something else.

These real numbers can then be used for any design decisions and for calculating the request units needed.
