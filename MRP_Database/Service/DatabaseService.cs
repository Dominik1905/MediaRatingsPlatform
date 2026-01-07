using Npgsql;
using DatabaseObjects;
using System.Text;

namespace DatabaseObjects.Service;

public class DatabaseService
{
    private readonly string connectionString =
        "host=localhost;Username=myuser;Password=mypassword;Database=mydb;Port=15432";

    // --- USERS ---

    public void AddUser(User user)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.ExecuteNonQuery();
    }

    public User? GetUserByUsername(string username)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "SELECT Id, Username, PasswordHash FROM Users WHERE Username = @Username;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
        };
    }

    public List<User> GetAllUsers()
    {
        var users = new List<User>();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = "SELECT Id, Username, PasswordHash FROM Users;";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
            });
        }

        return users;
    }

    // --- MEDIA ---

    public int InsertMedia(Media media)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = @"INSERT INTO Media (Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId)
                             VALUES (@Title, @Description, @Type, @ReleaseYear, @Genre, @AgeRestriction, @CreatedByUserId)
                             RETURNING Id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", media.Title);
        cmd.Parameters.AddWithValue("@Description", media.Description);
        cmd.Parameters.AddWithValue("@Type", (int)media.Type);
        cmd.Parameters.AddWithValue("@ReleaseYear", media.ReleaseYear);
        cmd.Parameters.AddWithValue("@Genre", media.Genre);
        cmd.Parameters.AddWithValue("@AgeRestriction", media.AgeRestriction);
        cmd.Parameters.AddWithValue("@CreatedByUserId", media.CreatedByUserId);

        var idObj = cmd.ExecuteScalar();
        var id = Convert.ToInt32(idObj);
        media.Id = id;
        return id;
    }


    public List<Media> GetAllMedia()
    {
        var list = new List<Media>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql =
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

        string sql = @"
        SELECT
            m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId,
            COALESCE(
                array_agg(DISTINCT u.Username) FILTER (WHERE u.Username IS NOT NULL),
                ARRAY[]::text[]
            ) AS liked_by
        FROM Media m
        LEFT JOIN UserLikedMedia ulm ON ulm.MediaId = m.Id
        LEFT JOIN Users u ON u.Id = ulm.UserId
        WHERE m.Id = @Id
        GROUP BY m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId;
    ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var likedArr = reader.GetFieldValue<string[]>(8);

        return new Media
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.GetString(2),
            Type = (MediaType)reader.GetInt32(3),
            ReleaseYear = reader.GetInt32(4),
            Genre = reader.GetString(5),
            AgeRestriction = reader.GetInt32(6),
            CreatedByUserId = reader.GetInt32(7),
            LikedByUsers = likedArr.ToList()
        };
    }


    public List<Media> SearchMediaByTitle(string titlePart)
    {
        var list = new List<Media>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = @"SELECT Id, Title, Description, Type, ReleaseYear, Genre, AgeRestriction, CreatedByUserId
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
    
    // Filters: title (partial), genre, type, release year (exact or range), age restriction, min average rating.
    // Sort: title, year, score.
    
    public List<MediaWithScore> GetMediaFiltered(MediaQueryOptions options)
    {
        var list = new List<MediaWithScore>();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        var sb = new StringBuilder();
        sb.Append(@"
SELECT
  m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId,
  COALESCE(AVG(r.Stars), 0) AS AvgScore
FROM Media m
LEFT JOIN Ratings r ON r.MediaId = m.Id
WHERE 1=1
");

        // WHERE-filters
        if (!string.IsNullOrWhiteSpace(options.TitlePart))
            sb.Append(" AND m.Title ILIKE @Title ");

        if (!string.IsNullOrWhiteSpace(options.Genre))
            sb.Append(" AND m.Genre ILIKE @Genre ");

        if (options.Type.HasValue)
            sb.Append(" AND m.Type = @Type ");

        if (options.ReleaseYear.HasValue)
            sb.Append(" AND m.ReleaseYear = @Year ");

        if (options.ReleaseYearFrom.HasValue)
            sb.Append(" AND m.ReleaseYear >= @YearFrom ");

        if (options.ReleaseYearTo.HasValue)
            sb.Append(" AND m.ReleaseYear <= @YearTo ");

        if (options.AgeRestriction.HasValue)
            sb.Append(" AND m.AgeRestriction = @AgeRestriction ");

        sb.Append(@"
GROUP BY m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId
");

        // HAVING-filter (aggregate)
        if (options.MinAverageRating.HasValue)
            sb.Append(" HAVING COALESCE(AVG(r.Stars), 0) >= @MinAvgRating ");

        // ORDER BY 
        var orderBy = options.SortBy?.ToLowerInvariant() switch
        {
            "year" => "m.ReleaseYear",
            "score" => "AvgScore",
            _ => "m.Title"
        };
        var dir = options.SortDescending ? "DESC" : "ASC";
        sb.Append($" ORDER BY {orderBy} {dir}, m.Id ASC ");

        // Optional paging
        if (options.Limit.HasValue)
            sb.Append(" LIMIT @Limit ");
        if (options.Offset.HasValue)
            sb.Append(" OFFSET @Offset ");

        using var cmd = new NpgsqlCommand(sb.ToString(), conn);

        if (!string.IsNullOrWhiteSpace(options.TitlePart))
            cmd.Parameters.AddWithValue("@Title", NpgsqlTypes.NpgsqlDbType.Text, "%" + options.TitlePart + "%");

        if (!string.IsNullOrWhiteSpace(options.Genre))
            cmd.Parameters.AddWithValue("@Genre", NpgsqlTypes.NpgsqlDbType.Text, options.Genre);

        if (options.Type.HasValue)
            cmd.Parameters.AddWithValue("@Type", (int)options.Type.Value);

        if (options.ReleaseYear.HasValue)
            cmd.Parameters.AddWithValue("@Year", options.ReleaseYear.Value);

        if (options.ReleaseYearFrom.HasValue)
            cmd.Parameters.AddWithValue("@YearFrom", options.ReleaseYearFrom.Value);

        if (options.ReleaseYearTo.HasValue)
            cmd.Parameters.AddWithValue("@YearTo", options.ReleaseYearTo.Value);

        if (options.AgeRestriction.HasValue)
            cmd.Parameters.AddWithValue("@AgeRestriction", options.AgeRestriction.Value);

        if (options.MinAverageRating.HasValue)
            cmd.Parameters.AddWithValue("@MinAvgRating", options.MinAverageRating.Value);

        if (options.Limit.HasValue)
            cmd.Parameters.AddWithValue("@Limit", options.Limit.Value);
        if (options.Offset.HasValue)
            cmd.Parameters.AddWithValue("@Offset", options.Offset.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var media = new Media
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

            list.Add(new MediaWithScore
            {
                Media = media,
                AvgScore = reader.GetDouble(8)
            });
        }

        return list;
    }

    public List<MediaWithScore> GetAllMediaWithAvgScore()
        => GetMediaFiltered(new MediaQueryOptions());

    public bool UpdateMedia(Media media)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = @"UPDATE Media 
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
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteMedia(int id, int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "DELETE FROM Media WHERE Id=@Id AND CreatedByUserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0;
    }

    // --- FAVORITES ---

    // NOTE: This uses your existing table name "UserLikedMedia".
    // For clarity you may want to rename it to "UserFavorites".

    public void AddFavorite(int userId, int mediaId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "INSERT INTO UserLikedMedia (UserId, MediaId) VALUES (@UserId, @MediaId) ON CONFLICT DO NOTHING;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        cmd.ExecuteNonQuery();
    }

    public void RemoveFavorite(int userId, int mediaId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "DELETE FROM UserLikedMedia WHERE UserId = @UserId AND MediaId = @MediaId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        cmd.ExecuteNonQuery();
    }

    public List<Media> GetFavoritesByUserId(int userId)
    {
        var favorites = new List<Media>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
SELECT
    m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId,
    COALESCE(
        array_agg(DISTINCT u.Username) FILTER (WHERE u.Username IS NOT NULL),
        ARRAY[]::text[]
    ) AS LikedByUsers
FROM UserLikedMedia f
JOIN Media m ON m.Id = f.MediaId

LEFT JOIN UserLikedMedia ulm ON ulm.MediaId = m.Id
LEFT JOIN Users u ON u.Id = ulm.UserId

WHERE f.UserId = @UserId
GROUP BY m.Id, m.Title, m.Description, m.Type, m.ReleaseYear, m.Genre, m.AgeRestriction, m.CreatedByUserId
ORDER BY m.Id;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var liked = reader.GetFieldValue<string[]>(8); // array_agg -> text[] -> string[]

            favorites.Add(new Media
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                Type = (MediaType)reader.GetInt32(3),
                ReleaseYear = reader.GetInt32(4),
                Genre = reader.GetString(5),
                AgeRestriction = reader.GetInt32(6),
                CreatedByUserId = reader.GetInt32(7),
                LikedByUsers = liked.ToList()
            });
        }

        return favorites;
    }



    // --- RATINGS ---

    public Rating UpsertRating(int mediaId, int userId, int stars, string? comment)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
INSERT INTO Ratings (MediaId, UserId, Stars, Comment, Confirmed, Timestamp)
VALUES (@MediaId, @UserId, @Stars, @Comment, FALSE, NOW())
ON CONFLICT (MediaId, UserId)
DO UPDATE SET Stars = EXCLUDED.Stars,
              Comment = EXCLUDED.Comment,
              Confirmed = FALSE,
              Timestamp = NOW()
RETURNING Id, MediaId, UserId, Stars, COALESCE(Comment,''), Confirmed, Timestamp;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@Stars", stars);
        cmd.Parameters.AddWithValue("@Comment", (object?)comment ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        reader.Read();

        return new Rating
        {
            Id = reader.GetInt32(0),
            MediaId = reader.GetInt32(1),
            UserId = reader.GetInt32(2),
            Stars = reader.GetInt32(3),
            Comment = reader.GetString(4),
            Confirmed = reader.GetBoolean(5),
            Timestamp = reader.GetDateTime(6)
        };
    }

    public bool DeleteRating(int ratingId, int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "DELETE FROM Ratings WHERE Id=@Id AND UserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", ratingId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool ConfirmRatingComment(int ratingId, int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "UPDATE Ratings SET Confirmed=TRUE WHERE Id=@Id AND UserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", ratingId);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public Rating? GetRatingById(int ratingId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
SELECT Id, MediaId, UserId, Stars, COALESCE(Comment,''), Confirmed, Timestamp
FROM Ratings
WHERE Id=@Id;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", ratingId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new Rating
        {
            Id = reader.GetInt32(0),
            MediaId = reader.GetInt32(1),
            UserId = reader.GetInt32(2),
            Stars = reader.GetInt32(3),
            Comment = reader.GetString(4),
            Confirmed = reader.GetBoolean(5),
            Timestamp = reader.GetDateTime(6),
        };
    }

    public bool UpdateUserPasswordHash(int userId, string passwordHash)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "UPDATE Users SET PasswordHash=@Hash WHERE Id=@Id;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Hash", passwordHash);
        cmd.Parameters.AddWithValue("@Id", userId);
        return cmd.ExecuteNonQuery() > 0;
    }


    public List<Rating> GetRatingsByMediaId(int mediaId, int requestingUserId)
    {
        var ratings = new List<Rating>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        const string sql = @"
SELECT
    r.Id,
    r.MediaId,
    r.UserId,
    r.Stars,
    COALESCE(r.Comment,'') AS Comment,
    r.Confirmed,
    r.Timestamp,
    COALESCE(COUNT(ulr.UserId), 0)::int AS Likes
FROM Ratings r
LEFT JOIN UserLikedRatings ulr ON ulr.RatingId = r.Id
WHERE
    r.MediaId = @MediaId
    AND (r.Confirmed = TRUE OR r.UserId = @RequestingUserId)
GROUP BY
    r.Id, r.MediaId, r.UserId, r.Stars, r.Comment, r.Confirmed, r.Timestamp
ORDER BY r.Timestamp DESC;";



        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        cmd.Parameters.AddWithValue("@RequestingUserId", requestingUserId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            
            ratings.Add(new Rating
            {
                Id = reader.GetInt32(0),
                MediaId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Stars = reader.GetInt32(3),
                Comment = reader.GetString(4),
                Confirmed = reader.GetBoolean(5),
                Timestamp = reader.GetDateTime(6),
                Likes = reader.GetInt32(7),
            });
        }

        return ratings;
    }

    public List<Rating> GetRatingsByUserId(int userId)
    {
        var ratings = new List<Rating>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
SELECT Id, MediaId, UserId, Stars, COALESCE(Comment,''), Confirmed, Timestamp
FROM Ratings
WHERE UserId=@UserId
ORDER BY Timestamp DESC;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ratings.Add(new Rating
            {
                Id = reader.GetInt32(0),
                MediaId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Stars = reader.GetInt32(3),
                Comment = reader.GetString(4),
                Confirmed = reader.GetBoolean(5),
                Timestamp = reader.GetDateTime(6),
            });
        }

        return ratings;
    }

    public double GetAverageScoreForMedia(int mediaId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "SELECT COALESCE(AVG(Stars), 0) FROM Ratings WHERE MediaId=@MediaId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);
        return Convert.ToDouble(cmd.ExecuteScalar());
    }

    // --- LIKES (Ratings) ---

    public bool LikeRating(int userId, int ratingId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "INSERT INTO UserLikedRatings (UserId, RatingId) VALUES (@UserId, @RatingId) ON CONFLICT DO NOTHING RETURNING 1;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@RatingId", ratingId);
        var res = cmd.ExecuteScalar();
        return res != null;
    }


    // --- STATS / LEADERBOARD ---

    public int GetTotalRatingsByUserId(int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "SELECT COUNT(*) FROM Ratings WHERE UserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public double GetAverageScoreByUserId(int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        const string sql = "SELECT COALESCE(AVG(Stars), 0) FROM Ratings WHERE UserId=@UserId;";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);
        return Convert.ToDouble(cmd.ExecuteScalar());
    }

    public string GetFavoriteGenreByUserId(int userId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        const string sql = @"
SELECT COALESCE(m.Genre, '') AS Genre, COUNT(*) AS Cnt
FROM Ratings r
JOIN Media m ON m.Id = r.MediaId
WHERE r.UserId=@UserId
GROUP BY m.Genre
ORDER BY Cnt DESC
LIMIT 1;";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return "n/a";
        return reader.GetString(0);
    }
    
    public List<UserPublicDto> GetUsersWhoLikedMedia(int mediaId)
    {
        var list = new List<UserPublicDto>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
        SELECT u.id, u.username
        FROM userlikedmedia ulm
        JOIN users u ON u.id = ulm.userid
        WHERE ulm.mediaid = @MediaId
        ORDER BY u.username;
    ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MediaId", mediaId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new UserPublicDto { Id = r.GetInt32(0), Username = r.GetString(1) });

        return list;
    }


    public List<LeaderboardEntry> GetLeaderboard()
    {
        var result = new List<LeaderboardEntry>();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        const string sql = @"
SELECT
  u.Username,
  COALESCE(COUNT(r.Id), 0)::int AS TotalRatings,
  COALESCE(AVG(r.Stars), 0)::double precision AS AvgScore,
  COALESCE((
    SELECT m.Genre
    FROM Ratings r2
    JOIN Media m ON m.Id = r2.MediaId
    WHERE r2.UserId = u.Id AND r2.Confirmed = TRUE
    GROUP BY m.Genre
    ORDER BY COUNT(*) DESC, m.Genre ASC
    LIMIT 1
  ), '') AS FavoriteGenre
FROM Users u
LEFT JOIN Ratings r
  ON r.UserId = u.Id AND r.Confirmed = TRUE
GROUP BY u.Id, u.Username
ORDER BY AvgScore DESC, TotalRatings DESC, u.Username ASC;
";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new LeaderboardEntry
            {
                Username = reader.GetString(0),
                TotalRatings = reader.GetInt32(1),
                AvgScore = reader.GetDouble(2),
                FavoriteGenre = reader.GetString(3)
            });
        }

        return result;
    }

}
