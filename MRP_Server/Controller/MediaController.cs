using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class MediaController
    {
        private static readonly List<Media> _media = new();

        public static async Task Handle(HttpListenerContext context, string method, string path)
        {
            if (method == "POST" && path == "/api/media")
            {
                // Auth prüfen
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
                    return;
                }

                newMedia.Id = _media.Count + 1;
                newMedia.CreatedByUserId = user.Id;
                _media.Add(newMedia);

                context.Response.StatusCode = 201;
                var json = JsonSerializer.Serialize(newMedia);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            else if (method == "GET" && path.StartsWith("/api/media/"))
            {
                var idStr = path.Replace("/api/media/", "");
                if (int.TryParse(idStr, out int id))
                {
                    var media = _media.FirstOrDefault(m => m.Id == id);
                    if (media == null)
                    {
                        context.Response.StatusCode = 404;
                    }
                    else
                    {
                        var json = JsonSerializer.Serialize(media);
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        context.Response.ContentType = "application/json";
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
                context.Response.Close();
            }
            // PUT + DELETE später
        }
    }