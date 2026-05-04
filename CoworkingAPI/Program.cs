using Coworking.Shared.Models.Interfaces;
using CoworkingAPI.Data;
using CoworkingAPI.Features.BuildingTopology;
using CoworkingAPI.Hubs;
using CoworkingAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddControllers();
var app = builder.Build();

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
