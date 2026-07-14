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
        _authStateProvider.NotifyUserAuthentication(token.access_token);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("farmAuth.clear");
        _authStateProvider.NotifyUserLogout();
    }

    private sealed record TokenResponse(string access_token, string token_type);
}
