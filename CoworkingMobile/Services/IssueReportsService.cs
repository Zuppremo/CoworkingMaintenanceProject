using System.Net.Http.Json;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using Microsoft.AspNetCore.SignalR.Client;

namespace CoworkingMobile.Services;

public class IssueReportsService : IAsyncDisposable
{
    private readonly HttpClient httpClient;
    private readonly AuthService authService;
    private HubConnection? hubConnection;
    private readonly string baseUrl;

    public event Action<ReportStatusUpdateTelemetry>? OnReportStatusUpdated;
    public event Action? OnConnectionStatusChanged;

    public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

    public IssueReportsService(IHttpClientFactory httpClientFactory, AuthService authService)
    {
        this.authService = authService;
        httpClient = httpClientFactory.CreateClient("Api");
        
        baseUrl = ApiConstants.ApiBaseUrl;
    }

    public async Task InitializeSignalRAsync()
    {
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("SignalR: No se puede conectar porque no hay token de autenticación.");
            OnConnectionStatusChanged?.Invoke();
            return;
        }

        if (hubConnection == null)
        {
            var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/alert-telemetry";

            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => 
                {
                    options.AccessTokenProvider = () => authService.GetTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<ReportStatusUpdateTelemetry>("OnReportStatusUpdated", (telemetry) =>
            {
                OnReportStatusUpdated?.Invoke(telemetry);
            });

            hubConnection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"SignalR reconectado: {connectionId}");
                OnConnectionStatusChanged?.Invoke();
                return Task.CompletedTask;
            };

            hubConnection.Closed += (error) =>
            {
                Console.WriteLine($"SignalR cerrado: {error?.Message}");
                OnConnectionStatusChanged?.Invoke();
                return Task.CompletedTask;
            };
        }

        if (hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                Console.WriteLine($"Iniciando conexión a SignalR en: {baseUrl.TrimEnd('/')}/hubs/alert-telemetry");
                await hubConnection.StartAsync();
                Console.WriteLine("SignalR conectado exitosamente!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error conectando a SignalR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                OnConnectionStatusChanged?.Invoke();
            }
        }
    }

    public async Task DisconnectSignalRAsync()
    {
        if (hubConnection != null)
        {
            try
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al desconectar SignalR: {ex.Message}");
            }
            finally
            {
                hubConnection = null;
                OnConnectionStatusChanged?.Invoke();
            }
        }
    }

    public async Task<List<IssueReportDto>> GetActiveIssuesAsync()
    {
        try
        {
            var token = await authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
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
            var token = await authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
            var token = await authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
