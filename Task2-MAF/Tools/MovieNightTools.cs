using System.Text.Json;

namespace Task2_MAF.Tools;

public static class MovieNightTools
{
    private record MediaTitle(
        string Id,
        string Title,
        string Type,
        string Genre,
        string Mood,
        int RuntimeMinutes,
        string Quality,
        string[] Subtitles,
        double Rating,
        string Synopsis
    );

    private static readonly List<MediaTitle> Library =
    [
        new(
            Id: "scrubs-s01e01",
            Title: "Scrubs - My First Day",
            Type: "Episode",
            Genre: "Comedy",
            Mood: "Funny",
            RuntimeMinutes: 24,
            Quality: "HD",
            Subtitles: ["English", "Hebrew"],
            Rating: 8.4,
            Synopsis: "A new intern faces his chaotic first day at the hospital."
        ),
        new(
            Id: "office-stress-relief",
            Title: "The Office - Stress Relief",
            Type: "Episode",
            Genre: "Comedy",
            Mood: "Funny",
            RuntimeMinutes: 22,
            Quality: "Full HD",
            Subtitles: ["English"],
            Rating: 9.7,
            Synopsis: "A disastrous safety drill turns the office into complete chaos."
        ),
        new(
            Id: "community-pilot",
            Title: "Community - Pilot",
            Type: "Episode",
            Genre: "Comedy",
            Mood: "Funny",
            RuntimeMinutes: 25,
            Quality: "SD",
            Subtitles: ["English"],
            Rating: 7.8,
            Synopsis: "A former lawyer starts a fake study group at community college."
        ),
        new(
            Id: "interstellar",
            Title: "Interstellar",
            Type: "Movie",
            Genre: "Science Fiction",
            Mood: "Epic",
            RuntimeMinutes: 169,
            Quality: "4K",
            Subtitles: ["English", "Hebrew"],
            Rating: 8.7,
            Synopsis: "Explorers travel through a wormhole in search of humanity's future."
        ),
    ];

    private static readonly Dictionary<string, List<string>> Watchlists = new();

    public static string SearchLibrary(
        string genre,
        string mood,
        int maxRuntimeMinutes,
        string type
    )
    {
        var results = Library
            .Where(item =>
                item.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase)
                && item.Mood.Equals(mood, StringComparison.OrdinalIgnoreCase)
                && item.RuntimeMinutes <= maxRuntimeMinutes
                && item.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
            )
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Type,
                item.RuntimeMinutes,
                item.Rating,
            })
            .ToList();

        return JsonSerializer.Serialize(
            new
            {
                SearchCriteria = new
                {
                    Genre = genre,
                    Mood = mood,
                    MaxRuntimeMinutes = maxRuntimeMinutes,
                    Type = type,
                },
                Results = results,
            }
        );
    }

    public static string GetTitleDetails(string titleId)
    {
        var item = Library.FirstOrDefault(title =>
            title.Id.Equals(titleId, StringComparison.OrdinalIgnoreCase)
        );

        if (item is null)
        {
            return JsonSerializer.Serialize(new { Error = $"No title found with id '{titleId}'." });
        }

        return JsonSerializer.Serialize(
            new
            {
                item.Id,
                item.Title,
                item.Type,
                item.Genre,
                item.Mood,
                item.RuntimeMinutes,
                item.Quality,
                item.Subtitles,
                item.Rating,
                item.Synopsis,
            }
        );
    }

    public static string AddToWatchlist(string titleId, string watchlistName)
    {
        var item = Library.FirstOrDefault(title =>
            title.Id.Equals(titleId, StringComparison.OrdinalIgnoreCase)
        );

        if (item is null)
        {
            return JsonSerializer.Serialize(
                new { Success = false, Error = $"No title found with id '{titleId}'." }
            );
        }

        if (!Watchlists.ContainsKey(watchlistName))
        {
            Watchlists[watchlistName] = [];
        }

        Watchlists[watchlistName].Add(item.Title);

        return JsonSerializer.Serialize(
            new
            {
                Success = true,
                WatchlistName = watchlistName,
                AddedTitle = item.Title,
                Message = $"'{item.Title}' was added to the '{watchlistName}' watchlist.",
            }
        );
    }
}
