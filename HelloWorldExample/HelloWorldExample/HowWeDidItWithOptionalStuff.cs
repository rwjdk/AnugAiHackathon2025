using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;

namespace HelloWorldExample;

public static class HowWeDidItWithOptionalStuff
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


        var answerBack = await kernel.InvokePromptAsync("Hi AI. Please Introduce yourself"); //Simple way to interact with Semantic Kernel. Agent below is the recommended way for more options
        Console.WriteLine(answerBack);
        Console.WriteLine();

        var agent = new ChatCompletionAgent
        {
            Kernel = kernel,
            Name = "FriendlyAI", //Warning name can't contain spaces
            Instructions = "You are a friendly AI, helping the user to answer questions",

            //Optional
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings
            {
                Temperature = 1, //Control how creative the model is (Between 0 and 2, Default is 1)
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), //This enable Function Calling (Not that we have any at the moment)
                //ResponseFormat = typeof(<someobject>), //This enables structured output (see separate example of that)
            }),
            HistoryReducer = new ChatHistoryTruncationReducer(3) //Will ensure the Chat-history to always be max 3 

        };

        var chatHistory = new ChatHistory();
        //Optional todo: Add anything you wish the AI to know up front (Like you name, age, preferences etc.) [Could also be in the Agent instructions if need be to ensure it is all chats]
        //chatHistory.AddUserMessage("My name is <your name here>");

        Console.OutputEncoding = Encoding.UTF8;

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

                    //Optional: Get how many tokens the interaction cost
                    if (response.Metadata?.TryGetValue("Usage", out object? usage) == true && usage is ChatTokenUsage chatTokenUsage)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"[Token Usage: {chatTokenUsage.InputTokenCount} In | {chatTokenUsage.OutputTokenCount} Out]");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine(string.Empty.PadLeft(50, '*'));
            Console.WriteLine();
            await agent.ReduceAsync(chatHistory); //optional - Reduce history
        }
    }
}