using CoworkingAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CoworkingAPI.Features.BuildingTopology;

public class TopologyCacheWarmer : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ITopologyCache topologyCache;

    public TopologyCacheWarmer(IServiceProvider serviceProvider, ITopologyCache topologyCache)
    {
        this.serviceProvider = serviceProvider;
        this.topologyCache = topologyCache;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var assetsIds = await context.RestroomAssets.AsNoTracking().Select(a => a.Id).ToListAsync();
        
        topologyCache.Seed(assetsIds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}