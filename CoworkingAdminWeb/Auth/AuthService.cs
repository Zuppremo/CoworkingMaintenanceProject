using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace CoworkingAdminWeb.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResult
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class AuthService
{
    private readonly HttpClient httpClient;
    private readonly AuthenticationStateProvider authenticationStateProvider;
    private readonly ILocalStorageService localStorage;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider, ILocalStorageService localStorage)
    {
        this.httpClient = httpClient;
        this.authenticationStateProvider = authenticationStateProvider;
        this.localStorage = localStorage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var loginModel = new LoginRequest { Email = email, Password = password };
        var response = await httpClient.PostAsJsonAsync("login?useCookies=false", loginModel);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        if (result == null || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            return false;
        }

        await localStorage.SetItemAsync("authToken", result.AccessToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

        var meResponse = await httpClient.GetAsync("api/auth/me");
        if (meResponse.IsSuccessStatusCode)
        {
            var userInfo = await meResponse.Content.ReadFromJsonAsync<UserInfo>();
            if (userInfo != null)
            {
                await localStorage.SetItemAsync("userInfo", userInfo);
            }
        }

        ((JwtAuthenticationStateProvider)authenticationStateProvider).MarkUserAsAuthenticated(result.AccessToken);

        return true;
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync("authToken");
        await localStorage.RemoveItemAsync("userInfo");
        ((JwtAuthenticationStateProvider)authenticationStateProvider).MarkUserAsLoggedOut();
        httpClient.DefaultRequestHeaders.Authorization = null;
    }
}
