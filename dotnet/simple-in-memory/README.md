
## Project Structure

- `Program.cs`: The main entry point of the application. It sets up the Semantic Kernel, generates embeddings, stores them in an in-memory vector store, and performs a vectorized search.
- `DataModel.cs`: Contains the `DocumentData` class used to store documents in the vector store.

## Simple-In-Memory

The application performs the following steps:

1. Sets up the Semantic Kernel with the Ollama text embedding generation service.
2. Generates embeddings for a sample text.
3. Stores the text and its embeddings in an in-memory vector store.
4. Generates embeddings for a search query.
5. Searches the vector store using the generated embeddings and prints the search result.
