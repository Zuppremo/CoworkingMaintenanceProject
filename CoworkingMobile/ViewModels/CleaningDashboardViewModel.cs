using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingMobile.Services;

namespace CoworkingMobile.ViewModels;

public partial class CleaningDashboardViewModel : ObservableObject
{
    private readonly IssueReportsService issueService;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatusText = "Conectando al servidor...";

    [ObservableProperty]
    private string connectionColor = "#FFA500"; // Naranja

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoIssues))]
    private int activeCount;

    public bool HasNoIssues => ActiveCount == 0;

    public ObservableCollection<IssueItemViewModel> ActiveIssues { get; } = new();

    public CleaningDashboardViewModel(IssueReportsService issueService)
    {
        this.issueService = issueService;

        this.issueService.OnReportStatusUpdated += HandleReportStatusUpdated;
        this.issueService.OnConnectionStatusChanged += UpdateConnectionStatus;
    }

    public async Task InitializeAsync()
    {
        await issueService.InitializeSignalRAsync();
        UpdateConnectionStatus();
        await LoadIssuesAsync();
    }

    private void UpdateConnectionStatus()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsConnected = issueService.IsConnected;
            ConnectionStatusText = IsConnected ? "🟢 En vivo (SignalR Conectado)" : "🔴 Sin conexión al servidor";
            ConnectionColor = IsConnected ? "#28A745" : "#DC3545";
        });
    }

    [RelayCommand]
    public async Task LoadIssuesAsync()
    {
        IsRefreshing = true;
        try
        {
            var issues = await issueService.GetActiveIssuesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ActiveIssues.Clear();
                foreach (var issue in issues)
                {
                    ActiveIssues.Add(new IssueItemViewModel(issue));
                }
                ActiveCount = ActiveIssues.Count;
            });
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void HandleReportStatusUpdated(ReportStatusUpdateTelemetry telemetry)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Si el estado es Resolved (2), removemos la tarjeta de la lista
            if (telemetry.Status == ReportStatus.Resolved)
            {
                var existing = ActiveIssues.FirstOrDefault(i => i.Id == telemetry.ReportId);
                if (existing != null)
                {
                    ActiveIssues.Remove(existing);
                    ActiveCount = ActiveIssues.Count;
                }
            }
            else
            {
                // Si es un nuevo reporte o cambió a InProgress, refrescamos la lista
                await LoadIssuesAsync();
            }
        });
    }

    [RelayCommand]
    public async Task AttendIssueAsync(IssueItemViewModel item)
    {
        if (item == null) return;
        var success = await issueService.AttendIssueAsync(item.Id);
        if (success)
        {
            item.Status = ReportStatus.InProgress;
        }
    }

    [RelayCommand]
    public async Task ResolveIssueAsync(IssueItemViewModel item)
    {
        if (item == null) return;
        var success = await issueService.ResolveIssueAsync(item.Id);
        if (success)
        {
            ActiveIssues.Remove(item);
            ActiveCount = ActiveIssues.Count;
        }
    }
}

public partial class IssueItemViewModel : ObservableObject
{
    public Guid Id { get; }
    public string IssueDescription { get; }
    public string BuildingName { get; }
    public int FloorNumber { get; }
    public string RestroomIdentifier { get; }
    public AssetType AssetType { get; }
    public DateTimeOffset GenerationDate { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeColor))]
    [NotifyPropertyChangedFor(nameof(CanAttend))]
    private ReportStatus status;

    public IssueItemViewModel(IssueReportDto dto)
    {
        Id = dto.Id;
        IssueDescription = dto.IssueDescription;
        BuildingName = dto.BuildingName;
        FloorNumber = dto.FloorNumber;
        RestroomIdentifier = dto.RestroomIdentifier;
        AssetType = dto.AssetType;
        GenerationDate = dto.GenerationDate;
        Status = dto.Status;
    }

    public string LocationText => $"🏢 {BuildingName} • Piso {FloorNumber} • {RestroomIdentifier}";

    public string AssetIconAndName => AssetType switch
    {
        AssetType.Toilet => "🚽 Inodoro",
        AssetType.Sink => "🚰 Lavamanos",
        AssetType.HandDryer => "💨 Secador",
        AssetType.PaperDispenser => "🧻 Dispensador",
        _ => "🔧 Activo"
    };

    public string ElapsedTimeText
    {
        get
        {
            var span = DateTimeOffset.UtcNow - GenerationDate;
            if (span.TotalMinutes < 1) return "Justo ahora";
            if (span.TotalMinutes < 60) return $"Hace {(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"Hace {(int)span.TotalHours} h";
            return $"Hace {(int)span.TotalDays} d";
        }
    }

    public string StatusBadgeText => Status switch
    {
        ReportStatus.Generated => "⚠️ PENDIENTE",
        ReportStatus.InProgress => "⏳ EN ATENCIÓN",
        ReportStatus.Resolved => "✅ RESUELTO",
        _ => Status.ToString()
    };

    public string StatusBadgeColor => Status switch
    {
        ReportStatus.Generated => "#DC3545", // Rojo
        ReportStatus.InProgress => "#FFC107", // Amarillo
        ReportStatus.Resolved => "#28A745", // Verde
        _ => "#6C757D"
    };

    public bool CanAttend => Status == ReportStatus.Generated;
}
