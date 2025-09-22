using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class AuthService
{
    private static readonly Dictionary<string, User> _tokens = new();
    private static readonly List<User> _users = new(); // später aus PostgreSQL laden

    public static bool Register(User user)
    {
        if (_users.Exists(u => u.Username == user.Username))
            return false;

        user.Id = _users.Count + 1;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _users.Add(user);
        return true;
    }

    public static User? Login(string username, string password)
    {
        var user = _users.Find(u => u.Username == username);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.Token = $"{username}-mrpToken-{Guid.NewGuid()}";
        _tokens[user.Token] = user;
        return user;
    }

    public static User? ValidateToken(string token)
    {
        return _tokens.TryGetValue(token, out var user) ? user : null;
    }
}