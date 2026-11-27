using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Coworking.Shared.Enums;
using Coworking.Shared.Models.DTOs;
using Coworking.Shared.Models.DTOs.Telemetry;
using CoworkingMobile.Services;
using Fonts;

namespace CoworkingMobile.ViewModels;

public partial class CleaningDashboardViewModel : ObservableObject
{
    private readonly IssueReportsService issueService;
    private readonly AuthService authService;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string connectionStatusText = "Conectando con servidor...";

    [ObservableProperty]
    private string connectionColor = "#D97706";

    [ObservableProperty]
    private string connectionBadgeBg = "#33D97706";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoIssues))]
    private int activeCount;

    [ObservableProperty]
    private int pendingCount;

    [ObservableProperty]
    private int inProgressCount;

    public bool HasNoIssues => ActiveCount == 0;

    public ObservableCollection<IssueItemViewModel> ActiveIssues { get; } = new();

    public CleaningDashboardViewModel(IssueReportsService issueService, AuthService authService)
    {
        this.issueService = issueService;
        this.authService = authService;

        this.issueService.OnReportStatusUpdated += HandleReportStatusUpdated;
        this.issueService.OnConnectionStatusChanged += UpdateConnectionStatus;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        await issueService.DisconnectSignalRAsync();
        authService.Logout();
        await Shell.Current.GoToAsync("//login");
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
            ConnectionStatusText = IsConnected ? "SignalR en vivo" : "Sin conexión";
            ConnectionColor = IsConnected ? "#14B8A6" : "#EF4444";
            ConnectionBadgeBg = IsConnected ? "#2614B8A6" : "#26EF4444";
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
                RecalculateCounts();
            });
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void RecalculateCounts()
    {
        ActiveCount = ActiveIssues.Count;
        PendingCount = ActiveIssues.Count(i => i.Status == ReportStatus.Generated);
        InProgressCount = ActiveIssues.Count(i => i.Status == ReportStatus.InProgress);
    }

    private void HandleReportStatusUpdated(ReportStatusUpdateTelemetry telemetry)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (telemetry.Status == ReportStatus.Resolved)
            {
                var existing = ActiveIssues.FirstOrDefault(i => i.Id == telemetry.ReportId);
                if (existing != null)
                {
                    ActiveIssues.Remove(existing);
                    RecalculateCounts();
                }
            }
            else
            {
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
            RecalculateCounts();
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
            RecalculateCounts();
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
    public short PositionX { get; }
    public short PositionY { get; }
    public DateTimeOffset GenerationDate { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeColor))]
    [NotifyPropertyChangedFor(nameof(StatusSoftBgColor))]
    [NotifyPropertyChangedFor(nameof(StatusIconGlyph))]
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
        PositionX = dto.PositionX;
        PositionY = dto.PositionY;
        GenerationDate = dto.GenerationDate;
        Status = dto.Status;
    }

    public string LocationText => $"{BuildingName}  •  Piso {FloorNumber}  •  {RestroomIdentifier}";

    public string CoordinatesText => $"Coord ({PositionX}, {PositionY})";

    public string AssetName => AssetType switch
    {
        AssetType.Toilet => "Inodoro",
        AssetType.Sink => "Lavamanos",
        AssetType.HandDryer => "Secador",
        AssetType.PaperDispenser => "Dispensador",
        _ => "Activo"
    };

    public string AssetIconGlyph => AssetType switch
    {
        AssetType.Toilet => FluentUI.wrench_24_regular,
        AssetType.Sink => FluentUI.drop_24_regular,
        AssetType.HandDryer => FluentUI.weather_blowing_snow_24_regular,
        AssetType.PaperDispenser => FluentUI.document_24_regular,
        _ => FluentUI.wrench_24_regular
    };

    public string ElapsedTimeText
    {
        get
        {
            var span = DateTimeOffset.UtcNow - GenerationDate;
            if (span.TotalMinutes < 1) return "Reportado hace un momento";
            if (span.TotalMinutes < 60) return $"Hace {(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"Hace {(int)span.TotalHours} h";
            return $"Hace {(int)span.TotalDays} d";
        }
    }

    public string StatusBadgeText => Status switch
    {
        ReportStatus.Generated => "PENDIENTE",
        ReportStatus.InProgress => "EN ATENCIÓN",
        ReportStatus.Resolved => "RESUELTO",
        _ => Status.ToString().ToUpperInvariant()
    };

    public string StatusBadgeColor => Status switch
    {
        ReportStatus.Generated => "#DC2626",
        ReportStatus.InProgress => "#D97706",
        ReportStatus.Resolved => "#0D9488",
        _ => "#64748B"
    };

    public string StatusSoftBgColor => Status switch
    {
        ReportStatus.Generated => "#FEF2F2",
        ReportStatus.InProgress => "#FFFBEB",
        ReportStatus.Resolved => "#F0FDFA",
        _ => "#F1F5F9"
    };

    public string StatusIconGlyph => Status switch
    {
        ReportStatus.Generated => FluentUI.warning_24_regular,
        ReportStatus.InProgress => FluentUI.clock_24_regular,
        ReportStatus.Resolved => FluentUI.checkmark_circle_24_regular,
        _ => FluentUI.alert_urgent_24_regular
    };

    public bool CanAttend => Status == ReportStatus.Generated;
}
