using System.Security.Claims;
using CoworkingAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Hubs
{
    [Authorize]
    public class AlertTelemetryHub : Hub
    {
        private readonly ILogger<AlertTelemetryHub> logger;
        private readonly AppDbContext dbContext;

        public AlertTelemetryHub(ILogger<AlertTelemetryHub> logger, AppDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }
        
        public override async Task OnConnectedAsync()
        {
            logger.LogInformation($"Client connected: {Context.ConnectionId}");
            
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var buildingIds = await dbContext.UserBuildings
                    .Where(ub => ub.UserId == userId)
                    .Select(ub => ub.BuildingId)
                    .ToListAsync();
                    
                foreach (var buildingId in buildingIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Building_{buildingId}");
                    logger.LogInformation($"User {userId} added to group Building_{buildingId}");
                }
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogInformation("Client disconnected: {ConnectionId}. Reason: {Exception}", Context.ConnectionId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }
    }    
}
