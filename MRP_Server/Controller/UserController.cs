using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class UserController
{
    public static async Task Handle(HttpListenerContext context, string method, string path)
    {
        if (method == "POST" && path.EndsWith("/login"))
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            var loginData = JsonSerializer.Deserialize<User>(body);

            if (loginData == null)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var user = AuthService.Login(loginData.Username, loginData.PasswordHash);
            if (user == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var responseJson = JsonSerializer.Serialize(new { Token = user.Token , Password = user.PasswordHash});
            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }
        else if (method == "POST" && path.EndsWith("/register"))
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            var newUser = JsonSerializer.Deserialize<User>(body);

            if (newUser == null)
            {
                context.Response.StatusCode = 400;
                return;
            }

            bool success = AuthService.Register(newUser);
            context.Response.StatusCode = success ? 201 : 409; // 409 Conflict wenn Username schon existiert
            context.Response.Close();
        }
        else if (method == "GET" && path.Contains("/profile"))
        {
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();
            var user = AuthService.ValidateToken(token);
            if (user == null)
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            // Profil zurückgeben (erstmal Dummy-Daten)
            var profile = new
            {
                Username = user.Username,
                TotalRatings = 0,
                AvgScore = 0,
                FavoriteGenre = "n/a"
            };

            var json = JsonSerializer.Serialize(profile);
            var buffer = Encoding.UTF8.GetBytes(json);
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }
    }
}