using System.Net;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Bunit;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs.Analytics;
using Coworking.Shared.Models.DTOs.Building;
using Coworking.Shared.Models.DTOs.Rest;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingAdminWeb.Auth;
using CoworkingAdminWeb.Components;
using CoworkingAdminWeb.Pages;
using CoworkingAdminWeb.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CoworkingAdminWeb.Tests.Pages;

public class PagesBunitTests : BunitContext
{
    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> handler;
        public List<(HttpMethod Method, string Path)> Calls { get; } = new();

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => this.handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request.Method, request.RequestUri!.PathAndQuery));
            return Task.FromResult(handler(request));
        }
    }

    private void RegisterHttp(Func<HttpRequestMessage, HttpResponseMessage> handler, out MockHttpHandler spy)
    {
        spy = new MockHttpHandler(handler);
        var client = new HttpClient(spy) { BaseAddress = new Uri("https://api.test/") };
        Services.AddSingleton(client);
    }

    [Fact]
    public void AssetPalette_RendersAllFourAssetTypes()
    {
        Services.AddSingleton<GridEditorState>();

        var cut = Render<AssetPalette>();

        Assert.Contains("Inodoro", cut.Markup);
        Assert.Contains("Lavamanos", cut.Markup);
        Assert.Contains("Dispensador Papel", cut.Markup);
        Assert.Contains("Secador Manos", cut.Markup);
    }

    [Fact]
    public void GridCanvas_RendersSvgForGridDimensionsAndPlacedAssets()
    {
        var state = new GridEditorState();
        state.LoadState(4, 3, new List<RestroomAssetDto>
        {
            new() { Id = Guid.NewGuid(), Type = AssetType.Toilet, PositionX = 1, PositionY = 2 }
        });
        Services.AddSingleton(state);

        var cut = Render<GridCanvas>(p => p.Add(c => c.CellSize, 40));

        var svg = cut.Find("svg");
        Assert.Equal("160", svg.GetAttribute("width"));
        Assert.Equal("120", svg.GetAttribute("height"));
        Assert.NotEmpty(cut.FindAll("foreignObject"));
    }

    [Fact]
    public void ReportIssue_WithoutRestroomParam_ShowsQrPrompt()
    {
        RegisterHttp(_ => new HttpResponseMessage(HttpStatusCode.OK), out _);

        var cut = Render<ReportIssue>();

        Assert.Contains("Escanea el código QR", cut.Markup);
    }

    [Fact]
    public void ReportIssue_WithRestroomParam_LoadsAssetsAndSubmitsReport()
    {
        var restroomId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        RegisterHttp(req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains($"api/Topology/{restroomId}"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new RestroomLayoutDto
                    {
                        RestroomId = restroomId,
                        Identifier = "Baño Lobby",
                        GridWidth = 5,
                        GridHeight = 5,
                        Assets = { new RestroomAssetDto { Id = assetId, Type = AssetType.Toilet, PositionX = 1, PositionY = 1 } }
                    })
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, out var spy);

        var cut = Render<ReportIssue>(p => p.Add(r => r.RestroomIdParam, restroomId));

        Assert.Contains("Baño Lobby", cut.Markup);
        cut.FindAll("button").First(b => b.TextContent.Contains("Sin papel higiénico")).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Enviar Alerta de Mantenimiento")).Click();

        Assert.Contains(spy.Calls, c => c.Method == HttpMethod.Post && c.Path.EndsWith("api/IssueReports"));
        Assert.Contains("Reporte Enviado", cut.Markup);
    }

    [Fact]
    public void ReportIssue_ConflictResponse_ShowsConflictAlert()
    {
        var restroomId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        RegisterHttp(req =>
        {
            if (req.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new RestroomLayoutDto
                    {
                        RestroomId = restroomId,
                        Identifier = "Norte",
                        GridWidth = 5,
                        GridHeight = 5,
                        Assets = { new RestroomAssetDto { Id = assetId, Type = AssetType.Sink } }
                    })
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent.Create(new { error = "Ya existe un reporte activo" })
            };
        }, out _);

        var cut = Render<ReportIssue>(p => p.Add(r => r.RestroomIdParam, restroomId));
        cut.FindAll("button").First(b => b.TextContent.Contains("Sin jabón")).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Enviar Alerta de Mantenimiento")).Click();

        Assert.Contains("Ya existe un reporte activo", cut.Markup);
    }

    [Fact]
    public void HierarchyManager_LoadsBuildingsAndGeneratesQrCodeOnClick()
    {
        var restroomId = Guid.NewGuid();
        var buildings = new List<BuildingDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Torre Central",
                Floors =
                {
                    new FloorDto
                    {
                        Id = Guid.NewGuid(),
                        FloorNumber = 2,
                        Restrooms = { new RestroomHierarchyDto { Id = restroomId, Identifier = "Caballeros" } }
                    }
                }
            }
        };
        RegisterHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(buildings)
        }, out _);

        var cut = Render<HierarchyManager>();

        Assert.Contains("Torre Central", cut.Markup);
        cut.FindAll(".list-group-item").First(e => e.TextContent.Contains("Torre Central")).Click();
        cut.FindAll(".list-group-item").First(e => e.TextContent.Contains("Piso 2")).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("Código QR")).Click();

        Assert.Contains("data:image/png;base64,", cut.Markup);
    }

    [Fact]
    public void AdminDashboard_RendersMetricsAndFiltersIssuesBySearchTerm()
    {
        var dashboard = new ResolvedIssuesDashboardDto
        {
            TotalResolved = 2,
            TotalActive = 1,
            ResolutionRatePercent = 66.7,
            AvgMttrMinutes = 15,
            AvgMttaMinutes = 5,
            AvgActiveRepairMinutes = 10,
            FastestResolutionMinutes = 10,
            SlowestResolutionMinutes = 20,
            ResolvedIssues =
            {
                new ResolvedIssueItemDto
                {
                    Id = Guid.NewGuid(),
                    IssueDescription = "Inodoro tapado",
                    BuildingName = "Norte",
                    FloorNumber = 1,
                    RestroomIdentifier = "B1",
                    AssetType = AssetType.Toilet,
                    MttrMinutes = 10,
                    AssignedOperatorId = "op-1",
                    GenerationDate = DateTimeOffset.UtcNow.AddMinutes(-10),
                    ResolutionDate = DateTimeOffset.UtcNow
                },
                new ResolvedIssueItemDto
                {
                    Id = Guid.NewGuid(),
                    IssueDescription = "Secador roto",
                    BuildingName = "Sur",
                    FloorNumber = 2,
                    RestroomIdentifier = "B2",
                    AssetType = AssetType.HandDryer,
                    MttrMinutes = 20,
                    AssignedOperatorId = "op-2",
                    GenerationDate = DateTimeOffset.UtcNow.AddMinutes(-20),
                    ResolutionDate = DateTimeOffset.UtcNow
                }
            }
        };

        RegisterHttp(req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("Hierarchy"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<BuildingDto>()) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(dashboard) };
        }, out _);

        var cut = Render<AdminDashboard>();

        Assert.Contains("Inodoro tapado", cut.Markup);
        Assert.Contains("Secador roto", cut.Markup);

        cut.Find("input[placeholder*='Buscar']").Input("Secador");

        Assert.DoesNotContain("Inodoro tapado", cut.Markup);
        Assert.Contains("Secador roto", cut.Markup);
    }

    [Fact]
    public void Login_InvalidCredentials_DisplaysErrorMessage()
    {
        var storage = new Mock<ILocalStorageService>();
        var httpClient = new HttpClient(new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)))
        {
            BaseAddress = new Uri("https://api.test/")
        };
        var stateProvider = new JwtAuthenticationStateProvider(storage.Object);
        Services.AddSingleton<AuthenticationStateProvider>(stateProvider);
        Services.AddSingleton(new AuthService(httpClient, stateProvider, storage.Object));

        var cut = Render<Login>();
        cut.Find("input[placeholder='admin@coworking.com']").Change("user@test.com");
        cut.Find("input[type='password']").Change("wrong");
        cut.Find("form").Submit();

        Assert.Contains("Credenciales incorrectas", cut.Markup);
    }

    [Fact]
    public void HierarchyManager_AdminRole_HidesBuildingCreateAndDeleteButtons()
    {
        var buildings = new List<BuildingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Edificio Norte" }
        };
        RegisterHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(buildings)
        }, out _);

        var adminPrincipal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin@coworking.com"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
            }, "bearer"));

        var cut = Render<HierarchyManager>(p => p.AddCascadingValue(
            Task.FromResult(new AuthenticationState(adminPrincipal))));

        Assert.Contains("Edificio Norte", cut.Markup);
        Assert.DoesNotContain("+ Nuevo", cut.Markup);
        Assert.DoesNotContain("Eliminar", cut.Markup);
    }

    [Fact]
    public void HierarchyManager_SuperAdminRole_ShowsBuildingCreateAndDeleteButtons()
    {
        var buildings = new List<BuildingDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Edificio Norte" }
        };
        RegisterHttp(req =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/Hierarchy/users"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<UserBuildingSummaryDto>()) };

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(buildings) };
        }, out _);

        var superAdminPrincipal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "superadmin@coworking.com"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin")
            }, "bearer"));

        var cut = Render<HierarchyManager>(p => p.AddCascadingValue(
            Task.FromResult(new AuthenticationState(superAdminPrincipal))));

        Assert.Contains("Edificio Norte", cut.Markup);
        Assert.Contains("+ Nuevo", cut.Markup);
        Assert.Contains("Eliminar", cut.Markup);
    }

    [Fact]
    public void HierarchyManager_SuperAdmin_CanAssignUserToSelectedBuilding()
    {
        var buildingId = Guid.NewGuid();
        var buildings = new List<BuildingDto>
        {
            new() { Id = buildingId, Name = "Torre Sur" }
        };
        var users = new List<UserBuildingSummaryDto>
        {
            new() { UserId = "u-admin", Email = "admin@coworking.com", Roles = { "Admin" } },
            new() { UserId = "u-super", Email = "superadmin@coworking.com", Roles = { "SuperAdmin" } }
        };

        RegisterHttp(req =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.PathAndQuery.Contains("api/Hierarchy/users"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(users) };

            if (req.Method == HttpMethod.Put && req.RequestUri!.PathAndQuery.Contains($"api/Hierarchy/buildings/{buildingId}/users"))
                return new HttpResponseMessage(HttpStatusCode.OK);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(buildings) };
        }, out var spy);

        var superAdminPrincipal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "superadmin@coworking.com"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin")
            }, "bearer"));

        var cut = Render<HierarchyManager>(p => p.AddCascadingValue(
            Task.FromResult(new AuthenticationState(superAdminPrincipal))));

        cut.FindAll(".list-group-item").First(e => e.TextContent.Contains("Torre Sur")).Click();

        Assert.Contains("Acceso de Usuarios", cut.Markup);
        Assert.Contains("admin@coworking.com", cut.Markup);

        cut.Find("input[type='checkbox']").Change(true);
        cut.FindAll("button").First(b => b.TextContent.Contains("Guardar Accesos")).Click();

        Assert.Contains(spy.Calls, c => c.Method == HttpMethod.Put && c.Path.EndsWith($"api/Hierarchy/buildings/{buildingId}/users"));
        Assert.Contains("Accesos actualizados para Torre Sur", cut.Markup);
    }
}
