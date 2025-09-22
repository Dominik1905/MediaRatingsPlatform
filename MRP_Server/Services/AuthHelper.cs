using System.Net;
using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class AuthHelper
{
    public static User? GetUserFromRequest(HttpListenerContext context)
    {
        var authHeader = context.Request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return null;

        string token = authHeader.Substring("Bearer ".Length).Trim();
        return AuthService.ValidateToken(token);
    }
}