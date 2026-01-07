namespace MRP_Client.Models;

public sealed class LoginResponse { public string? Token { get; set; } }

public sealed class MediaDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Type { get; set; }
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = "";
    public int AgeRestriction { get; set; }
    public double? AvgScore { get; set; }
    public List<string> LikedByUsers { get; set; } = new();
    public List<RatingDto>? Ratings { get; set; }
}

public sealed class RatingDto
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public int UserId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public bool Confirmed { get; set; }
    public int Likes { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public List<string> LikedByUsers { get; set; } = new();
}

public sealed class MediaDetailsResponse
{
    public MediaDto? Media { get; set; }
    public double? AvgScore { get; set; }
    public List<RatingDto> Ratings { get; set; } = new();
}

public sealed class ProfileDto
{
    public string Username { get; set; } = "";
    public int TotalRatings { get; set; }
    public double AvgScore { get; set; }
    public string FavoriteGenre { get; set; } = "";
}

public sealed class LeaderboardEntry
{
    public string Username { get; set; } = "";
    public double AvgScore { get; set; }
    public int TotalRatings { get; set; }
    public string FavoriteGenre { get; set; } = "";
}