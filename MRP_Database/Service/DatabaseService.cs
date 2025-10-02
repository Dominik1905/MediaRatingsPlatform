using Microsoft.Data.SqlClient;
using Npgsql;

namespace DatabaseObjects.Service;

public class DatabaseService
{
    private readonly string connectionString =
        "host=localhost;Username=myuser;Password=mypassword;Database=mydb;Port=15432";

    public DatabaseService()
    {
    }

    public void AddUser(User user)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.ExecuteNonQuery();
    }
    public User? GetUserByUsername(string username)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = "SELECT Id, Username, PasswordHash FROM Users WHERE Username = @Username;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
            };
        }

        return null; // Falls kein User gefunden wurde
    }
    public List<User> GetAllUsers()
    {
        var users = new List<User>();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        string sql = "SELECT * FROM Users;";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var user = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
            };

            users.Add(user);
        }

        return users;
    }
    public void InsertMedia(Media media)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = @"INSERT INTO Media (Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId)
                       VALUES (@Title, @Description, @Type, @ReleaseYear, @Genre, @AgeRestriction, @CreatedByUserId);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", media.Title);
        cmd.Parameters.AddWithValue("@Description", media.Description);
        cmd.Parameters.AddWithValue("@Type", (int)media.Type);
        cmd.Parameters.AddWithValue("@ReleaseYear", media.ReleaseYear);
        cmd.Parameters.AddWithValue("@Genre", media.Genre);
        cmd.Parameters.AddWithValue("@AgeRestriction", media.AgeRestriction);
        cmd.Parameters.AddWithValue("@CreatedByUserId", media.CreatedByUserId);
        cmd.ExecuteNonQuery();
    }
    public List<Media> GetAllMedia()
    {
        var list = new List<Media>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql =
            "SELECT Id, Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId FROM Media;";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Media
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Type = (MediaType)reader.GetInt32(3),
                ReleaseYear = reader.GetInt32(4),
                Genre = reader.GetString(5),
                AgeRestriction = reader.GetInt32(6),
                CreatedByUserId = reader.GetInt32(7)
            });
        }

        return list;
    }
    public Media? GetMediaById(int id)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql =
            "SELECT Id, Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId FROM Media WHERE Id = @Id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Media
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Type = (MediaType)reader.GetInt32(3),
                ReleaseYear = reader.GetInt32(4),
                Genre = reader.GetString(5),
                AgeRestriction = reader.GetInt32(6),
                CreatedByUserId = reader.GetInt32(7)
            };
        }

        return null;
    }
    public List<Media> SearchMediaByTitle(string titlePart)
    {
        var list = new List<Media>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = @"SELECT Id, Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId
                   FROM Media WHERE Title ILIKE @Title;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", NpgsqlTypes.NpgsqlDbType.Text, "%" + titlePart + "%");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Media
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Type = (MediaType)reader.GetInt32(3),
                ReleaseYear = reader.GetInt32(4),
                Genre = reader.GetString(5),
                AgeRestriction = reader.GetInt32(6),
                CreatedByUserId = reader.GetInt32(7)
            });
        }
        return list;
    }

    public void UpdateMedia(Media media)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = @"UPDATE Media 
                   SET Title=@Title, Description=@Description, Type=@Type, ReleaseYear=@ReleaseYear, Genre=@Genre, AgeRestriction=@AgeRestriction
                   WHERE Id=@Id AND CreatedByUserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", media.Title);
        cmd.Parameters.AddWithValue("@Description", media.Description);
        cmd.Parameters.AddWithValue("@Type", (int)media.Type);
        cmd.Parameters.AddWithValue("@ReleaseYear", media.ReleaseYear);
        cmd.Parameters.AddWithValue("@Genre", media.Genre);
        cmd.Parameters.AddWithValue("@AgeRestriction", media.AgeRestriction);
        cmd.Parameters.AddWithValue("@Id", media.Id);
        cmd.Parameters.AddWithValue("@UserId", media.CreatedByUserId);
        cmd.ExecuteNonQuery();
    }
    public void DeleteMedia(int id, int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = "DELETE FROM Media WHERE Id=@Id AND CreatedByUserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.ExecuteNonQuery();
    }


    // Rating einfügen
    public void InsertRating(Rating rating)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = @"INSERT INTO Ratings (MediaId, UserId, Stars, Comment, Confirmed, Timestamp)
                       VALUES (@MediaId, @UserId, @Stars, @Comment, @Confirmed, @Timestamp);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MediaId", rating.MediaId);
        cmd.Parameters.AddWithValue("@UserId", rating.UserId);
        cmd.Parameters.AddWithValue("@Stars", rating.Stars);
        cmd.Parameters.AddWithValue("@Comment", rating.Comment);
        cmd.Parameters.AddWithValue("@Confirmed", rating.Confirmed);
        cmd.Parameters.AddWithValue("@Timestamp", rating.Timestamp);
        cmd.ExecuteNonQuery();
    }

    // Many-to-Many Likes
    public void LikeMedia(int userId, int mediaId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = "INSERT INTO UserLikedMedia (UserId, MediaId) VALUES (@UserId, @MediaId);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        cmd.ExecuteNonQuery();
    }

    public void LikeRating(int userId, int ratingId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        string sql = "INSERT INTO UserLikedRatings (UserId, RatingId) VALUES (@UserId, @RatingId);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@RatingId", ratingId);
        cmd.ExecuteNonQuery();
    }
}