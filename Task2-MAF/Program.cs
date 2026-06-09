using Microsoft.Extensions.AI;
using OllamaSharp;

Console.WriteLine("Task 2 - Local model test with Ollama");
Console.WriteLine();

IChatClient chatClient = new OllamaApiClient(
    uriString: "http://localhost:11434",
    defaultModel: "llama3.2:3b"
);

ChatResponse response = await chatClient.GetResponseAsync(
    "Reply with exactly: C# local agent connection works."
);

Console.WriteLine("[LOCAL MODEL]");
Console.WriteLine(response.Text);
