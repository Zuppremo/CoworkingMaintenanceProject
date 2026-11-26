using System.Net;
using System.Net.Http.Json;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using CoworkingMobile.Services;
using CoworkingMobile.Tests.Shims;
using Moq;

namespace CoworkingMobile.Tests.Services;

[Collection("MauiTestHost")]
public class MobileServicesTests : IDisposable
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

    public MobileServicesTests() => MauiTestHost.Reset();
    public void Dispose() => MauiTestHost.Reset();

    private static IHttpClientFactory FactoryFor(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Api"))
            .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
        return factory.Object;
    }

    [Fact]
    public void ApiConstants_LocalUrl_AdaptsToAndroidEmulator()
    {
        MauiTestHost.PlatformName = "Android";
        Assert.Equal("http://10.0.2.2:5200", ApiConstants.LocalApiUrl);

        MauiTestHost.PlatformName = "Linux";
        Assert.Equal("https://localhost:7200", ApiConstants.LocalApiUrl);
    }

    [Fact]
    public async Task AuthService_LoginAsync_ValidCredentials_SavesTokenInSecureStorage()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LoginResult { AccessToken = "mobile-jwt", ExpiresIn = 3600 })
        });
        var service = new AuthService(FactoryFor(handler));

        var ok = await service.LoginAsync("staff@coworking.com", "Pass123!");

        Assert.True(ok);
        Assert.Equal("mobile-jwt", await service.GetTokenAsync());
    }

    [Fact]
    public async Task AuthService_LoginAsync_UnauthorizedOrEmptyToken_ReturnsFalse()
    {
        var unauth = new AuthService(FactoryFor(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        Assert.False(await unauth.LoginAsync("a@b.com", "wrong"));
        Assert.Null(await unauth.GetTokenAsync());

        var empty = new AuthService(FactoryFor(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LoginResult { AccessToken = "" })
        })));
        Assert.False(await empty.LoginAsync("a@b.com", "pass"));
    }

    [Fact]
    public async Task AuthService_Logout_RemovesStoredToken()
    {
        MauiTestHost.Storage["authToken"] = "tok";
        var service = new AuthService(FactoryFor(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        service.Logout();

        Assert.Null(await service.GetTokenAsync());
    }

    [Fact]
    public async Task IssueReportsService_GetActiveIssuesAsync_AttachesBearerTokenAndDeserializes()
    {
        MauiTestHost.Storage["authToken"] = "tok-xyz";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<IssueReportDto>
            {
                new() { Id = Guid.NewGuid(), IssueDescription = "Fuga", Status = ReportStatus.Generated, BuildingName = "Norte" }
            })
        });
        var factory = FactoryFor(handler);
        var service = new IssueReportsService(factory, new AuthService(factory));

        var issues = await service.GetActiveIssuesAsync();

        var item = Assert.Single(issues);
        Assert.Equal("Fuga", item.IssueDescription);
        Assert.Equal("Bearer", Assert.Single(handler.Requests).Headers.Authorization?.Scheme);
        Assert.Equal("tok-xyz", handler.Requests[0].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task IssueReportsService_GetActiveIssuesAsync_HttpError_ReturnsEmptyList()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var factory = FactoryFor(handler);
        var service = new IssueReportsService(factory, new AuthService(factory));

        var issues = await service.GetActiveIssuesAsync();

        Assert.Empty(issues);
    }

    [Fact]
    public async Task IssueReportsService_AttendAndResolve_PostToExpectedEndpoints()
    {
        var id = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var factory = FactoryFor(handler);
        var service = new IssueReportsService(factory, new AuthService(factory));

        Assert.True(await service.AttendIssueAsync(id, "Juan Perez"));
        Assert.True(await service.ResolveIssueAsync(id, "Juan Perez"));

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal($"/api/IssueReports/{id}/attend?operatorId=Juan%20Perez", handler.Requests[0].RequestUri!.PathAndQuery);
        Assert.Equal($"/api/IssueReports/{id}/resolve?operatorId=Juan%20Perez", handler.Requests[1].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task IssueReportsService_InitializeSignalRAsync_WithoutToken_DoesNotConnectAndNotifies()
    {
        var factory = FactoryFor(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new IssueReportsService(factory, new AuthService(factory));
        var statusChanged = 0;
        service.OnConnectionStatusChanged += () => statusChanged++;

        await service.InitializeSignalRAsync();

        Assert.False(service.IsConnected);
        Assert.Equal(1, statusChanged);
    }
}
