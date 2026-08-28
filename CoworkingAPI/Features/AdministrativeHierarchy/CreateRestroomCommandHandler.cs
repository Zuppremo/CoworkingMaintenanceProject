using Coworking.Shared.Models;
using Coworking.Shared.Models.DTOs.Rest;
using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.AdministrativeHierarchy;

public class CreateRestroomCommandHandler
{
    private readonly AppDbContext context;

    public CreateRestroomCommandHandler(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<Guid?> ExecuteAsync(CreateRestroomRequest request)
    {
        var floorExists = await context.Floors.AnyAsync(f => f.Id == request.FloorId);
        if (!floorExists) return null;

        var restroom = new Restroom
        {
            Id = Guid.NewGuid(),
            Identifier = request.Identifier,
            FloorId = request.FloorId,
            GridWidth = 15, // Default sizes
            GridHeight = 15
        };

        context.Restrooms.Add(restroom);
        await context.SaveChangesAsync();

        return restroom.Id;
    }
}
