// Import packages

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Text; // for chunking
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

using MyDataModel; // include MyDataModel from DataModel.cs

using UglyToad.PdfPig; // for pdf parsing
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.VectorData; 



// Dont worry about these they just suppress warnings
#pragma warning disable 
#pragma warning disable format
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050


internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set the ollama url and model variables
        var url = new Uri("http://localhost:11434");
        var embeddingModel = "nomic-embed-text:latest"; // The model used for generating embeddings -> you can use any model from the Ollama model zoo just make sure to use the correct dimentions in the DataModel.cs file
        var model = "llama3.1:latest";

        // Create a new kernel builder
        var builder = Kernel.CreateBuilder();

        // Add the ollama services to the kernel
        builder.AddOllamaTextEmbeddingGeneration(embeddingModel, url, "ollamaEmbedding").AddOllamaChatCompletion(model, url, "ollamaChat");

        // Build the kernel
        Kernel kernel = builder.Build();
        var history = new ChatHistory();
        
        // Set the Planning and Chat Settings Parameters
        OllamaPromptExecutionSettings ollamaPromptSettings = new OllamaPromptExecutionSettings();
        ollamaPromptSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
        ollamaPromptSettings.Temperature = 0.7f;
        ollamaPromptSettings.TopP = 0.95f;


        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Get the embedding service
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        // Extract text from a pdf file and chunk it -- Look at the ExtractTextFromPdf and Chunker functions below
        string allText = ExtractTextFromPdf(@"mohre.pdf");
        var chunkedData = Chunker(allText);

        // Generate embeddings for the data to be stored in the vector store
        var embedings = await embeddingGenerator.GenerateEmbeddingsAsync(chunkedData);

        // Construct the vector store and get the collection - we are using an in-memory store as a vector store
        var vectorStore = new InMemoryVectorStore();

        // Here we are using the DocumentData class from the MyDataModel namespace which has the required attributes for the vector store
        var collection = vectorStore.GetCollection<string,DocumentData>("skglossary");

        // Create a collection in the Vector Store if it does not exist
        await collection.CreateCollectionIfNotExistsAsync();

 
        List<DocumentData> documents = new List<DocumentData>();
        for (int i = 0; i < chunkedData.Count; i++)
        {
            var documentData = new DocumentData
            {
                Key = (i + 1).ToString(),
                content = chunkedData[i],
                ContentEmbedding = embedings[i] // Assuming embedings is defined elsewhere
            };
            documents.Add(documentData);
        }

        // Insert the text with the embeddings into the collection
        await foreach (var result in collection.UpsertBatchAsync(documents))
        {
            // Handle each result if needed

        }

        // Search the collection using VectorizedSearch 
        var vectorSearchOptions = new VectorSearchOptions
        {
            Top = 5,
            Skip = 0,
            VectorPropertyName = nameof(DocumentData.ContentEmbedding)

        };
        // ====================== Start the chat loop ======================
        string? userInput;
        do
        {
            // Collect user input
            Console.Write("User > ");
            userInput = Console.ReadLine();
            // Generate embeddings for the user input
            var searchTextEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(userInput);

            var searchResult = await collection.VectorizedSearchAsync(searchTextEmbedding,vectorSearchOptions);
            var searchResultItems = await searchResult.Results.ToListAsync();
            foreach (var results in searchResultItems)
            {
                history.AddUserMessage(results.Record.content);
            }
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


    // ===================== Extract text from a pdf file =====================
        private static string ExtractTextFromPdf(string filePath)
    {
        using PdfDocument document = PdfDocument.Open(filePath);
        var allText = new System.Text.StringBuilder();

        foreach (Page page in document.GetPages())
        {
            allText.AppendLine(page.Text);
        }
        
        return allText.ToString();
    }


    // ===================== Chunk the text to be embedded =====================
        private static List<string> Chunker(string text)
    {
        var lines = TextChunker.SplitPlainTextLines(text, 40);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 120);
        return paragraphs;
    }

}

