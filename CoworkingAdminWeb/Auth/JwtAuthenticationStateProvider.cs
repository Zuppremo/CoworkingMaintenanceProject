using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace CoworkingAdminWeb.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService localStorage;
    private const string TOKEN_KEY = "authToken";

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        this.localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsync<string>(TOKEN_KEY);
        var userInfo = await localStorage.GetItemAsync<UserInfo>("userInfo");
        
        if (string.IsNullOrWhiteSpace(token) || userInfo == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity("bearer");
        identity.AddClaim(new Claim(ClaimTypes.Name, userInfo.Email));
        
        foreach (var role in userInfo.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public void MarkUserAsAuthenticated(string token)
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }
}
