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
            try
            {
                using var reader = new StreamReader(context.Request.InputStream);
                var body = await reader.ReadToEndAsync();
                var loginData = JsonSerializer.Deserialize<User>(body);

                if (loginData == null)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    return;
                }

                var jwt = AuthService.Login(loginData.Username, loginData.PasswordHash);
                if (jwt == null)
                {
                    context.Response.StatusCode = 401;
                    context.Response.Close();
                    return;
                }

                var responseJson = JsonSerializer.Serialize(new { Token = jwt });
                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length; // <<< hier fix
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login Fehler: " + ex);
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
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
            context.Response.StatusCode = success ? 201 : 409; // 409 = Username schon existiert
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
            var principal = AuthService.ValidateJwtToken(token);
            if (principal == null)
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            // Claims auslesen
            var username = principal.Identity?.Name ?? "unknown";

            var profile = new
            {
                Username = username,
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
