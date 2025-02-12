using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string openAiKey = config["AiKey"]!;
const string chatModel = "gpt-4o-mini";


var client = new OpenAIClient(openAiKey);
ChatClient chatClient = client.GetChatClient(chatModel);

List<ChatMessage> messages = [];
while (true)
{
    Console.Write("> ");
    var inputFromUser = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(inputFromUser))
    {
        messages.Add(new UserChatMessage(inputFromUser));
        await foreach (var response in chatClient.CompleteChatStreamingAsync(messages))
        {
            if (response.ContentUpdate.Count > 0)
            {
                Console.Write(response.ContentUpdate[0].Text);
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine(string.Empty.PadLeft(50, '*'));
    Console.WriteLine();
}