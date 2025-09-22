namespace DatabaseObjects;

public enum MediaType { Movie, Series, Game }
public class Media
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = string.Empty;
    public int AgeRestriction { get; set; }

    public int CreatedByUserId { get; set; }
    
    public List<Rating> Ratings { get; set; } = new();
    public List<User> LikedByUsers { get; set; } = new();
}