using System.Net;
using System.Text;

namespace MediaRatingsPlatform;

public static class Router
{
    public static async Task Handle(HttpListenerContext context)
    {
        string path = context.Request.Url!.AbsolutePath.ToLower();
        string method = context.Request.HttpMethod;

        if (path.StartsWith("/api/users"))
        {
            await UserController.Handle(context, method, path);
        }
        else if (path.StartsWith("/api/media"))
        {
            await MediaController.Handle(context, method, path);
        }
        else
        {
            context.Response.StatusCode = 404;
            byte[] buffer = Encoding.UTF8.GetBytes("Not Found");
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }
    }
}