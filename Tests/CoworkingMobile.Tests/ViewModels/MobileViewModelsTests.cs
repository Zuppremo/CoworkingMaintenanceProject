using System.Net;
using System.Net.Http.Json;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using CoworkingMobile.Services;
using CoworkingMobile.Tests.Shims;
using CoworkingMobile.ViewModels;
using CoworkingMobile.ViewModels.Auth;
using Moq;

namespace CoworkingMobile.Tests.ViewModels;

[Collection("MauiTestHost")]
public class MobileViewModelsTests : IDisposable
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => this.responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    public MobileViewModelsTests() => MauiTestHost.Reset();
    public void Dispose() => MauiTestHost.Reset();

    private static IHttpClientFactory FactoryFor(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Api"))
            .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
        return factory.Object;
    }

    [Fact]
    public void LoginViewModel_EmptyCredentials_ShowsAlertWithoutCallingApi()
    {
        var called = false;
        var factory = FactoryFor(_ => { called = true; return new HttpResponseMessage(HttpStatusCode.OK); });
        var vm = new LoginViewModel(new AuthService(factory));

        vm.LoginCommand.Execute(null);

        Assert.False(called);
        Assert.Single(MauiTestHost.Alerts);
        Assert.Empty(MauiTestHost.NavigatedRoutes);
    }

    [Fact]
    public void LoginViewModel_ValidCredentials_NavigatesToMain()
    {
        var factory = FactoryFor(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LoginResult { AccessToken = "jwt" })
        });
        var vm = new LoginViewModel(new AuthService(factory))
        {
            Email = "staff@coworking.com",
            Password = "Password1!"
        };

        vm.LoginCommand.Execute(null);
        Assert.True(SpinWait.SpinUntil(() => !MauiTestHost.NavigatedRoutes.IsEmpty, TimeSpan.FromSeconds(2)));

        Assert.Contains("//main", MauiTestHost.NavigatedRoutes);
        Assert.Empty(MauiTestHost.Alerts);
    }

    [Fact]
    public void LoginViewModel_InvalidCredentials_DisplaysErrorAlert()
    {
        var factory = FactoryFor(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var vm = new LoginViewModel(new AuthService(factory))
        {
            Email = "staff@coworking.com",
            Password = "bad"
        };

        vm.LoginCommand.Execute(null);
        Assert.True(SpinWait.SpinUntil(() => !MauiTestHost.Alerts.IsEmpty, TimeSpan.FromSeconds(2)));

        Assert.Single(MauiTestHost.Alerts);
        Assert.Empty(MauiTestHost.NavigatedRoutes);
    }

    [Fact]
    public async Task CleaningDashboardViewModel_LoadAttendResolve_UpdatesCollectionsAndCounters()
    {
        var issue1 = new IssueReportDto
        {
            Id = Guid.NewGuid(),
            IssueDescription = "Sin papel",
            Status = ReportStatus.Generated,
            BuildingName = "Norte",
            FloorNumber = 2,
            RestroomIdentifier = "Hombres",
            AssetType = AssetType.PaperDispenser,
            PositionX = 1,
            PositionY = 3,
            GenerationDate = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        var issue2 = new IssueReportDto
        {
            Id = Guid.NewGuid(),
            IssueDescription = "Lavamanos",
            Status = ReportStatus.InProgress,
            BuildingName = "Norte",
            FloorNumber = 2,
            RestroomIdentifier = "Mujeres",
            AssetType = AssetType.Sink,
            GenerationDate = DateTimeOffset.UtcNow.AddHours(-2)
        };

        var factory = FactoryFor(req =>
        {
            if (req.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { issue1, issue2 }) };
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var auth = new AuthService(factory);
        var service = new IssueReportsService(factory, auth);
        var vm = new CleaningDashboardViewModel(service, auth);

        await vm.LoadIssuesAsync();

        Assert.Equal(2, vm.ActiveCount);
        Assert.Equal(1, vm.PendingCount);
        Assert.Equal(1, vm.InProgressCount);
        Assert.False(vm.HasNoIssues);

        var firstItem = vm.ActiveIssues[0];
        Assert.True(firstItem.CanAttend);
        Assert.Equal("PENDIENTE", firstItem.StatusBadgeText);
        Assert.Equal("Dispensador", firstItem.AssetName);
        Assert.Equal("Coord (1, 3)", firstItem.CoordinatesText);

        await vm.AttendIssueAsync(firstItem);

        Assert.Equal(ReportStatus.InProgress, firstItem.Status);
        Assert.False(firstItem.CanAttend);
        Assert.Equal("EN ATENCIÓN", firstItem.StatusBadgeText);
        Assert.Equal(0, vm.PendingCount);
        Assert.Equal(2, vm.InProgressCount);

        await vm.ResolveIssueAsync(firstItem);

        Assert.Single(vm.ActiveIssues);
        Assert.Equal(1, vm.ActiveCount);
        Assert.Equal(1, vm.InProgressCount);
    }

    [Fact]
    public async Task CleaningDashboardViewModel_Logout_ClearsTokenAndNavigatesToLogin()
    {
        MauiTestHost.Storage["authToken"] = "jwt";
        var factory = FactoryFor(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var auth = new AuthService(factory);
        var service = new IssueReportsService(factory, auth);
        var vm = new CleaningDashboardViewModel(service, auth);

        await vm.LogoutAsync();

        Assert.Null(await auth.GetTokenAsync());
        Assert.Contains("//login", MauiTestHost.NavigatedRoutes);
    }
}
