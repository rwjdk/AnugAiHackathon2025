//RAG Example
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using RagExample.Models;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

//Configuration
var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string azureOpenAiEndpoint = config["AiEndpoint"]!;
string azureOpenAiKey = config["AiKey"]!;
string chatModel = "gpt-4o-mini";
string embeddingModel = "text-embedding-ada-002";
string azureSearchEndpoint = config["AiSearchEndpoint"]!;
string azureSearchKey = config["AiSearchKey"]!;
string cosmosConnectionString = config["CosmosConnectionString"]!;

//Settings
VectorStoreToUse vectorStoreToUse = VectorStoreToUse.AzureAiSearch;
SearchMethod searchMethod = SearchMethod.TextSearch;

//Kernel
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(chatModel, azureOpenAiEndpoint, azureOpenAiKey);
kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModel, azureOpenAiEndpoint, azureOpenAiKey);
var kernel = kernelBuilder.Build();
var embeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var collection = GetCollection(vectorStoreToUse, azureSearchEndpoint, azureSearchKey, cosmosConnectionString);
//await AddDataToVectorStore(collection, embeddingGenerationService);

var agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "ComicBookNerd",
    Instructions = "You are a comic book nerd, that answer answer questions about Super Heroes." +
                   "DOT NOT USE YOUR GENERAL KNOWLEDGE. ONLY THE SUPERHEROES I GIVE YOU!"
};

Console.OutputEncoding = Encoding.UTF8;
while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine()!;
    if (!string.IsNullOrWhiteSpace(input))
    {
        var searchResultData = await RagSearch(input);
        var history = new ChatHistory();
        history.AddUserMessage($"Superheroes what match question: {string.Join($"{Environment.NewLine}***{Environment.NewLine}", searchResultData)}");
        history.AddUserMessage(input);
        await foreach (var content in agent.InvokeStreamingAsync(history))
        {
            Console.Write(content);
        }

        Console.WriteLine();
        Console.WriteLine("******************************************************");
        Console.WriteLine();
    }
}

async Task<string> GenerateSuperHeroJson(Kernel kernel1)
{
    var generatorAgent = new ChatCompletionAgent()
    {
        Name = "StanLee",
        Kernel = kernel1,
        Instructions = "You are a Comic Book Writer that generate new cool, but silly superheroes",
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
        {
            ResponseFormat = typeof(SuperHeroStructuredOutput)
        })
    };

    var history = new ChatHistory();
    history.AddUserMessage("Please generate 25 random superheroes");
    string superHeroJson = string.Empty;
    await foreach (var content in generatorAgent.InvokeAsync(history))
    {
        superHeroJson = content.ToString();
    }

    return superHeroJson;
}

async Task AddDataToVectorStore(IVectorStoreRecordCollection<string, SuperHeroVectorEntity> vectorStoreRecordCollection, ITextEmbeddingGenerationService textEmbeddingGenerationService)
{
    //string jsonData = await GenerateSuperHeroJson(kernel);
    string jsonData = File.ReadAllText("Data.json");
    await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();
    var data = JsonSerializer.Deserialize<SuperHeroStructuredOutput>(jsonData)!;
    foreach (var superHero in data.Heroes)
    {
        StringBuilder description = new StringBuilder();
        description.AppendLine($"Name: {superHero.Name}");
        description.AppendLine($"Sex: {superHero.Sex}");
        description.AppendLine($"Description: {superHero.Description}");
        description.AppendLine($"Strenght: {superHero.Strenght}");
        description.AppendLine($"Weakness: {superHero.Weakness}");
        description.AppendLine($"BackgroundStory: {superHero.BackgroundStory}");
        var vector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(description.ToString());
        await vectorStoreRecordCollection.UpsertAsync(new SuperHeroVectorEntity
        {
            Id = superHero.Id,
            Name = superHero.Name,
            Description = description.ToString(),
            DescriptionEmbedding = vector
        });
    }
}

IVectorStoreRecordCollection<string, SuperHeroVectorEntity> GetCollection(VectorStoreToUse vectorStoreToUse1, string s, string azureSearchKey1, string cosmosConnectionString1)
{
    IVectorStoreRecordCollection<string, SuperHeroVectorEntity> vectorStoreRecordCollection1;
    switch (vectorStoreToUse1)
    {
        case VectorStoreToUse.InMemory:
            var inMemoryVectorStore = new InMemoryVectorStore();
            vectorStoreRecordCollection1 = inMemoryVectorStore.GetCollection<string, SuperHeroVectorEntity>("heroes");
            break;
        case VectorStoreToUse.AzureAiSearch:
            var azureAiSearchVectorStore = new AzureAISearchVectorStore(new SearchIndexClient(new Uri(s), new AzureKeyCredential(azureSearchKey1)));
            vectorStoreRecordCollection1 = azureAiSearchVectorStore.GetCollection<string, SuperHeroVectorEntity>("heroes");
            break;
        case VectorStoreToUse.CosmosDb:
            var cosmosClient = new CosmosClient(cosmosConnectionString1, new CosmosClientOptions()
            {
                // When initializing CosmosClient manually, setting this property is required 
                // due to limitations in default serializer. 
                UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
            });
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
            var database = cosmosClient.GetDatabase("data");
            vectorStoreRecordCollection1 = new AzureCosmosDBNoSQLVectorStoreRecordCollection<SuperHeroVectorEntity>(database, "heroes", new()
            {
                PartitionKeyPropertyName = nameof(SuperHeroVectorEntity.Id),
                JsonSerializerOptions = jsonSerializerOptions
            });
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    return vectorStoreRecordCollection1;
}

async Task<string[]> RagSearch(string input)
{
    List<string> searchResults = new List<string>();
    switch (searchMethod)
    {
        case SearchMethod.TextSearch:
            var textSearch = new VectorStoreTextSearch<SuperHeroVectorEntity>(collection, embeddingGenerationService);
            KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(input, new() { Top = 5 });
            await foreach (TextSearchResult result in textResults.Results)
            {
                searchResults.Add(result.Value);
            }
            break;
        case SearchMethod.VectorSearch:
            var searchVector = await embeddingGenerationService.GenerateEmbeddingAsync(input);
            var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 5 });

            // Inspect the returned hotel.
            await foreach (var record in searchResult.Results.Where(x => x.Score > 0.7))
            {
                Console.WriteLine("Found Hero: " + record.Record.Description);
                Console.WriteLine("Found record score: " + record.Score);
            }
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    return searchResults.ToArray();
}