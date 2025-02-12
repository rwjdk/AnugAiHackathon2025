//RAG Example

using System.Text;
using System.Text.Json;
using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
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
const string chatModel = "gpt-4o-mini";
const string embeddingModel = "text-embedding-ada-002";
string azureSearchEndpoint = config["AiSearchEndpoint"]!;
string azureSearchKey = config["AiSearchKey"]!;
string cosmosConnectionString = config["CosmosConnectionString"]!;

//Settings
const VectorStoreToUse vectorStoreToUse = VectorStoreToUse.CosmosDb;

//Kernel
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(chatModel, azureOpenAiEndpoint, azureOpenAiKey);
kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(embeddingModel, azureOpenAiEndpoint, azureOpenAiKey);
var kernel = kernelBuilder.Build();
var embeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

var collection = GetCollection();
await AddDataToVectorStore(collection, embeddingGenerationService);

var agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "ComicBookNerd",
    Instructions = "You are a comic book nerd, that answer answer questions about Super Heroes." +
                   "DO NOT USE YOUR GENERAL KNOWLEDGE. ONLY THE SUPERHEROES I GIVE YOU!"
};

Console.OutputEncoding = Encoding.UTF8;
while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine()!;
    if (!string.IsNullOrWhiteSpace(input))
    {
        string[] searchResultData = await RagSearch(input);
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
            Sex = superHero.Sex,
            Name = superHero.Name,
            Description = description.ToString(),
            DescriptionEmbedding = vector
        });
    }
}

IVectorStoreRecordCollection<string, SuperHeroVectorEntity> GetCollection()
{
    IVectorStoreRecordCollection<string, SuperHeroVectorEntity> vectorStoreRecordCollection;
    switch (vectorStoreToUse)
    {
        case VectorStoreToUse.InMemory:
            InMemoryVectorStore inMemoryVectorStore = new InMemoryVectorStore();
            vectorStoreRecordCollection = inMemoryVectorStore.GetCollection<string, SuperHeroVectorEntity>("heroes");
            break;
        case VectorStoreToUse.AzureAiSearch:
            var azureAiSearchVectorStore = new AzureAISearchVectorStore(new SearchIndexClient(new Uri(azureSearchEndpoint), new AzureKeyCredential(azureSearchKey)));
            vectorStoreRecordCollection = azureAiSearchVectorStore.GetCollection<string, SuperHeroVectorEntity>("heroes");
            break;
        case VectorStoreToUse.CosmosDb:
            var cosmosClient = new CosmosClient(cosmosConnectionString, new CosmosClientOptions()
            {
                // When initializing CosmosClient manually, setting this property is required 
                // due to limitations in default serializer. 
                UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
            });
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
            var database = cosmosClient.GetDatabase("data");
            vectorStoreRecordCollection = new AzureCosmosDBNoSQLVectorStoreRecordCollection<SuperHeroVectorEntity>(database, "heroes", new()
            {
                PartitionKeyPropertyName = nameof(SuperHeroVectorEntity.Id),
                JsonSerializerOptions = jsonSerializerOptions
            });
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    return vectorStoreRecordCollection;
}

async Task<string[]> RagSearch(string input)
{
    List<string> searchResults = new List<string>();
    ReadOnlyMemory<float> searchVector = await embeddingGenerationService.GenerateEmbeddingAsync(input);
    var searchResult = await collection.VectorizedSearchAsync(searchVector, new()
    {
        //Filter = new VectorSearchFilter([new EqualToFilterClause(nameof(SuperHeroVectorEntity.Sex), "Female")]),
        //VectorPropertyName = nameof(SuperHeroVectorEntity.DescriptionEmbedding),
        Top = 5
    });

    await foreach (var record in searchResult.Results.Where(x => x.Score > 0.7))
    {
        searchResults.Add(record.Record.Description);
    }

    return searchResults.ToArray();
}