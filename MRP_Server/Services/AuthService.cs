using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class AuthService
{
    private static readonly Dictionary<string, User> _tokens = new();
    private static DatabaseService dbService = new DatabaseService();
    private static bool UserExists(string username)
    {
        var user = dbService.GetUserByUsername(username);
        if (user == null)
            return false;
        return true;
    }
    
    public static bool Register(User user)
    {
        if (UserExists(user.Username))
            return false;
        
           
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        dbService.AddUser(user);
        return true;
    }

    public static User? Login(string username, string password)
    {
        if (!UserExists(username))
            return null;
        
        var user = dbService.GetUserByUsername(username);
        
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.Token = $"{username}-mrpToken-{Guid.NewGuid()}";
        dbService.UpdateUserToken(username, user.Token);
        return user;
    }

    public static User? ValidateToken(string token)
    {
        return _tokens.TryGetValue(token, out var user) ? user : null;
    }
}