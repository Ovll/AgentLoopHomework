using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

Console.WriteLine("Task 2 - Microsoft Agent Framework with local Ollama");
Console.WriteLine();

IChatClient chatClient = new OllamaApiClient(
    uriString: "http://localhost:11434",
    defaultModel: "llama3.2:3b"
);

ChatClientAgent agent = new(
    chatClient,
    "MovieNightAgent",
    "A local movie night assistant.",
    """
    You are a helpful movie night assistant.
    Keep answers short and practical.
    """,
    [],
    null,
    null
);

AgentResponse response = await agent.RunAsync(
    "Reply with exactly: MAF local agent works."
);

Console.WriteLine("[AGENT RESPONSE]");
Console.WriteLine(response.Text);
