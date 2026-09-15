using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace CoworkingAdminWeb.Auth;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService localStorage;

    public JwtAuthorizationMessageHandler(ILocalStorageService localStorage)
    {
        this.localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
