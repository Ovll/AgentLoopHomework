using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Task2_MAF.Models;
using Task2_MAF.Tools;

Console.WriteLine("Task 2 - Microsoft Agent Framework with local Ollama and Movie Night tools");
Console.WriteLine();

IChatClient baseChatClient = new OllamaApiClient(
    uriString: "http://localhost:11434",
    defaultModel: "qwen2.5:7b"
);

IChatClient chatClient = new ChatClientBuilder(baseChatClient).UseFunctionInvocation().Build();

AITool searchFunnyEpisodesTool = AIFunctionFactory.Create(
    SearchFunnyEpisodes,
    name: "search_funny_episodes",
    description: "Searches the mocked library for funny comedy episodes under a maximum runtime."
);

AITool getTitleDetailsTool = AIFunctionFactory.Create(
    GetTitleDetails,
    name: "get_title_details",
    description: "Gets full details for a media title by id. Use this before recommending a title."
);

AITool addToWatchlistTool = AIFunctionFactory.Create(
    AddToWatchlist,
    name: "add_to_watchlist",
    description: "Adds a media title to a mocked watchlist. Use this only after checking title details."
);

// System prompt: sent to the model as a system-role message before the conversation starts.
// This is where you define the agent's persona, constraints, and required workflow.
string systemPrompt = """
    You are a movie night agent.

    You must use tools. Do not invent titles.

    Required process:
    1. Call search_funny_episodes with maxRuntimeMinutes = 30.
    2. Choose the best title id from the search result. Prefer the highest rating.
    3. Call get_title_details with that exact title id.
    4. Check that subtitles contain English.
    5. Check that quality is HD or Full HD.
    6. If the checks pass, call add_to_watchlist with:
       - the same title id
       - watchlistName = "Tonight"
    7. Do not say the title was added unless add_to_watchlist was actually called.
    """;

ChatClientAgent agent = new(
    chatClient,
    "MovieNightAgent",
    "A local movie night assistant with movie-library tools.",
    systemPrompt,
    [searchFunnyEpisodesTool, getTitleDetailsTool, addToWatchlistTool],
    null,
    null
);

// Step 1: run the agent — tools fire here, MAF handles the loop automatically.
AgentResponse agentResponse = await agent.RunAsync(
    """
    Find me a funny episode under 30 minutes for tonight.
    Check that the best choice has English subtitles and HD quality.
    Then add it to my Tonight watchlist.
    """
);

Console.WriteLine();
Console.WriteLine("[AGENT SUMMARY]");
Console.WriteLine(agentResponse.Text);

// Step 2: structured output — ask the base client to reformat the agent's summary
// as a typed object. GetResponseAsync<T> sets the JSON schema on the request so the
// model is constrained to produce a valid MovieNightFinalResponse without manual parsing.
Console.WriteLine();
Console.WriteLine("========== STRONGLY TYPED FINAL RESPONSE ==========");

List<ChatMessage> formatMessages =
[
    new(ChatRole.System, "Convert the agent summary into the requested JSON schema. Output only valid JSON, no markdown."),
    new(ChatRole.User, agentResponse.Text),
];

// useJsonSchemaResponseFormat: false — qwen2.5 via Ollama does not support schema-constrained
// output, so we rely on prompt guidance instead of a native schema constraint.
ChatResponse<MovieNightFinalResponse> typed = await baseChatClient.GetResponseAsync<MovieNightFinalResponse>(
    formatMessages,
    useJsonSchemaResponseFormat: false
);

if (typed.Result is null)
{
    Console.WriteLine("Could not deserialize typed response.");
}
else
{
    Console.WriteLine($"Request:        {typed.Result.Request}");
    Console.WriteLine($"Selected title: {typed.Result.SelectedTitle}");
    Console.WriteLine($"Reason:         {typed.Result.Reason}");
    Console.WriteLine($"Watchlist:      {typed.Result.WatchlistName}");
    Console.WriteLine($"Added:          {typed.Result.AddedToWatchlist}");
    Console.WriteLine($"Tools used:     {string.Join(", ", typed.Result.ToolsUsed)}");
}

static string SearchFunnyEpisodes(
    [Description("Maximum allowed runtime in minutes.")] int maxRuntimeMinutes
)
{
    Console.WriteLine();
    Console.WriteLine($"[MAF TOOL EXECUTED] search_funny_episodes({maxRuntimeMinutes})");

    string result = MovieNightTools.SearchLibrary(
        genre: "Comedy",
        mood: "Funny",
        maxRuntimeMinutes: maxRuntimeMinutes,
        type: "Episode"
    );

    Console.WriteLine("[TOOL RESULT]");
    Console.WriteLine(result);

    return result;
}

static string GetTitleDetails(
    [Description("The exact title id from the search result.")] string titleId
)
{
    Console.WriteLine();
    Console.WriteLine($"[MAF TOOL EXECUTED] get_title_details({titleId})");

    string result = MovieNightTools.GetTitleDetails(titleId);

    Console.WriteLine("[TOOL RESULT]");
    Console.WriteLine(result);

    return result;
}

static string AddToWatchlist(
    [Description("The exact title id to add.")] string titleId,
    [Description("The watchlist name. Use Tonight.")] string watchlistName
)
{
    Console.WriteLine();
    Console.WriteLine($"[MAF TOOL EXECUTED] add_to_watchlist({titleId}, {watchlistName})");

    string result = MovieNightTools.AddToWatchlist(titleId, watchlistName);

    Console.WriteLine("[TOOL RESULT]");
    Console.WriteLine(result);

    return result;
}
