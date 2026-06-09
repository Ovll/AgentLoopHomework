namespace Task2_MAF.Models;

public class MovieNightFinalResponse
{
    public string Request { get; set; } = "";
    public string SelectedTitle { get; set; } = "";
    public string Reason { get; set; } = "";
    public string WatchlistName { get; set; } = "";
    public bool AddedToWatchlist { get; set; }
    public List<string> ToolsUsed { get; set; } = [];
}
