// Import packages

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;


#pragma warning disable format
#pragma warning disable SKEXP0070
#pragma warning disable CS8604

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set the ollama url and model variables
        var url = new Uri("http://localhost:11434");
        var model = "llama3.1:latest";

        // Create a new kernel builder
        var builder = Kernel.CreateBuilder();

        // Add the ollama services to the kernel
        builder.AddOllamaChatCompletion(model, url, "ollamaChat");

        // Build the kernel
        Kernel kernel = builder.Build();

        // Get the chat service
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Set the Planning and Chat Settings Parameters
        OllamaPromptExecutionSettings ollamaPromptSettings = new OllamaPromptExecutionSettings();
        ollamaPromptSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
        ollamaPromptSettings.Temperature = 0.7f;
        ollamaPromptSettings.TopP = 0.95f;

       // create an object of ChatHistory to track the conversation
        var history = new ChatHistory();

        // Add a System message
        history.AddSystemMessage("You are an AI assistant that helps people find information.");
  

        // ====================== Start the chat loop ======================
        string? userInput;
        do
        {
            // Collect user input
            Console.Write("User > ");
            userInput = Console.ReadLine();

            // Add user input
            history.AddUserMessage(userInput);

            // Get the response from the AI
            var result = await chatService.GetChatMessageContentAsync(
                history,
                executionSettings: ollamaPromptSettings,
                kernel: kernel);

            // Print the results
            Console.WriteLine("Assistant > " + result);

            // Add the message from the agent to the chat history
            history.AddAssistantMessage(result.ToString());
            Console.WriteLine(result.ToString());
        } while (userInput is not null);

    }
}
