using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace StructuredOutputExample;

public static class NoStructuredOutput
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
            Instructions = "You are a Movie Expert"
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("What are the top 10 Movies according to IMDB?");
        await foreach (var response in agent.InvokeAsync(chatHistory))
        {
            Console.WriteLine(response.Content);
        }
    }
}