using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string azureOpenAiEndpoint = config["AiEndpoint"]!;
string azureOpenAiKey = config["AiKey"]!;
const string chatModel = "gpt-4o-mini";

//https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.OpenAI
IChatClient chatClient = new ChatClientBuilder(
        new AzureOpenAIClient(new Uri(azureOpenAiEndpoint), new AzureKeyCredential(azureOpenAiKey)).AsChatClient(modelId: chatModel))
    .UseFunctionInvocation()
.Build();

ChatOptions chatOptions = new()
{
    //ResponseFormat = //Gave up after 30 min of trying to find a simple mode without defining it as string :-( SK is much better!!! 
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

List<ChatMessage> messages = [];
while (true)
{
    Console.Write("> ");
    var inputFromUser = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(inputFromUser))
    {
        messages.Add(new ChatMessage(ChatRole.User, inputFromUser));
        await foreach (var response in chatClient.CompleteStreamingAsync(messages, chatOptions))
        {
            Console.Write(response.Text);
        }
    }
    messages.Clear();

    Console.WriteLine();
    Console.WriteLine(string.Empty.PadLeft(50, '*'));
    Console.WriteLine();
}

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
public class MyClass
{
    public string Name { get; set; }
    public int Age { get; set; }
}