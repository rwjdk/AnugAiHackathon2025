using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Plugins.Core;

#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050
const string chatModel = "llama3.2";
const string endpoint = "http://127.0.0.1:11434";

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOllamaChatCompletion(modelId: chatModel, new Uri(endpoint));
var kernel = kernelBuilder.Build();
kernel.ImportPluginFromType<TimePlugin>();

var agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Instructions = "You are a friendly AI, helping the user to answer questions", //Give it some personality
    Arguments = new KernelArguments(new OllamaPromptExecutionSettings
    {
        Temperature = 0.5f,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        //Note: Structured output is not supported
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
        await foreach (var response in agent.InvokeAsync(chatHistory)) //Note: Ollama do not support function calling in streaming mode
        {
            Console.Write(response.Content);
        }
    }

    Console.WriteLine();
    Console.WriteLine(string.Empty.PadLeft(50, '*'));
    Console.WriteLine();
}