namespace DatabaseObjects;

public sealed class LeaderboardEntry
{
    public string Username { get; set; } = "";
    public int TotalRatings { get; set; }
    public double AvgScore { get; set; }
    public string FavoriteGenre { get; set; } = "";
}