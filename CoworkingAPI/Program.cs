using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Hubs;
using CoworkingAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CoworkingAPI.Features.AdministrativeHierarchy;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddSingleton<ITopologyCache, TopologyCache>();
builder.Services.AddHostedService<TopologyCacheWarmer>();
builder.Services.AddTransient<IAlertPublisher, SignalRAlertPublisher>();

builder.Services.AddScoped<CoworkingAPI.Features.BuildingTopology.SyncTopology.SyncRestroomTopologyCommandHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.BuildingTopology.SyncTopology.GetRestroomTopologyQueryHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.IssueReports.CreateIssueReportCommandHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.IssueReports.GetActiveIssuesQueryHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.IssueReports.AttendIssueReportCommandHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.IssueReports.ResolveIssueReportCommandHandler>();

// Hierarchy Handlers
builder.Services.AddScoped<CreateBuildingCommandHandler>();
builder.Services.AddScoped<DeleteBuildingCommandHandler>();
builder.Services.AddScoped<CreateFloorCommandHandler>();
builder.Services.AddScoped<CreateRestroomCommandHandler>();
builder.Services.AddScoped<GetHierarchyQueryHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

builder.Services.AddControllers();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    await DataSeeder.SeedRolesAndSuperAdminAsync(scope.ServiceProvider);
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CoworkingAPI.Middleware.SignalRQueryStringAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AlertTelemetryHub>("/alertsHub");
app.MapHub<AlertTelemetryHub>("/hubs/alert-telemetry");

app.MapIdentityApi<ApplicationUser>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.Run();
