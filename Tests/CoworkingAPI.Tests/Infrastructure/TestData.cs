using System.Security.Claims;
using Coworking.Shared.Enums;
using Coworking.Shared.Models;
using CoworkingAPI.Data;
using Microsoft.AspNetCore.Http;

namespace CoworkingAPI.Tests.Infrastructure;

public sealed record SeededTopology(
    Guid BuildingId,
    Guid FloorId,
    Guid RestroomId,
    Guid ToiletId,
    Guid SinkId,
    Guid HandDryerId);

public static class TestData
{
    public static readonly DateTimeOffset BaseTime = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates Building -> Floor -> Restroom (10x10) with a Toilet (1,1), a Sink (2,1) and a HandDryer (2,1, wall layer).
    /// </summary>
    public static SeededTopology SeedTopology(
        AppDbContext context,
        string buildingName = "Edificio A",
        int floorNumber = 1,
        string restroomIdentifier = "Baño 1")
    {
        var building = new Building { Id = Guid.NewGuid(), Name = buildingName };
        var floor = new Floor { Id = Guid.NewGuid(), BuildingId = building.Id, FloorNumber = floorNumber };
        var restroom = new Restroom
        {
            Id = Guid.NewGuid(),
            FloorId = floor.Id,
            Identifier = restroomIdentifier,
            GridWidth = 10,
            GridHeight = 10
        };

        var toilet = new Toilet { RestroomId = restroom.Id, PositionX = 1, PositionY = 1 };
        var sink = new Sink { RestroomId = restroom.Id, PositionX = 2, PositionY = 1 };
        var dryer = new HandDryer { RestroomId = restroom.Id, PositionX = 2, PositionY = 1 };

        context.Buildings.Add(building);
        context.Floors.Add(floor);
        context.Restrooms.Add(restroom);
        context.RestroomAssets.AddRange(toilet, sink, dryer);
        context.SaveChanges();

        return new SeededTopology(building.Id, floor.Id, restroom.Id, toilet.Id, sink.Id, dryer.Id);
    }

    public static IssueReport AddReport(
        AppDbContext context,
        Guid assetId,
        ReportStatus status = ReportStatus.Generated,
        DateTimeOffset? generationDate = null,
        DateTimeOffset? attentionDate = null,
        DateTimeOffset? resolutionDate = null,
        string description = "Sin papel higiénico",
        string assignedOperatorId = "",
        string reportingUserId = "coworker-1")
    {
        var report = new IssueReport
        {
            Id = Guid.NewGuid(),
            RestroomAssetId = assetId,
            Status = status,
            GenerationDate = generationDate ?? BaseTime,
            AttentionDate = attentionDate,
            ResolutionDate = resolutionDate,
            IssueDescription = description,
            AssignedOperatorId = assignedOperatorId,
            ReportingUserId = reportingUserId
        };

        context.IssueReports.Add(report);
        context.SaveChanges();
        return report;
    }

    public static ApplicationUser AddUser(AppDbContext context, string email, params Guid[] buildingIds)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };
        context.Users.Add(user);

        foreach (var buildingId in buildingIds)
        {
            context.UserBuildings.Add(new UserBuilding { UserId = user.Id, BuildingId = buildingId });
        }

        context.SaveChanges();
        return user;
    }

    public static ClaimsPrincipal Principal(string? userId, params string[] roles)
    {
        if (userId == null && roles.Length == 0)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    public static IHttpContextAccessor AccessorFor(string? userId, params string[] roles)
    {
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = Principal(userId, roles) }
        };
    }
}
