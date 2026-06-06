using System.Text.Json;
using AgentLoopHomework.Models;
using AgentLoopHomework.Tools;
using Anthropic;
using Anthropic.Helpers.Beta;
using Anthropic.Models.Beta.Messages;
using DotNetEnv;

Env.Load();

string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("ERROR: ANTHROPIC_API_KEY was not found in .env.");
    return;
}

AnthropicClient client = new() { ApiKey = apiKey };

List<BetaRunnableTool> tools =
[
    CreateSearchLibraryTool(),
    CreateGetTitleDetailsTool(),
    CreateAddToWatchlistTool(),
];

var runner = client.Beta.Messages.ToolRunner(
    new MessageCreateParams
    {
        Model = "claude-haiku-4-5-20251001",
        MaxTokens = 1024,
        Messages =
        [
            new()
            {
                Role = Role.User,
                Content = """
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
        ],
    },
    tools
);

int cycle = 0;
string finalText = "";

await foreach (var message in runner)
{
    cycle++;

    Console.WriteLine();
    Console.WriteLine($"========== MANUAL LOG CYCLE {cycle} ==========");
    Console.WriteLine($"Stop reason: {message.StopReason}");
    Console.WriteLine();

    foreach (var block in message.Content)
    {
        if (block.TryPickText(out var text))
        {
            Console.WriteLine("[MODEL TEXT]");
            Console.WriteLine(text.Text);
            Console.WriteLine();

            finalText = text.Text;
        }
        else if (block.TryPickToolUse(out var toolUse))
        {
            Console.WriteLine("[MODEL REQUESTED TOOL]");
            Console.WriteLine($"Tool: {toolUse.Name}");
            Console.WriteLine($"Input: {JsonSerializer.Serialize(toolUse.Input)}");
            Console.WriteLine();
        }
    }
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

static BetaRunnableTool CreateSearchLibraryTool()
{
    return new BetaRunnableTool
    {
        Name = "search_library",
        Definition = new BetaTool
        {
            Name = "search_library",
            Description =
                "Searches a mocked personal media library by genre, mood, maximum runtime, and media type.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["genre"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "string",
                            description = "Genre to search for, for example Comedy.",
                        }
                    ),
                    ["mood"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "string",
                            description = "Mood to search for, for example Funny.",
                        }
                    ),
                    ["maxRuntimeMinutes"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "integer",
                            description = "Maximum allowed runtime in minutes.",
                        }
                    ),
                    ["type"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "string",
                            description = "Media type, for example Episode or Movie.",
                        }
                    ),
                },
                Required = ["genre", "mood", "maxRuntimeMinutes", "type"],
            },
        },
        Run = (toolUse, _) =>
        {
            string genre = toolUse.Input.TryGetValue("genre", out var g) ? g.GetString() ?? "" : "";
            string mood = toolUse.Input.TryGetValue("mood", out var m) ? m.GetString() ?? "" : "";
            int maxRuntimeMinutes = toolUse.Input.TryGetValue("maxRuntimeMinutes", out var r)
                ? r.GetInt32()
                : 30;
            string type = toolUse.Input.TryGetValue("type", out var t) ? t.GetString() ?? "" : "";

            Console.WriteLine(
                $"[C# TOOL EXECUTED] search_library(genre={genre}, mood={mood}, maxRuntimeMinutes={maxRuntimeMinutes}, type={type})"
            );

            string result = MovieNightTools.SearchLibrary(genre, mood, maxRuntimeMinutes, type);

            return Task.FromResult<BetaToolResultBlockParamContent>(result);
        },
    };
}

static BetaRunnableTool CreateGetTitleDetailsTool()
{
    return new BetaRunnableTool
    {
        Name = "get_title_details",
        Definition = new BetaTool
        {
            Name = "get_title_details",
            Description = "Gets full details for one media title using its id.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["titleId"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "string",
                            description = "The id of the title, for example office-stress-relief.",
                        }
                    ),
                },
                Required = ["titleId"],
            },
        },
        Run = (toolUse, _) =>
        {
            string titleId = toolUse.Input.TryGetValue("titleId", out var id)
                ? id.GetString() ?? ""
                : "";

            Console.WriteLine($"[C# TOOL EXECUTED] get_title_details(titleId={titleId})");

            string result = MovieNightTools.GetTitleDetails(titleId);

            return Task.FromResult<BetaToolResultBlockParamContent>(result);
        },
    };
}

static BetaRunnableTool CreateAddToWatchlistTool()
{
    return new BetaRunnableTool
    {
        Name = "add_to_watchlist",
        Definition = new BetaTool
        {
            Name = "add_to_watchlist",
            Description = "Adds a media title to a mocked watchlist.",
            InputSchema = new InputSchema
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["titleId"] = JsonSerializer.SerializeToElement(
                        new { type = "string", description = "The id of the title to add." }
                    ),
                    ["watchlistName"] = JsonSerializer.SerializeToElement(
                        new
                        {
                            type = "string",
                            description = "The watchlist name, for example Tonight.",
                        }
                    ),
                },
                Required = ["titleId", "watchlistName"],
            },
        },
        Run = (toolUse, _) =>
        {
            string titleId = toolUse.Input.TryGetValue("titleId", out var id)
                ? id.GetString() ?? ""
                : "";
            string watchlistName = toolUse.Input.TryGetValue("watchlistName", out var w)
                ? w.GetString() ?? ""
                : "";

            Console.WriteLine(
                $"[C# TOOL EXECUTED] add_to_watchlist(titleId={titleId}, watchlistName={watchlistName})"
            );

            string result = MovieNightTools.AddToWatchlist(titleId, watchlistName);

            return Task.FromResult<BetaToolResultBlockParamContent>(result);
        },
    };
}
