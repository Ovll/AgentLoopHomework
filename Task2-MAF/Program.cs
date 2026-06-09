using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
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

ChatClientAgent agent = new(
    chatClient,
    "MovieNightAgent",
    "A local movie night assistant with movie-library tools.",
    """
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
    7. Final answer must mention only the title returned by the tools.
    8. Do not say the title was added unless add_to_watchlist was actually called.

    Keep the final answer short.
    """,
    [searchFunnyEpisodesTool, getTitleDetailsTool, addToWatchlistTool],
    null,
    null
);

AgentResponse response = await agent.RunAsync(
    """
    Find me a funny episode under 30 minutes for tonight.
    Check that the best choice has English subtitles and HD quality.
    Then add it to my Tonight watchlist.
    """
);

Console.WriteLine();
Console.WriteLine("[AGENT RESPONSE]");
Console.WriteLine(response.Text);

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
