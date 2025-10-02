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
        var principal = AuthService.ValidateJwtToken(token);
        if (principal == null)
            return null;

        var username = principal.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return null;

        // User aus DB holen
        return new DatabaseObjects.Service.DatabaseService().GetUserByUsername(username);
    }
}