using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;

namespace HelloWorldExample;

public static class HowWeDidIt
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
            Name = "FriendlyAI", //Warning name can't contain spaces
            Instructions = "You are a friendly AI, helping the user to answer questions",
        };

        var chatHistory = new ChatHistory();
        while (true)
        {
            Console.Write("> ");
            var inputFromUser = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(inputFromUser))
            {
                chatHistory.AddUserMessage(inputFromUser);
                await foreach (var response in agent.InvokeStreamingAsync(chatHistory))
                {
                    Console.Write(response.Content);
                }
            }

            Console.WriteLine();
            Console.WriteLine(string.Empty.PadLeft(50, '*'));
            Console.WriteLine();
        }
    }
}