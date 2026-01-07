using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class UserController
{
    private static readonly DatabaseService dbService = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private sealed class CredentialsDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public static async Task Handle(HttpListenerContext context, string method, string path)
    {
        // POST /api/users/login
        if (method == "POST" && path.EndsWith("/login"))
        {
            await HandleLogin(context);
            return;
        }

        // POST /api/users/register
        if (method == "POST" && path.EndsWith("/register"))
        {
            await HandleRegister(context);
            return;
        }

        // Everything below requires auth
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        // PUT /api/users/{username}/profile
        if (method == "PUT" && path.EndsWith("/profile"))
        {
            await HandleProfileUpdate(context, user, path);
            return;
        }

        // GET /api/users/{username}/profile
        if (method == "GET" && path.EndsWith("/profile"))
        {
            await HandleProfile(context, user, path);
            return;
        }

        // GET /api/users/{username}/favorites
        if (method == "GET" && path.EndsWith("/favorites"))
        {
            await HandleFavorites(context, user, path);
            return;
        }

        // GET /api/users/{username}/ratings
        if (method == "GET" && path.EndsWith("/ratings"))
        {
            await HandleRatingHistory(context, user, path);
            return;
        }

        // GET /api/users/{username}/recommendations
        if (method == "GET" && path.EndsWith("/recommendations"))
        {
            await HandleRecommendations(context, user, path);
            return;
        }

        // GET /api/users/leaderboard
        if (method == "GET" && path.EndsWith("/leaderboard"))
        {
            await HandleLeaderboard(context);
            return;
        }

        context.Response.StatusCode = 404;
        context.Response.Close();
    }

    private static async Task HandleLogin(HttpListenerContext context)
    {
        try
        {
            var body = await ReadBody(context);
            var creds = JsonSerializer.Deserialize<CredentialsDto>(body, JsonOpts);

            if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var jwt = AuthService.Login(creds.Username, creds.Password);
            if (jwt == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            await WriteJson(context, 200, JsonSerializer.Serialize(new { Token = jwt }));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Login Fehler: " + ex);
            context.Response.StatusCode = 500;
            context.Response.Close();
        }
    }

    private static async Task HandleRegister(HttpListenerContext context)
    {
        var body = await ReadBody(context);
        var creds = JsonSerializer.Deserialize<CredentialsDto>(body, JsonOpts);

        if (creds == null || string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var newUser = new User
        {
            Username = creds.Username,
            PasswordHash = creds.Password 
        };

        bool success = AuthService.Register(newUser);
        context.Response.StatusCode = success ? 201 : 409;
        context.Response.Close();
    }

    private static async Task HandleProfile(HttpListenerContext context, User authUser, string path)
    {
        var usernameFromPath = ExtractUsernameFromUsersPath(path);
        if (usernameFromPath == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }
        
        if (!usernameFromPath.Equals(authUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        var profile = new
        {
            Username = authUser.Username,
            TotalRatings = dbService.GetTotalRatingsByUserId(authUser.Id),
            AvgScore = dbService.GetAverageScoreByUserId(authUser.Id),
            FavoriteGenre = dbService.GetFavoriteGenreByUserId(authUser.Id),
        };

        await WriteJson(context, 200, JsonSerializer.Serialize(profile));
    }

    private sealed class ProfileUpdateDto
    {
        public string? Password { get; set; }
    }

    private static async Task HandleProfileUpdate(HttpListenerContext context, User authUser, string path)
    {
        var usernameFromPath = ExtractUsernameFromUsersPath(path);
        if (usernameFromPath == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (!usernameFromPath.Equals(authUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        var body = await ReadBody(context);
        var dto = JsonSerializer.Deserialize<ProfileUpdateDto>(body, JsonOpts);
        if (dto == null || string.IsNullOrWhiteSpace(dto.Password))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }
        
        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var ok = dbService.UpdateUserPasswordHash(authUser.Id, newHash);
        context.Response.StatusCode = ok ? 204 : 500;
        context.Response.Close();
    }


    private static async Task HandleFavorites(HttpListenerContext context, User authUser, string path)
    {
        var usernameFromPath = ExtractUsernameFromUsersPath(path);
        if (usernameFromPath == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (!usernameFromPath.Equals(authUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        var favorites = dbService.GetFavoritesByUserId(authUser.Id);
        await WriteJson(context, 200, JsonSerializer.Serialize(favorites));
    }

    private static async Task HandleRatingHistory(HttpListenerContext context, User authUser, string path)
    {
        var usernameFromPath = ExtractUsernameFromUsersPath(path);
        if (usernameFromPath == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (!usernameFromPath.Equals(authUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        var ratings = dbService.GetRatingsByUserId(authUser.Id);
        await WriteJson(context, 200, JsonSerializer.Serialize(ratings));
    }

    private static async Task HandleLeaderboard(HttpListenerContext context)
    {
        var list = dbService.GetLeaderboard()
            .Select(x => new
            {
                x.Username,
                TotalRatings = x.TotalRatings,
                AvgScore = x.AvgScore,
                FavoriteGenre = x.FavoriteGenre
            })
            .ToList();

        await WriteJson(context, 200, JsonSerializer.Serialize(list));
    }


    private static async Task HandleRecommendations(HttpListenerContext context, User authUser, string path)
    {
        var usernameFromPath = ExtractUsernameFromUsersPath(path);
        if (usernameFromPath == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (!usernameFromPath.Equals(authUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }
        
        int limit = 10;
        if (int.TryParse(context.Request.QueryString["limit"], out var l) && l > 0 && l <= 50)
            limit = l;

        var allMedia = dbService.GetAllMediaWithAvgScore();
        var ratings = dbService.GetRatingsByUserId(authUser.Id);
        var favIds = dbService.GetFavoritesByUserId(authUser.Id).Select(m => m.Id).ToHashSet();

        var recommendations = RecommendationEngine.Recommend(allMedia, ratings, favIds, limit);
        await WriteJson(context, 200, JsonSerializer.Serialize(recommendations));
    }

    private static string? ExtractUsernameFromUsersPath(string path)
    {
        // expected: /api/users/{username}/profile (or /favorites, /ratings)
        var parts = path.Trim('/').Split('/');
        if (parts.Length < 3) return null;
        if (!parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)) return null;
        if (!parts[1].Equals("users", StringComparison.OrdinalIgnoreCase)) return null;
        return parts[2];
    }

    private static async Task<string> ReadBody(HttpListenerContext context)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    private static async Task WriteJson(HttpListenerContext context, int statusCode, string json)
    {
        var buffer = Encoding.UTF8.GetBytes(json);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        context.Response.Close();
    }
}
