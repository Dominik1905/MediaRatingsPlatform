namespace DatabaseObjects;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    public List<Media> LikedMedia { get; set; } = new();
    public List<Rating> LikedRatings { get; set; } = new();
}