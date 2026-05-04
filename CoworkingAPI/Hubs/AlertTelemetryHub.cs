using Microsoft.AspNetCore.SignalR;

namespace CoworkingAPI.Hubs
{
    public class AlertTelemetryHub : Hub
    {
        private readonly ILogger<AlertTelemetryHub> logger;

        public AlertTelemetryHub(ILogger<AlertTelemetryHub> logger)
        {
            this.logger = logger;
        }
        
        public override async Task OnConnectedAsync()
        {
            logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogInformation("Client disconnected: {ConnectionId}. Reason: {Exception}", Context.ConnectionId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SuscribeToFloor(string floorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, floorId);
            logger.LogInformation("Client {ConnectionId} subscribed to floor {FloorId}", Context.ConnectionId, floorId);
        }

        public async Task UnsuscribeToFloor(string floorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, floorId);
        }
    }    
}
