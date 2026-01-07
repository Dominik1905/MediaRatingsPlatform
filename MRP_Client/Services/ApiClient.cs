using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MRP_Client.Models;
using MRP_Client.Pages;

namespace MRP_Client.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http, AuthState auth) { _http = http; _auth = auth; }

    private void ApplyAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrWhiteSpace(_auth.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    private static StringContent JsonBody(object obj)
        => new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    public async Task<(bool ok, string message)> RegisterAsync(string username, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users/register")
        { Content = JsonBody(new { Username = username, Password = password }) };

        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string? token, string message)> LoginAsync(string username, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/users/login")
        { Content = JsonBody(new { Username = username, Password = password }) };

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return (false, null, txt);

        try { return (true, JsonSerializer.Deserialize<LoginResponse>(txt, JsonOpts)?.Token, txt); }
        catch { return (true, null, txt); }
    }

    public async Task<List<MediaDetailsResponse>> GetMediaAsync(Dictionary<string, string?> query)
    {
        var qs = string.Join("&", query.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!.Trim())}"));

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/media" + (string.IsNullOrWhiteSpace(qs) ? "" : "?" + qs)
        );

        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();

        try { return JsonSerializer.Deserialize<List<MediaDetailsResponse>>(txt, JsonOpts) ?? new(); }
        catch { return new(); }
    }


    public async Task<(bool ok, string raw, MediaDetailsResponse? details)> GetMediaDetailsAsync(int id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/media/{id}");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        var raw = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return (false, raw, null);

        try { return (true, raw, JsonSerializer.Deserialize<MediaDetailsResponse>(raw, JsonOpts)); }
        catch { return (true, raw, null); }
    }

    public async Task<(bool ok, int? id, string message)> CreateMediaAsync(MediaDto m)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/media")
        { Content = JsonBody(new { Title=m.Title, Description=m.Description, Type=m.Type, ReleaseYear=m.ReleaseYear, Genre=m.Genre, AgeRestriction=m.AgeRestriction }) };
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return (false, null, txt);

        try { return (true, JsonSerializer.Deserialize<MediaDto>(txt, JsonOpts)?.Id, txt); }
        catch { return (true, null, txt); }
    }

    public async Task<(bool ok, string message)> UpdateMediaAsync(int id, MediaDto m)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/media/{id}")
        { Content = JsonBody(new { Title=m.Title, Description=m.Description, Type=m.Type, ReleaseYear=m.ReleaseYear, Genre=m.Genre, AgeRestriction=m.AgeRestriction }) };
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> DeleteMediaAsync(int id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/media/{id}");
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<List<RatingDto>> GetRatingsForMediaAsync(int mediaId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/media/{mediaId}/ratings");
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();

        try { return JsonSerializer.Deserialize<List<RatingDto>>(txt, JsonOpts) ?? new(); }
        catch { return new(); }
    }

    public async Task<(bool ok, string message)> RateAsync(int mediaId, int stars, string? comment)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/media/{mediaId}/ratings")
        { Content = JsonBody(new { Stars = stars, Comment = comment }) };
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> LikeRatingAsync(int ratingId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/ratings/{ratingId}/like");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> ConfirmRatingAsync(int ratingId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/ratings/{ratingId}/confirm");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> DeleteRatingAsync(int ratingId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/ratings/{ratingId}");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> FavoriteAddAsync(int mediaId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/media/{mediaId}/favorite");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<(bool ok, string message)> FavoriteRemoveAsync(int mediaId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/media/{mediaId}/favorite");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    public async Task<ProfileDto?> GetProfileAsync(string username)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Uri.EscapeDataString(username)}/profile");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return null;
        try { return JsonSerializer.Deserialize<ProfileDto>(txt, JsonOpts); } catch { return null; }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/users/leaderboard");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();
        try { return JsonSerializer.Deserialize<List<LeaderboardEntry>>(txt, JsonOpts) ?? new(); } catch { return new(); }
    }

    public async Task<List<MediaDto>> GetFavoritesAsync(string username)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Uri.EscapeDataString(username)}/favorites");
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();

        try { return JsonSerializer.Deserialize<List<MediaDto>>(txt, JsonOpts) ?? new(); }
        catch { return new(); }
    }


    public async Task<List<RatingDto>> GetRatingHistoryAsync(string username)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Uri.EscapeDataString(username)}/ratings");
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();
        try { return JsonSerializer.Deserialize<List<RatingDto>>(txt, JsonOpts) ?? new(); } catch { return new(); }
    }

    public async Task<List<MediaDetailsResponse>> GetRecommendationsAsync(string username, int limit)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/users/{Uri.EscapeDataString(username)}/recommendations?limit={limit}"
        );
        ApplyAuth(req);

        var res = await _http.SendAsync(req);
        var txt = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return new();

        try { return JsonSerializer.Deserialize<List<MediaDetailsResponse>>(txt, JsonOpts) ?? new(); }
        catch { return new(); }
    }


    public async Task<(bool ok, string message)> ChangePasswordAsync(string username, string newPassword)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{Uri.EscapeDataString(username)}/profile")
        { Content = JsonBody(new { Password = newPassword }) };
        ApplyAuth(req);
        var res = await _http.SendAsync(req);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }
}
