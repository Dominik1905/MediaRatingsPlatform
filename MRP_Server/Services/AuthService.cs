﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DatabaseObjects;
using DatabaseObjects.Service;
using Microsoft.IdentityModel.Tokens;

namespace MediaRatingsPlatform;

public static class AuthService
{
    private static readonly string SecretKey = "supersecret-key-change-me-1234567890";
    private static readonly SymmetricSecurityKey SigningKey =
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

    private static readonly DatabaseService dbService = new DatabaseService();

    private static bool UserExists(string username)
    {
        var user = dbService.GetUserByUsername(username);
        return user != null;
    }

    public static bool Register(User user)
    {
        if (UserExists(user.Username))
            return false;

        // Passwort hashen
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        dbService.AddUser(user);
        return true;
    }

    public static string? Login(string username, string password)
    {
        if (!UserExists(username))
            return null;

        var user = dbService.GetUserByUsername(username);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        // JWT erzeugen
        return GenerateJwtToken(user);
    }

    private static string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("userId", user.Id.ToString())
        };

        var creds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "mrp-server",
            audience: "mrp-client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return tokenHandler.WriteToken(token);
    }

    public static ClaimsPrincipal? ValidateJwtToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "mrp-server",
                    ValidateAudience = true,
                    ValidAudience = "mrp-client",
                    ValidateLifetime = true,
                    IssuerSigningKey = SigningKey,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero 
                }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
