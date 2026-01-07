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
        else if (path.StartsWith("/api/ratings"))
        {
            await RatingController.Handle(context, method, path);
        }
        else
        {
            await UiController.Handle(context, method, path);
        }
    }
}
