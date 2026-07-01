using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using develop_agents.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;

namespace develop_agents
{
    public class AgenticRAGAzureSearch
    {
        private IChatClient chatClient;
        private IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;

        private Uri searchEndpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT")!);
        private AzureKeyCredential searchKey = new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_SEARCH_KEY")!);
        private string indexName = "docs-index";

        public AgenticRAGAzureSearch(string endpoint, string deploymentName, string? apiKey = null)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // using api key
                chatClient = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new ApiKeyCredential(apiKey)
                )
                .GetChatClient(deploymentName)
                .AsIChatClient();

                embeddingGenerator = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
                    .GetEmbeddingClient("text-embedding-3-small")
                    .AsIEmbeddingGenerator();
            }
            else
            {
                chatClient = new AzureOpenAIClient(
                    new Uri(endpoint),
                    new AzureCliCredential()
                )
                .GetChatClient(deploymentName)
                .AsIChatClient();

                embeddingGenerator = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
                    .GetEmbeddingClient("text-embedding-3-small")
                    .AsIEmbeddingGenerator();
            }
        }

        public async Task GenerateEmbeddings()
        {
            var searchIndexClient = new SearchIndexClient(searchEndpoint, searchKey);

            var index = new SearchIndex(indexName)
            {
                Fields =
                {
                    new SimpleField("DocumentId", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SearchableField("Title") { IsSortable = false },
                    new SearchableField("Content"),
                    new SearchField("ContentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        IsHidden = false,
                        VectorSearchDimensions = 1536,
                        VectorSearchProfileName = "vector-profile"
                    }
                },
                VectorSearch = new VectorSearch
                {
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration("hnsw")
                    },
                    Profiles =
                    {
                        new VectorSearchProfile("vector-profile", "hnsw")
                    }
                }
            };

            var searchIndex = await searchIndexClient.CreateOrUpdateIndexAsync(index);

            var sampleAdrs = new List<ArchitectureDecision>
            {
                new()
                {
                    DocumentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Title = "ADR-001: gRPC for Internal Microservices Communication",
                    Content = "In January 2024, we decided to adopt gRPC over REST for internal microservices communication. Key reasons: 1) Binary protocol (Protocol Buffers) provides 10x better performance than JSON, 2) Strong typing with .proto contracts reduces integration bugs, 3) Native support for bidirectional streaming enables real-time data flows, 4) HTTP/2 multiplexing reduces connection overhead. Trade-offs accepted: steeper learning curve and reduced browser compatibility (acceptable for internal services)."
                },
                new()
                {
                    DocumentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Title = "ADR-002: PostgreSQL as Primary Database",
                    Content = "In March 2024, we selected PostgreSQL over MongoDB for our primary database. Reasons: ACID compliance required for financial transactions, mature tooling ecosystem, excellent JSON support for semi-structured data, and lower operational costs. We use MongoDB only for specific document-heavy workloads."
                },
                new()
                {
                    DocumentId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Title = "ADR-003: Event-Driven Architecture with Kafka",
                    Content = "In February 2024, we adopted Apache Kafka for event-driven communication between bounded contexts. This decouples services, enables event sourcing, and provides replay capability for debugging. RabbitMQ was rejected due to limited horizontal scaling."
                }
            };

            // 5. Generate embeddings and upsert records
            foreach (var adr in sampleAdrs)
            {
                var embedding = await embeddingGenerator.GenerateAsync(adr.Content);
                adr.ContentVector = embedding.Vector;
            }

            var searchClient = new SearchClient(searchEndpoint, indexName, searchKey);

            await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(sampleAdrs));

            Console.WriteLine($"Seeded {sampleAdrs.Count} ADR records into Azure Search.\n");
        }

        public async Task ExecuteRAGSearch()
        {
            TextSearchProviderOptions textSearchOptions = new()
            {
                SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
            };

            AIAgent architectAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                Name = "EnterpriseArchitect",
                ChatOptions = new()
                {
                    Instructions = "You are a senior enterprise architect. Always reference the provided ADR context to answer questions about past architectural decisions. If you are not sure, decline request politely"
                },
                AIContextProviders = [new TextSearchProvider(VectorSearchAdapter, textSearchOptions)]
            });

            Console.WriteLine("--- Enterprise Architecture Agent Online --- \n");

            string query = "Why did we choose PostgreSQL as your preffered database in 2024 ?";
            Console.WriteLine($"User: {query}");

            AgentResponse response = await architectAgent.RunAsync(query);

            Console.WriteLine($"Agent: {response.Text}");
        }

        private async Task<IEnumerable<TextSearchProvider.TextSearchResult>> VectorSearchAdapter(string query, CancellationToken cancellationToken)
        {
            var searchClient = new SearchClient(searchEndpoint, indexName, searchKey);

            // Generate embedding for the user's query
            var queryEmbedding = await embeddingGenerator.GenerateAsync(query, cancellationToken: cancellationToken);
            var queryVector = queryEmbedding.Vector;

            var options = new SearchOptions
            {
                Select = { "DocumentId", "Title", "Content" }
            };
            options.VectorSearch = new VectorSearchOptions();

            options.VectorSearch.Queries.Add(new VectorizedQuery(queryVector)
            {
                KNearestNeighborsCount = 3,
                Fields = { "ContentVector" }
            });

            var response = await searchClient.SearchAsync<ArchitectureDecision>(query, options);

            var results = new List<TextSearchProvider.TextSearchResult>();

            await foreach (var item in response.Value.GetResultsAsync())
            {
                results.Add(new TextSearchProvider.TextSearchResult
                {
                    SourceName = $"ADR: {item.Document.Title}",
                    SourceLink = $"adr://{item.Document.DocumentId}",
                    Text = $"Title: {item.Document.Title}\nContent: {item.Document.Content}"
                });
            }

            return results;
        }
    }
}
