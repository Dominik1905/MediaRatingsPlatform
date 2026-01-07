using Microsoft.JSInterop;

namespace MRP_Client.Services;

public sealed class AuthState
{
    private readonly IJSRuntime _js;

    public string Token { get; private set; } = "";
    public string Username { get; private set; } = "";

    public event Action? Changed;

    public AuthState(IJSRuntime js) => _js = js;

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public async Task LoadAsync()
    {
        Token = await _js.InvokeAsync<string>("mrpAuth.getToken") ?? "";
        Username = await _js.InvokeAsync<string>("mrpAuth.getUsername") ?? "";
        Changed?.Invoke();
    }

    public async Task SetAsync(string username, string token)
    {
        Username = username;
        Token = token;
        await _js.InvokeVoidAsync("mrpAuth.set", username, token);
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        Username = "";
        Token = "";
        await _js.InvokeVoidAsync("mrpAuth.clear");
        Changed?.Invoke();
    }
}