using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class MediaController
{
    private static readonly DatabaseObjects.Service.DatabaseService dbService = new();

    public static async Task Handle(HttpListenerContext context, string method, string path)
    {
        if (method == "POST" && path == "/api/media")
        {
            await HandlePost(context);
        }
        else if (method == "GET" && path.StartsWith("/api/media/search"))
        {
            var query = context.Request.QueryString["title"] ?? "";
            var result = dbService.SearchMediaByTitle(query);
            var json = JsonSerializer.Serialize(result);
            await WriteJson(context, 200, json);
        }
        else if (method == "GET" && path.StartsWith("/api/media/"))
        {
            await HandleGet(context, path);
        }
        else if (method == "PUT" && path.StartsWith("/api/media/"))
        {
            await HandlePut(context, path);
        }
        else if (method == "DELETE" && path.StartsWith("/api/media/"))
        {
            await HandleDelete(context, path);
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }

    private static async Task HandlePost(HttpListenerContext context)
    {
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        var body = await reader.ReadToEndAsync();
        var newMedia = JsonSerializer.Deserialize<Media>(body);

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

    private static async Task HandleGet(HttpListenerContext context, string path)
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

        await WriteJson(context, 200, JsonSerializer.Serialize(media));
    }

    private static async Task HandlePut(HttpListenerContext context, string path)
    {
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        if (!int.TryParse(path.Replace("/api/media/", ""), out int id))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        var body = await reader.ReadToEndAsync();
        var updatedMedia = JsonSerializer.Deserialize<Media>(body);
        if (updatedMedia == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        updatedMedia.Id = id;
        updatedMedia.CreatedByUserId = user.Id;
        dbService.UpdateMedia(updatedMedia);

        context.Response.StatusCode = 204;
        context.Response.Close();
    }

    private static async Task HandleDelete(HttpListenerContext context, string path)
    {
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        if (!int.TryParse(path.Replace("/api/media/", ""), out int id))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        dbService.DeleteMedia(id, user.Id);
        context.Response.StatusCode = 204;
        context.Response.Close();
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
