using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Plugins.Core;

namespace FunctionCalling.ShoppingLists
{
    public static class ShoppingListUtil
    {
        public static async Task Run()
        {
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050

            const bool httpLoggingActive = false;

            var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            string azureOpenAiEndpoint = config["AiEndpoint"]!;
            string azureOpenAiKey = config["AiKey"]!;
            const string chatModel = "gpt-4o-mini";
            //const string chatModel = "gpt-4o";
            //const string chatModel = "o3-mini";
            
            
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter, FunctionCallingFilter>();

            if (httpLoggingActive)
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(chatModel, azureOpenAiEndpoint, azureOpenAiKey, httpClient: new HttpClient(new LoggingDelegatingHandler()));
            }
            else
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(chatModel, azureOpenAiEndpoint, azureOpenAiKey);
            }

            var kernel = kernelBuilder.Build();

            kernel.ImportPluginFromObject(new ShoppingListPlugin());
            kernel.ImportPluginFromType<HttpPlugin>();
            kernel.ImportPluginFromType<TimePlugin>();

            var agent = new ChatCompletionAgent
            {
                Kernel = kernel,
                Instructions =
@"You should help users adding needed ingredients to their shopping list based on recipes.
Some ingredients should be ignored as I have them already.
Before adding ingredients to the shopping list check whether they are already there.
If the recipe is in another language than english please translate the ingredients and the unit names into english before adding or updating the shoppinglist.
If an ingredient is already on the shopping list then remove the existing ingredient and add a new ingredient with the old amount plus the new amount combined
(example1: If 1 egg is on the list and the recipe requires 2 eggs then remove the ingredient with 1 egg and add an ingredient with 3 eggs (1 + 2 = 3).
(example2: If 1 liter of milk is on the list and the recipe requires 2½ liter of milk then remove the ingredient with 1 liter milk and add an ingredient with 3½ liter milk (1 + 2½ = 3½)).
(example3: If 200g beef is on the list and the recipe requires 1 kg beef then remove the ingredient with 200g beef and add an ingredient with 1.2kg beef (1kg = 1000g and 200g + 1000g = 1, 200g = 1.2kg)."
            };

            PromptExecutionSettings promptExecutionSettings = new AzureOpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            };

            while (true)
            {
                Console.Write("> ");
                var inputFromUser = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(inputFromUser))
                {
                    var chatHistory = new ChatHistory();
                    chatHistory.AddUserMessage(inputFromUser);
                    await foreach (var response in agent.InvokeStreamingAsync(chatHistory, new KernelArguments(promptExecutionSettings)))
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
}
