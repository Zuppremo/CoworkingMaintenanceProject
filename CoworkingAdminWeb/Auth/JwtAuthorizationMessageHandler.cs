using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CoworkingAdminWeb.Auth;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService localStorage;
    private readonly AuthenticationStateProvider? authenticationStateProvider;
    private readonly NavigationManager? navigationManager;

    public JwtAuthorizationMessageHandler(
        ILocalStorageService localStorage,
        AuthenticationStateProvider? authenticationStateProvider = null,
        NavigationManager? navigationManager = null)
    {
        this.localStorage = localStorage;
        this.authenticationStateProvider = authenticationStateProvider;
        this.navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !(request.RequestUri?.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            await localStorage.RemoveItemAsync("authToken");
            await localStorage.RemoveItemAsync("userInfo");
            await localStorage.RemoveItemAsync("tokenExpiry");

            if (authenticationStateProvider is JwtAuthenticationStateProvider jwtProvider)
            {
                jwtProvider.MarkUserAsLoggedOut();
            }

            if (navigationManager != null &&
                !navigationManager.Uri.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
            {
                navigationManager.NavigateTo("login");
            }
        }

        return response;
    }
}
