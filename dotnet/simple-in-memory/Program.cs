// Import packages

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;

using MyDataModel; // include MyDataModel from DataModel.cs


// Dont worry about these they just suppress warnings
#pragma warning disable format
#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0001


internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set the ollama url and model variables
        var url = new Uri("http://localhost:11434");
        var embeddingModel = "mxbai-embed-large:latest"; // The model used for generating embeddings -> you can use any model from the Ollama model zoo just make sure to use the correct dimentions in the DataModel.cs file

        // Create a new kernel builder
        var builder = Kernel.CreateBuilder();

        // Add the ollama services to the kernel
        builder.AddOllamaTextEmbeddingGeneration(embeddingModel, url, "ollamaEmbedding");

        // Build the kernel
        Kernel kernel = builder.Build();

        // Get the embedding service
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        // Generate embeddings for the data to be stored in the vector store
        var embedings = await embeddingGenerator.GenerateEmbeddingAsync("Application Programming Interface. A set of rules and specifications that allow software components to communicate and exchange data.");

        // Construct the vector store and get the collection - we are using an in-memory store as a vector store
        var vectorStore = new InMemoryVectorStore();

        // Here we are using the DocumentData class from the MyDataModel namespace which has the required attributes for the vector store
        var collection = vectorStore.GetCollection<string,DocumentData>("skglossary");

        // Create a collection in the Vector Store if it does not exist
        await collection.CreateCollectionIfNotExistsAsync();

        // Create a sample text to be stored in the collection
        var documentData = new DocumentData
       {
            Key = "1",
            content = "Application Programming Interface. A set of rules and specifications that allow software components to communicate and exchange data.",
            ContentEmbedding = embedings
        };

        // Insert the text with the embeddings into the collection
        await collection.UpsertAsync(documentData);

        // Lets search for the text we just inserted , we are using the VectorizedSearch method to search the collection
        var searchText = "What is an Application Programming Interface";

        // Generate embeddings for the search string
        var searchTextEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(searchText);

        // Search the collection using VectorizedSearch 
        var searchResult = await collection.VectorizedSearchAsync(searchTextEmbedding,new(){Top = 1});
        var searchResultItems = await searchResult.Results.ToListAsync();

        // Print the search result
        Console.WriteLine("========== Search Result ==========");
        Console.WriteLine();
        Console.WriteLine("Search Result: " + searchResultItems.First().Record.content);
        Console.WriteLine();
        Console.WriteLine("===================================");

    }
}
