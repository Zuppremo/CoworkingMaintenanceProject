using System.Net.Http.Json;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using Microsoft.AspNetCore.SignalR.Client;

namespace CoworkingMobile.Services;

public class IssueReportsService : IAsyncDisposable
{
    private readonly HttpClient httpClient;
    private HubConnection? hubConnection;
    private readonly string baseUrl;

    public event Action<ReportStatusUpdateTelemetry>? OnReportStatusUpdated;
    public event Action? OnConnectionStatusChanged;

    public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

    public IssueReportsService(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient("Api");
        
        baseUrl = DeviceInfo.Platform == DevicePlatform.Android 
            ? "http://10.0.2.2:5200" 
            : "http://localhost:5200";
    }

    public async Task InitializeSignalRAsync()
    {
        if (hubConnection != null) return;

        hubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/alert-telemetry")
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<ReportStatusUpdateTelemetry>("OnReportStatusUpdated", (telemetry) =>
        {
            OnReportStatusUpdated?.Invoke(telemetry);
        });

        hubConnection.Reconnected += (connectionId) =>
        {
            OnConnectionStatusChanged?.Invoke();
            return Task.CompletedTask;
        };

        hubConnection.Closed += (error) =>
        {
            OnConnectionStatusChanged?.Invoke();
            return Task.CompletedTask;
        };

        try
        {
            await hubConnection.StartAsync();
            OnConnectionStatusChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error conectando a SignalR: {ex.Message}");
            OnConnectionStatusChanged?.Invoke();
        }
    }

    public async Task<List<IssueReportDto>> GetActiveIssuesAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<IssueReportDto>>("api/IssueReports/active");
            return result ?? new List<IssueReportDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo incidencias activas: {ex.Message}");
            return new List<IssueReportDto>();
        }
    }

    public async Task<bool> AttendIssueAsync(Guid reportId, string operatorName = "Personal de Aseo")
    {
        try
        {
            var response = await httpClient.PostAsync($"api/IssueReports/{reportId}/attend?operatorId={Uri.EscapeDataString(operatorName)}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marcando en progreso: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResolveIssueAsync(Guid reportId, string operatorName = "Personal de Aseo")
    {
        try
        {
            var response = await httpClient.PostAsync($"api/IssueReports/{reportId}/resolve?operatorId={Uri.EscapeDataString(operatorName)}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resolviendo incidencia: {ex.Message}");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
