using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace CosmosDB.RUsTests
{
    class Program
    {
        private string _accountEndpoint;
        private string _accountKey;

        private string _databaseName;

        private string _collectionName;

        private DocumentClient client;

        static async Task Main(string[] args)
        {
            Program p = new Program();

            try
            {
                await p.RunAllTestsAsync();
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine($"{de.StatusCode} error occurred: {de.Message}");
                Console.WriteLine(baseException);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine(baseException);
            }
            finally
            {
                Console.WriteLine("End of RU tests - take a look at 'results.csv', press any key to exit.");
                Console.ReadKey();
            }
        }

        public Program()
        {
            var configFileJson = File.ReadAllText("config.json");
            var config = JObject.Parse(configFileJson);
            _accountEndpoint = (string)config["AccountEndpoint"];
            _accountKey = (string)config["AccountKey"];
            _databaseName = (string)config["DatabaseName"];
            _collectionName = (string)config["CollectionName"];
        }

        private async Task RunAllTestsAsync()
        {
            var documents = Directory.EnumerateFiles("scenarios/", "document.*")
                .Select(f => new DocumentScenario(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
                .ToList();

            var queryData = Directory.EnumerateFiles("scenarios/", "querydata.*")
                .Select(f => new QueryDataScenario(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
                .ToList();

            var queries = Directory.EnumerateFiles("scenarios/", "query.*")
                .Select(f => new QueryScenario(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
                .ToList();

            var indexPolicies = Directory.EnumerateFiles("scenarios/", "indexpolicy.*")
                .Select(f => new IndexPolicyScenario(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
                .ToList();

            var consistencyLevels = new[]
            {
                ConsistencyLevel.Eventual,
                ConsistencyLevel.ConsistentPrefix,
                ConsistencyLevel.Session,
                ConsistencyLevel.BoundedStaleness,
                ConsistencyLevel.Strong,
            };

            // All combinations of
            // - ConsistencyLevel
            // - Index Policies
            // - Document & Query

            var results = new List<RunConfiguration>();

            foreach (var consistencyLevel in consistencyLevels)
            {
                foreach (var indexPolicy in indexPolicies)
                {
                    var runConfig = new RunConfiguration
                    {
                        ConsistencyLevel = consistencyLevel,
                        IndexPolicyScenario = indexPolicy,
                        DocumentScenarios = documents.Copy(),
                        QueryDataScenarios = queryData,
                        QueryScenarios = queries.Copy()
                    };
                    results.Add(runConfig);

                    await RunTestsAsync(runConfig);
                }
            }

            string resultsFile = GenerateResultFile(results);
            File.WriteAllText("results.csv", resultsFile);
        }

        private string GenerateResultFile(List<RunConfiguration> runConfigs)
        {
            var csvResults = new StringBuilder();
            const string sep = ";";

            csvResults.Append("Name");
            csvResults.Append(sep);
            csvResults.Append("Original Doc Size");
            csvResults.Append(sep);
            csvResults.Append("Doc Size w Metadata");
            csvResults.Append(sep);
            csvResults.Append("Create RUs");
            csvResults.Append(sep);
            csvResults.Append("Read RUs");
            csvResults.Append(sep);
            csvResults.Append("Replace RUs");
            csvResults.Append(sep);
            csvResults.Append("Delete RUs");
            csvResults.Append(sep);
            csvResults.Append("Query RUs");
            csvResults.Append(sep);
            csvResults.Append("Item Count");
            csvResults.Append(sep);
            csvResults.Append("Total Size");
            csvResults.Append(sep);
            csvResults.Append("Consistency Level");
            csvResults.Append(sep);
            csvResults.Append("Index Policy Scenario");
            csvResults.AppendLine();

            foreach (var runConfig in runConfigs)
            {
                foreach (var document in runConfig.DocumentScenarios)
                {
                    csvResults.Append(document.Name);
                    csvResults.Append(sep);
                    csvResults.Append(document.OriginalDocumentSize);
                    csvResults.Append(sep);
                    csvResults.Append(document.DocumentSizeWithMetadata);
                    csvResults.Append(sep);
                    csvResults.Append(document.CreateRequestCharge);
                    csvResults.Append(sep);
                    csvResults.Append(document.ReadRequestCharge);
                    csvResults.Append(sep);
                    csvResults.Append(document.ReplaceRequestCharge);
                    csvResults.Append(sep);
                    csvResults.Append(document.DeleteRequestCharge);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(runConfig.ConsistencyLevel);
                    csvResults.Append(sep);
                    csvResults.Append(runConfig.IndexPolicyScenario.Name);
                    csvResults.AppendLine();
                }
                foreach (var query in runConfig.QueryScenarios)
                {
                    csvResults.Append(query.Name);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(sep);
                    csvResults.Append(query.RequestCharge);
                    csvResults.Append(sep);
                    csvResults.Append(query.ItemCount);
                    csvResults.Append(sep);
                    csvResults.Append(query.TotalSize);
                    csvResults.Append(sep);
                    csvResults.Append(runConfig.ConsistencyLevel);
                    csvResults.Append(sep);
                    csvResults.Append(runConfig.IndexPolicyScenario.Name);
                    csvResults.AppendLine();
                }
            }

            return csvResults.ToString();
        }

        private async Task RunTestsAsync(RunConfiguration runConfig)
        {
            // For each
            // 1. Create Collection with Index Policy
            // 2. Load all Querydata in
            // 3. Execute Document operations
            // 4. Execute Queries

            Console.WriteLine($"ConsistencyLevel: {runConfig.ConsistencyLevel}, IndexingPolicy: {runConfig.IndexPolicyScenario.Name}");

            await CreateDatabaseAndCollectionAsync(_databaseName, _collectionName, runConfig.ConsistencyLevel, runConfig.IndexPolicyScenario.IndexingPolicy);

            foreach (var queryData in runConfig.QueryDataScenarios.SelectMany(_ => _.Documents))
            {
                await GetRequestChargeForCreateDocumentAsync(_databaseName, _collectionName, queryData);
            }

            foreach (var documentScenario in runConfig.DocumentScenarios)
            {
                var document = documentScenario.Document;
                var documentId = documentScenario.DocumentId;

                var create = await GetRequestChargeForCreateDocumentAsync(_databaseName, _collectionName, document);
                documentScenario.DocumentSizeWithMetadata = create.documentSizeWithMetadata;
                documentScenario.OriginalDocumentSize = create.originalDocumentSize;
                documentScenario.CreateRequestCharge = create.requestCharge;

                var read = await GetRequestChargeForReadDocumentAsync(_databaseName, _collectionName, documentId);
                documentScenario.ReadRequestCharge = read;

                var replace = await GetRequestChargeForReplaceDocumentAsync(_databaseName, _collectionName, documentId, document);
                documentScenario.ReplaceRequestCharge = replace;

                var delete = await GetRequestChargeForDeleteDocumentAsync(_databaseName, _collectionName, documentId);
                documentScenario.DeleteRequestCharge = delete;
            }

            foreach (var queryScenario in runConfig.QueryScenarios)
            {
                var query = await GetRequestChargeForQueryDocuments(_databaseName, _collectionName, queryScenario.Query);
                queryScenario.RequestCharge = query.requestCharge;
                queryScenario.ItemCount = query.count;
                queryScenario.TotalSize = query.totalSize;
            }
        }

        private async Task CreateDatabaseAndCollectionAsync(string databaseName, string collectionName, ConsistencyLevel consistencyLevel, IndexingPolicy indexingPolicy)
        {
            this.client = new DocumentClient(new Uri(_accountEndpoint), _accountKey, null, consistencyLevel);

            try
            {
                await this.client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException e) when (e.StatusCode != HttpStatusCode.NotFound)
            {
                throw;
            }
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });

            var collection = new DocumentCollection { Id = collectionName, IndexingPolicy = indexingPolicy };
            collection = await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseName), collection);
        }

        private async Task<(double originalDocumentSize, double documentSizeWithMetadata, double requestCharge)> GetRequestChargeForCreateDocumentAsync(string databaseName, string collectionName, JObject jsonDocument)
        {
            var response = await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), jsonDocument);
            var sizeWithoutMetadata = jsonDocument.ToString(Formatting.None).Length;
            var sizeWithMetadata = response.Resource.ToByteArray().Length;
            return (sizeWithoutMetadata / 1000.0, sizeWithMetadata / 1000.0, response.RequestCharge);
        }

        private async Task<double> GetRequestChargeForReadDocumentAsync(string databaseName, string collectionName, string documentId)
        {
            var response = await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentId));
            return response.RequestCharge;
        }

        private async Task<double> GetRequestChargeForReplaceDocumentAsync(string databaseName, string collectionName, string documentId, JObject jsonDocument)
        {
            var response = await this.client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentId), jsonDocument);
            return response.RequestCharge;
        }

        private async Task<double> GetRequestChargeForDeleteDocumentAsync(string databaseName, string collectionName, string documentId)
        {
            var response = await this.client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentId));
            return response.RequestCharge;
        }

        private async Task<(double totalSize, int count, double requestCharge)> GetRequestChargeForQueryDocuments(string databaseName, string collectionName, string sqlQuery)
        {
            var queryInSql = this.client
                .CreateDocumentQuery<Document>(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), sqlQuery)
                .AsDocumentQuery();
            var response = await queryInSql.ExecuteNextAsync<Document>();
            return (response.Sum(_ => _.ToByteArray().Length / 1000.0), response.Count, response.RequestCharge);
        }
    }
}
