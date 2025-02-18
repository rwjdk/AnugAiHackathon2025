using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Plugins.Core;

#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050
IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var apiKey = config["geminiApiKey"]!;

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddGoogleAIGeminiChatCompletion("gemini-2.0-flash", apiKey);
var kernel = kernelBuilder.Build();
kernel.ImportPluginFromType<TimePlugin>();

var agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Instructions = "You are a friendly AI, helping the user to answer questions", //Give it some personality
    Arguments = new KernelArguments(new GeminiPromptExecutionSettings
    {
        Temperature = 0.5f,
        ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
        //You need to use Prompt-engineering to get structured output (Or at least I could not get it to work)
    })
};

var chatHistory = new ChatHistory();
while (true)
{
    Console.Write("> ");
    var inputFromUser = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(inputFromUser)) //Ignore if no user input
    {
        chatHistory.AddUserMessage(inputFromUser);
        await foreach (var response in agent.InvokeStreamingAsync(chatHistory, kernel: kernel))
        {
            Console.Write(response.Content);
        }
    }

    Console.WriteLine();
    Console.WriteLine(string.Empty.PadLeft(50, '*'));
    Console.WriteLine();
}

public class MyClass
{
    public string Message { get; set; }
    public string RandomFact { get; set; }
}