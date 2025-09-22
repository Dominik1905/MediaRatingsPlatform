namespace DatabaseObjects;

public class Rating
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public int UserId { get; set; }
    public int Stars { get; set; } // 1–5
    public string Comment { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
    public DateTime Timestamp { get; set; }

    public List<User> LikedByUsers { get; set; } = new();
}