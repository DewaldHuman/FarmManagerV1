using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Farm.Web.Core.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly FarmAuthStateProvider _authStateProvider;
    private readonly IJSRuntime _jsRuntime;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _authStateProvider = (FarmAuthStateProvider)authStateProvider;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var form = new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
        };

        using var content = new FormUrlEncodedContent(form);
        var response = await _httpClient.PostAsync("api/v1/core/auth/login", content);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (token is null || string.IsNullOrWhiteSpace(token.access_token))
        {
            return false;
        }

        await _jsRuntime.InvokeVoidAsync("farmAuth.set", token.access_token);

        // Sync the client's stored language to this user's saved preference so the
        // next boot's synchronous culture bootstrap (Program.cs) already picks it
        // up — this call needs the token we just stored, since /auth/me requires auth.
        var user = await GetCurrentUserAsync();
        if (user is not null)
        {
            await _jsRuntime.InvokeVoidAsync("farmCulture.set", user.PreferredLanguage);
        }

        _authStateProvider.NotifyUserAuthentication(token.access_token);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("farmAuth.clear");
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/core/auth/me");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>();
    }

    private sealed record TokenResponse(string access_token, string token_type);
}
