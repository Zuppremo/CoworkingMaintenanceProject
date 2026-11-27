using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using CoworkingAdminWeb.Auth;
using Moq;
using Moq.Protected;

namespace CoworkingAdminWeb.Tests.Auth;

public class AuthTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
        public List<HttpRequestMessage> Requests { get; } = new();

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => this.responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }

    [Fact]
    public async Task JwtAuthStateProvider_NoToken_ReturnsAnonymousPrincipal()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        storage.Setup(s => s.GetItemAsync<UserInfo>("userInfo", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var provider = new JwtAuthenticationStateProvider(storage.Object);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task JwtAuthStateProvider_ValidTokenAndUserInfo_ReturnsClaimsWithRoles()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jwt-token");
        storage.Setup(s => s.GetItemAsync<UserInfo>("userInfo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo { Email = "admin@coworking.com", Roles = { "SuperAdmin", "Admin" } });

        var provider = new JwtAuthenticationStateProvider(storage.Object);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("admin@coworking.com", state.User.Identity?.Name);
        Assert.True(state.User.IsInRole("SuperAdmin"));
        Assert.True(state.User.IsInRole("Admin"));
    }

    [Fact]
    public async Task JwtAuthStateProvider_MarkUserAsLoggedOut_NotifiesWithAnonymousUser()
    {
        var storage = new Mock<ILocalStorageService>();
        var provider = new JwtAuthenticationStateProvider(storage.Object);
        ClaimsPrincipal? notifiedUser = null;
        provider.AuthenticationStateChanged += async t => notifiedUser = (await t).User;

        provider.MarkUserAsLoggedOut();

        Assert.NotNull(notifiedUser);
        Assert.False(notifiedUser!.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task JwtAuthorizationMessageHandler_AddsBearerHeaderWhenTokenExists()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("my-token");

        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new JwtAuthorizationMessageHandler(storage.Object) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };

        await client.GetAsync("api/Hierarchy");

        var sent = Assert.Single(inner.Requests);
        Assert.Equal("Bearer", sent.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", sent.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task JwtAuthorizationMessageHandler_NoToken_DoesNotAddAuthorizationHeader()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new JwtAuthorizationMessageHandler(storage.Object) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };

        await client.GetAsync("api/Hierarchy");

        Assert.Null(Assert.Single(inner.Requests).Headers.Authorization);
    }

    [Fact]
    public async Task AuthService_LoginAsync_ValidCredentials_StoresTokenAndUserInfo()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("tok-123");
        storage.Setup(s => s.GetItemAsync<UserInfo>("userInfo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo { Email = "a@b.com", Roles = { "Admin" } });

        var httpHandler = new StubHttpMessageHandler(req =>
        {
            if (req.RequestUri!.PathAndQuery.StartsWith("/login"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new LoginResult { AccessToken = "tok-123", ExpiresIn = 3600 })
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserInfo { Email = "a@b.com", Roles = { "Admin" } })
            };
        });

        using var httpClient = new HttpClient(httpHandler) { BaseAddress = new Uri("https://api.test/") };
        var stateProvider = new JwtAuthenticationStateProvider(storage.Object);
        var authService = new AuthService(httpClient, stateProvider, storage.Object);

        var ok = await authService.LoginAsync("a@b.com", "Pass123!");

        Assert.True(ok);
        storage.Verify(s => s.SetItemAsync("authToken", "tok-123", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.SetItemAsync("userInfo", It.Is<UserInfo>(u => u.Email == "a@b.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthService_LoginAsync_InvalidCredentials_ReturnsFalseAndDoesNotStoreToken()
    {
        var storage = new Mock<ILocalStorageService>();
        var httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var httpClient = new HttpClient(httpHandler) { BaseAddress = new Uri("https://api.test/") };
        var authService = new AuthService(httpClient, new JwtAuthenticationStateProvider(storage.Object), storage.Object);

        var ok = await authService.LoginAsync("bad@b.com", "wrong");

        Assert.False(ok);
        storage.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuthService_LogoutAsync_ClearsStorageAndAuthorizationHeader()
    {
        var storage = new Mock<ILocalStorageService>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            BaseAddress = new Uri("https://api.test/")
        };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "tok");
        var authService = new AuthService(httpClient, new JwtAuthenticationStateProvider(storage.Object), storage.Object);

        await authService.LogoutAsync();

        storage.Verify(s => s.RemoveItemAsync("authToken", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.RemoveItemAsync("userInfo", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public async Task JwtAuthStateProvider_ExpiredToken_ClearsStorageAndReturnsAnonymous()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("expired-token");
        storage.Setup(s => s.GetItemAsync<UserInfo>("userInfo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo { Email = "admin@coworking.com", Roles = { "Admin" } });
        storage.Setup(s => s.GetItemAsync<DateTimeOffset?>("tokenExpiry", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(-5));

        var provider = new JwtAuthenticationStateProvider(storage.Object);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        storage.Verify(s => s.RemoveItemAsync("authToken", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.RemoveItemAsync("userInfo", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.RemoveItemAsync("tokenExpiry", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JwtAuthorizationMessageHandler_UnauthorizedResponse_ClearsStorageAndMarksLoggedOut()
    {
        var storage = new Mock<ILocalStorageService>();
        storage.Setup(s => s.GetItemAsync<string>("authToken", It.IsAny<CancellationToken>()))
            .ReturnsAsync("stale-token");

        var stateProvider = new JwtAuthenticationStateProvider(storage.Object);
        ClaimsPrincipal? notifiedUser = null;
        stateProvider.AuthenticationStateChanged += async t => notifiedUser = (await t).User;

        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var handler = new JwtAuthorizationMessageHandler(storage.Object, stateProvider) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };

        var response = await client.GetAsync("api/Hierarchy");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        storage.Verify(s => s.RemoveItemAsync("authToken", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.RemoveItemAsync("userInfo", It.IsAny<CancellationToken>()), Times.Once);
        storage.Verify(s => s.RemoveItemAsync("tokenExpiry", It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(notifiedUser);
        Assert.False(notifiedUser!.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task JwtAuthorizationMessageHandler_UnauthorizedOnLogin_DoesNotClearStorage()
    {
        var storage = new Mock<ILocalStorageService>();
        var inner = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var handler = new JwtAuthorizationMessageHandler(storage.Object) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };

        await client.PostAsJsonAsync("login?useCookies=false", new LoginRequest());

        storage.Verify(s => s.RemoveItemAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
