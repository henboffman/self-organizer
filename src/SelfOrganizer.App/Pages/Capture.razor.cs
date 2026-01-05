using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SelfOrganizer.App.Services;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Pages;

public partial class Capture
{
    [Inject]
    private ICaptureService CaptureService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IPlatformService PlatformService { get; set; } = default!;

    [Inject]
    private IDataChangeNotificationService DataChangeNotification { get; set; } = default!;

    private ElementReference _textareaRef;
    private string _captureText = string.Empty;
    private bool _isCapturing = false;
    private bool _showSuccess = false;
    private int _todayCaptureCount = 0;
    private List<CaptureItem> _recentCaptures = new();
    private string _modifierKey = "Ctrl";

    protected override async Task OnInitializedAsync()
    {
        await RefreshData();
        _modifierKey = await PlatformService.GetModifierKeySymbolAsync();
    }

    private async Task RefreshData()
    {
        try
        {
            _todayCaptureCount = await CaptureService.GetTodayCaptureCountAsync();

            // Get recent captures (last 5)
            var allCaptures = await CaptureService.GetUnprocessedAsync();
            _recentCaptures = allCaptures
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();
        }
        catch
        {
            _todayCaptureCount = 0;
            _recentCaptures = new();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Support Cmd+Enter on Mac and Ctrl+Enter on Windows/Linux
        if (e.Key == "Enter" && PlatformService.IsModifierKeyPressed(e.CtrlKey, e.MetaKey))
        {
            await CaptureItem();
        }
    }

    private async Task CaptureItem()
    {
        if (string.IsNullOrWhiteSpace(_captureText) || _isCapturing)
            return;

        _isCapturing = true;
        _showSuccess = false;

        try
        {
            await CaptureService.CaptureAsync(_captureText.Trim());
            _captureText = string.Empty;
            _showSuccess = true;
            DataChangeNotification.NotifyDataChanged();
            await RefreshData();

            // Hide success message after 2 seconds
            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                _showSuccess = false;
                InvokeAsync(StateHasChanged);
            });
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM d");
    }
}
