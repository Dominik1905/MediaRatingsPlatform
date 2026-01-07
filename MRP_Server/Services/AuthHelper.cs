using System.Net;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class AuthHelper
{
    public static User? GetUserFromRequest(HttpListenerContext context)
    {

        var authHeader = context.Request.Headers["Authorization"]
                         ?? context.Request.Headers["Authentication"];

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        string token = authHeader.Substring("Bearer ".Length).Trim();
        var principal = AuthService.ValidateJwtToken(token);
        if (principal == null)
            return null;

        var username = principal.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return null;

        return new DatabaseService().GetUserByUsername(username);
    }
}
