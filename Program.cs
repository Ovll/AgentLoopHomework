using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentLoopHomework.Models;
using AgentLoopHomework.Tools;
using DotNetEnv;

Env.Load();

string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("ERROR: ANTHROPIC_API_KEY was not found in .env.");
    return;
}

using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
httpClient.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json")
);

List<Dictionary<string, object?>> messages =
[
    new()
    {
        ["role"] = "user",
        ["content"] = """
            Find me a funny episode under 30 minutes for tonight.
            Check that the best choice has English subtitles and HD quality.
            Then add it to my Tonight watchlist.

            Final answer must be ONLY valid JSON with exactly these fields:
            {
              "request": "...",
              "selectedTitle": "...",
              "reason": "...",
              "watchlistName": "...",
              "addedToWatchlist": true,
              "toolsUsed": ["search_library", "get_title_details", "add_to_watchlist"]
            }

            Do not include markdown in the final answer.
            You must use the available tools.
            """,
    },
];

int cycle = 0;
string finalText = "";

while (cycle < 10)
{
    cycle++;

    var requestBody = new
    {
        model = "claude-haiku-4-5-20251001",
        max_tokens = 1024,
        tools = CreateToolDefinitions(),
        messages = messages,
    };

    string requestJson = JsonSerializer.Serialize(requestBody);

    using StringContent content = new(requestJson, Encoding.UTF8, "application/json");

    Console.WriteLine();
    Console.WriteLine($"[HTTP] Sending request to Claude, cycle {cycle}...");

    using HttpResponseMessage httpResponse = await httpClient.PostAsync(
        "https://api.anthropic.com/v1/messages",
        content
    );

    Console.WriteLine($"[HTTP] Response received, status: {(int)httpResponse.StatusCode}");

    string responseJson = await httpResponse.Content.ReadAsStringAsync();

    if (!httpResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("API request failed:");
        Console.WriteLine(responseJson);
        return;
    }

    using JsonDocument document = JsonDocument.Parse(responseJson);
    JsonElement root = document.RootElement;

    string stopReason = root.GetProperty("stop_reason").GetString() ?? "";

    Console.WriteLine();
    Console.WriteLine($"========== MANUAL MODEL / TOOL CYCLE {cycle} ==========");
    Console.WriteLine($"Stop reason: {stopReason}");
    Console.WriteLine();

    JsonElement responseContent = root.GetProperty("content").Clone();

    List<object> toolResultBlocks = [];
    bool toolWasRequested = false;

    foreach (JsonElement block in responseContent.EnumerateArray())
    {
        string type = block.GetProperty("type").GetString() ?? "";

        if (type == "text")
        {
            string text = block.GetProperty("text").GetString() ?? "";

            Console.WriteLine("[MODEL TEXT]");
            Console.WriteLine(text);
            Console.WriteLine();

            if (stopReason == "end_turn")
            {
                finalText += text;
            }
        }
        else if (type == "tool_use")
        {
            toolWasRequested = true;

            string toolUseId = block.GetProperty("id").GetString() ?? "";
            string toolName = block.GetProperty("name").GetString() ?? "";
            JsonElement input = block.GetProperty("input");

            Console.WriteLine("[MODEL REQUESTED TOOL]");
            Console.WriteLine($"Tool: {toolName}");
            Console.WriteLine($"Input: {input}");
            Console.WriteLine();

            string toolResult = ExecuteTool(toolName, input);

            Console.WriteLine("[C# TOOL RESULT]");
            Console.WriteLine(toolResult);
            Console.WriteLine();

            toolResultBlocks.Add(
                new Dictionary<string, object?>
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = toolUseId,
                    ["content"] = toolResult,
                }
            );
        }
    }

    // We manually add Claude's assistant message into conversation history.
    messages.Add(
        new Dictionary<string, object?> { ["role"] = "assistant", ["content"] = responseContent }
    );

    if (!toolWasRequested || stopReason == "end_turn")
    {
        break;
    }

    // We manually add the tool result as the next user message.
    messages.Add(
        new Dictionary<string, object?> { ["role"] = "user", ["content"] = toolResultBlocks }
    );
}

Console.WriteLine();
Console.WriteLine("========== STRONGLY TYPED FINAL RESPONSE ==========");

try
{
    MovieNightFinalResponse? finalResponse = JsonSerializer.Deserialize<MovieNightFinalResponse>(
        finalText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );

    if (finalResponse is null)
    {
        Console.WriteLine("Could not parse final JSON.");
        return;
    }

    Console.WriteLine($"Request: {finalResponse.Request}");
    Console.WriteLine($"Selected title: {finalResponse.SelectedTitle}");
    Console.WriteLine($"Reason: {finalResponse.Reason}");
    Console.WriteLine($"Watchlist: {finalResponse.WatchlistName}");
    Console.WriteLine($"Added: {finalResponse.AddedToWatchlist}");
    Console.WriteLine($"Tools used: {string.Join(", ", finalResponse.ToolsUsed)}");
}
catch (Exception ex)
{
    Console.WriteLine("Final response was not valid JSON for the C# model.");
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Console.WriteLine("[RAW FINAL TEXT]");
    Console.WriteLine(finalText);
}

static object[] CreateToolDefinitions()
{
    return
    [
        new
        {
            name = "search_library",
            description = "Searches a mocked personal media library by genre, mood, maximum runtime, and media type.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    genre = new
                    {
                        type = "string",
                        description = "Genre to search for, for example Comedy.",
                    },
                    mood = new
                    {
                        type = "string",
                        description = "Mood to search for, for example Funny.",
                    },
                    maxRuntimeMinutes = new
                    {
                        type = "integer",
                        description = "Maximum allowed runtime in minutes.",
                    },
                    type = new
                    {
                        type = "string",
                        description = "Media type, for example Episode or Movie.",
                    },
                },
                required = new[] { "genre", "mood", "maxRuntimeMinutes", "type" },
            },
        },
        new
        {
            name = "get_title_details",
            description = "Gets full details for one media title using its id.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    titleId = new
                    {
                        type = "string",
                        description = "The id of the title, for example office-stress-relief.",
                    },
                },
                required = new[] { "titleId" },
            },
        },
        new
        {
            name = "add_to_watchlist",
            description = "Adds a media title to a mocked watchlist.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    titleId = new { type = "string", description = "The id of the title to add." },
                    watchlistName = new
                    {
                        type = "string",
                        description = "The watchlist name, for example Tonight.",
                    },
                },
                required = new[] { "titleId", "watchlistName" },
            },
        },
    ];
}

static string ExecuteTool(string toolName, JsonElement input)
{
    return toolName switch
    {
        "search_library" => MovieNightTools.SearchLibrary(
            genre: input.GetProperty("genre").GetString() ?? "",
            mood: input.GetProperty("mood").GetString() ?? "",
            maxRuntimeMinutes: input.GetProperty("maxRuntimeMinutes").GetInt32(),
            type: input.GetProperty("type").GetString() ?? ""
        ),

        "get_title_details" => MovieNightTools.GetTitleDetails(
            titleId: input.GetProperty("titleId").GetString() ?? ""
        ),

        "add_to_watchlist" => MovieNightTools.AddToWatchlist(
            titleId: input.GetProperty("titleId").GetString() ?? "",
            watchlistName: input.GetProperty("watchlistName").GetString() ?? ""
        ),

        _ => JsonSerializer.Serialize(new { Error = $"Unknown tool: {toolName}" }),
    };
}
