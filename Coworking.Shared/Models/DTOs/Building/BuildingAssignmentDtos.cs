namespace Coworking.Shared.Models.DTOs.Building;

public record UserBuildingSummaryDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public List<Guid> AssignedBuildingIds { get; init; } = new();
}

public record UpdateBuildingUsersRequest
{
    public List<string> UserIds { get; init; } = new();
}
