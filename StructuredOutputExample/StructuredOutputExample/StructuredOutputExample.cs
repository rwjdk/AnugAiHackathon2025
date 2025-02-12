using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using StructuredOutputExample.Models;

namespace StructuredOutputExample;

public static class StructuredOutputExample
{
    public static async Task Run()
    {
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

        //Configuration
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        string azureOpenAiEndpoint = config["AiEndpoint"]!;
        string azureOpenAiKey = config["AiKey"]!;
        const string chatModel = "gpt-4o-mini";

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(chatModel, azureOpenAiEndpoint, azureOpenAiKey);
        var kernel = kernelBuilder.Build();

        var agent = new ChatCompletionAgent
        {
            Kernel = kernel,
            Name = "MovieNerd",
            Instructions = "You are a Movie Expert",
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
            {
                ResponseFormat = typeof(MovieResult), //<-- This enables structured output (Must be an object (can't be a collection))
            })
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("What are the top 10 Movies according to IMDB?"); //This is technically not needed anymore, but help guide the AI
        await foreach (var response in agent.InvokeAsync(chatHistory)) //Note that streaming content back do not make sense in structured output
        {
            string json = response.Content!;
            var movieResult = JsonSerializer.Deserialize<MovieResult>(json, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() } //Needed if you use enums as LLM get them as name strings
            });
            int counter = 1;

            Console.WriteLine(movieResult!.MessageBack);
            foreach (var movie in movieResult.Top10Movies)
            {
                Console.WriteLine($"{counter}: {movie.Title} ({movie.YearOfRelease}) - Genre: {movie.Genre} - Director: {movie.Director} - IMDB Score: {movie.ImdbScore}");
                counter++;
            }
        }
    }
}