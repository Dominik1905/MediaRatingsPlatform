using System.Net;
using System.Text;
using System.Text.Json;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class RatingController
{
    private static readonly DatabaseService dbService = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static async Task Handle(HttpListenerContext context, string method, string path)
    {
        //every request except register/login must be authenticated.
        var user = AuthHelper.GetUserFromRequest(context);
        if (user == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        // POST /api/ratings/{id}/like
        if (method == "POST" && path.EndsWith("/like"))
        {
            if (!TryParseRatingId(path, out int ratingId))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var rating = dbService.GetRatingById(ratingId);
            if (rating == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            // cannot like own rating
            if (rating.UserId == user.Id)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var inserted = dbService.LikeRating(user.Id, ratingId);
            context.Response.StatusCode = inserted ? 204 : 409; // already liked
            context.Response.Close();
            return;
        }

        // POST /api/ratings/{id}/confirm
        if (method == "POST" && path.EndsWith("/confirm"))
        {
            if (!TryParseRatingId(path, out int ratingId))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var rating = dbService.GetRatingById(ratingId);
            if (rating == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            if (rating.UserId != user.Id)
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            var ok = dbService.ConfirmRatingComment(ratingId, user.Id);
            context.Response.StatusCode = ok ? 204 : 500;
            context.Response.Close();
            return;
        }

        // DELETE /api/ratings/{id}
        if (method == "DELETE")
        {
            if (!TryParseRatingId(path, out int ratingId))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var rating = dbService.GetRatingById(ratingId);
            if (rating == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            if (rating.UserId != user.Id)
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }

            var ok = dbService.DeleteRating(ratingId, user.Id);
            context.Response.StatusCode = ok ? 204 : 500;
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 404;
        context.Response.Close();
    }

    private static bool TryParseRatingId(string path, out int ratingId)
    {
        // supports: /api/ratings/{id} , /api/ratings/{id}/like , /api/ratings/{id}/confirm
        ratingId = 0;
        var parts = path.Trim('/').Split('/');
        if (parts.Length < 3) return false;
        if (!parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)) return false;
        if (!parts[1].Equals("ratings", StringComparison.OrdinalIgnoreCase)) return false;
        return int.TryParse(parts[2], out ratingId);
    }
}
