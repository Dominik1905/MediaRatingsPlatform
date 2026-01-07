using System.Net;
using System.Text;

namespace MediaRatingsPlatform;

public static class UiController
{
    public static async Task Handle(HttpListenerContext context, string method, string rawPath)
    {
        if (method != "GET" && method != "HEAD")
        {
            context.Response.StatusCode = 405;
            context.Response.Close();
            return;
        }
        
        var wwwroot = FindWwwRoot();
        if (wwwroot == null)
        {
            await WriteText(context, 500, "wwwroot not found. Copy Blazor publish output to MRP_Server/wwwroot.");
            return;
        }
        
        var path = rawPath;
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            path = "/index.html";
        
        path = Uri.UnescapeDataString(path);

        // Security: Directory traversal blocken
        if (path.Contains(".."))
        {
            await WriteText(context, 400, "Bad Request");
            return;
        }
        
        var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(wwwroot, relative);
        
        if (Directory.Exists(fullPath))
            fullPath = Path.Combine(fullPath, "index.html");
        
        if (!File.Exists(fullPath))
        {
            var fileName = Path.GetFileName(fullPath);
            var hasExtension = fileName.Contains('.');
            if (!hasExtension)
            {
                var indexPath = Path.Combine(wwwroot, "index.html");
                if (File.Exists(indexPath))
                    fullPath = indexPath;
                else
                {
                    await WriteText(context, 404, "Not Found");
                    return;
                }
            }
            else
            {
                await WriteText(context, 404, "Not Found");
                return;
            }
        }
        
        context.Response.ContentType = GetContentType(fullPath);
        context.Response.AddHeader("Cache-Control", "no-cache");

        var bytes = await File.ReadAllBytesAsync(fullPath);
        context.Response.ContentLength64 = bytes.Length;

        if (method == "GET")
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);

        context.Response.Close();
    }

    private static string? FindWwwRoot()
    {
        var c1 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (Directory.Exists(c1)) return c1;
        
        var c2 = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(c2)) return c2;
        
        var c3 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot"));
        if (Directory.Exists(c3)) return c3;

        return null;
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css"  => "text/css; charset=utf-8",
            ".js"   => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".wasm" => "application/wasm",
            ".dll"  => "application/octet-stream",
            ".dat"  => "application/octet-stream",
            ".ico"  => "image/x-icon",
            ".png"  => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg"  => "image/svg+xml",
            ".txt"  => "text/plain; charset=utf-8",
            _       => "application/octet-stream"
        };
    }

    private static async Task WriteText(HttpListenerContext context, int status, string text)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "text/plain; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(text);
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        context.Response.Close();
    }
}
