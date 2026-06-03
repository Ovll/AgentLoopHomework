using Anthropic;
using Anthropic.Models.Messages;
using DotNetEnv;

Env.Load();

string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("ERROR: ANTHROPIC_API_KEY was not found in .env.");
    return;
}

try
{
    AnthropicClient client = new() { ApiKey = apiKey };

    Console.WriteLine("Sending first request to Claude...");

    MessageCreateParams parameters = new()
    {
        Model = "claude-haiku-4-5-20251001",
        MaxTokens = 100,
        Messages =
        [
            new() { Role = Role.User, Content = "Reply with exactly: C# agent connection works." },
        ],
    };

    var message = await client.Messages.Create(parameters);

    Console.WriteLine();
    Console.WriteLine("[ASSISTANT]");
    Console.WriteLine(message);
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("The API request failed:");
    Console.WriteLine(ex.Message);
}
