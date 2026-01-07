using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class MediaController
{
    private static readonly DatabaseService dbService = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static async Task Handle(HttpListenerContext context, string method, string path)
    {
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        // GET /api/media
        if (method == "GET" && path == "/api/media")
        {
            // filter
            var options = MediaQueryOptionsParser.Parse(context.Request.QueryString);
            var list = dbService.GetMediaFiltered(options);
            await WriteJson(context, 200, JsonSerializer.Serialize(list));
            return;
        }

        // POST /api/media
        if (method == "POST" && path == "/api/media")
        {
            await HandlePost(context, user);
            return;
        }

        // GET /api/media/search?title=foo
        if (method == "GET" && path.StartsWith("/api/media/search"))
        {
            var options = MediaQueryOptionsParser.Parse(context.Request.QueryString);
            if (string.IsNullOrWhiteSpace(options.TitlePart))
                options.TitlePart = context.Request.QueryString["title"];
            var result = dbService.GetMediaFiltered(options);
            await WriteJson(context, 200, JsonSerializer.Serialize(result));
            return;
        }

        // Nested routes: /api/media/{id}/ratings
        if (path.StartsWith("/api/media/") && path.EndsWith("/ratings"))
        {
            if (!TryParseMediaIdFromPath(path, out int mediaId))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            if (method == "GET")
            {
                var ratings = dbService.GetRatingsByMediaId(mediaId, user.Id);
                await WriteJson(context, 200, JsonSerializer.Serialize(ratings));
                return;
            }

            if (method == "POST")
            {
                await HandleCreateOrUpdateRating(context, user, mediaId);
                return;
            }
        }

        // Favorite toggle: POST/DELETE /api/media/{id}/favorite
        if (path.StartsWith("/api/media/") && path.EndsWith("/favorite"))
        {
            if (!TryParseMediaIdFromPath(path, out int mediaId))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            if (method == "POST")
            {
                dbService.AddFavorite(user.Id, mediaId);
                context.Response.StatusCode = 204;
                context.Response.Close();
                return;
            }

            if (method == "DELETE")
            {
                dbService.RemoveFavorite(user.Id, mediaId);
                context.Response.StatusCode = 204;
                context.Response.Close();
                return;
            }
        }

        // GET/PUT/DELETE /api/media/{id}
        if (path.StartsWith("/api/media/") && !path.Contains("/"))
        {
        }

        if (path.StartsWith("/api/media/") && method == "GET")
        {
            await HandleGet(context, user, path);
            return;
        }

        if (path.StartsWith("/api/media/") && method == "PUT")
        {
            await HandlePut(context, user, path);
            return;
        }

        if (path.StartsWith("/api/media/") && method == "DELETE")
        {
            await HandleDelete(context, user, path);
            return;
        }

        context.Response.StatusCode = 404;
        context.Response.Close();
    }

    private static async Task HandlePost(HttpListenerContext context, User user)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var newMedia = JsonSerializer.Deserialize<Media>(body, JsonOpts);

        if (newMedia == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        newMedia.CreatedByUserId = user.Id;
        dbService.InsertMedia(newMedia);

        await WriteJson(context, 201, JsonSerializer.Serialize(newMedia));
    }

    private static async Task HandleGet(HttpListenerContext context, User user, string path)
    {
        if (!int.TryParse(path.Replace("/api/media/", ""), out int id))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var media = dbService.GetMediaById(id);
        if (media == null)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        var response = new
        {
            Media = media,
            AvgScore = dbService.GetAverageScoreForMedia(id),
            Ratings = dbService.GetRatingsByMediaId(id, user.Id),
        };

        await WriteJson(context, 200, JsonSerializer.Serialize(response));
    }

    private static async Task HandlePut(HttpListenerContext context, User user, string path)
    {
        if (!int.TryParse(path.Replace("/api/media/", ""), out int id))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var existing = dbService.GetMediaById(id);
        if (existing == null)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        // only creator can modify the media entry.
        if (existing.CreatedByUserId != user.Id)
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var updatedMedia = JsonSerializer.Deserialize<Media>(body, JsonOpts);
        if (updatedMedia == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        updatedMedia.Id = id;
        updatedMedia.CreatedByUserId = user.Id;

        var ok = dbService.UpdateMedia(updatedMedia);
        if (!ok)
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 204;
        context.Response.Close();
    }


    private static async Task HandleDelete(HttpListenerContext context, User user, string path)
    {
        if (!int.TryParse(path.Replace("/api/media/", ""), out int id))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var existing = dbService.GetMediaById(id);
        if (existing == null)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        // only creator can delete the media entry.
        if (existing.CreatedByUserId != user.Id)
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        var ok = dbService.DeleteMedia(id, user.Id);
        if (!ok)
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 204;
        context.Response.Close();
    }


    private sealed class RatingCreateDto
    {
        public int Stars { get; set; }
        public string? Comment { get; set; }
    }

    private static async Task HandleCreateOrUpdateRating(HttpListenerContext context, User user, int mediaId)
    {
        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var dto = JsonSerializer.Deserialize<RatingCreateDto>(body, JsonOpts);

        if (dto == null || dto.Stars < 1 || dto.Stars > 5)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var rating = dbService.UpsertRating(mediaId, user.Id, dto.Stars, dto.Comment);
        await WriteJson(context, 201, JsonSerializer.Serialize(rating));
    }

    private static bool TryParseMediaIdFromPath(string path, out int mediaId)
    {
        // accepts /api/media/{id}/ratings or /favorite
        mediaId = 0;
        var parts = path.Trim('/').Split('/');
        if (parts.Length < 3) return false;
        if (!parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)) return false;
        if (!parts[1].Equals("media", StringComparison.OrdinalIgnoreCase)) return false;
        return int.TryParse(parts[2], out mediaId);
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
