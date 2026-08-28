using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Hubs;
using CoworkingAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CoworkingAPI.Features.AdministrativeHierarchy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR().AddMessagePackProtocol();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ITopologyCache, TopologyCache>();
builder.Services.AddHostedService<TopologyCacheWarmer>();
builder.Services.AddTransient<IAlertPublisher, SignalRAlertPublisher>();

builder.Services.AddScoped<CoworkingAPI.Features.BuildingTopology.SyncTopology.SyncRestroomTopologyCommandHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.BuildingTopology.SyncTopology.GetRestroomTopologyQueryHandler>();
builder.Services.AddScoped<CoworkingAPI.Features.IssueReports.CreateIssueReportCommandHandler>();

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
              .AllowCredentials(); // SignalR needs credentials (can't use AllowAnyOrigin with AllowCredentials)
    });
});

builder.Services.AddControllers();
var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<AlertTelemetryHub>("/alertsHub");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
