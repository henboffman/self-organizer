namespace SelfOrganizer.App.Services;

/// <summary>
/// Service that broadcasts data change notifications so UI components can refresh.
/// Includes debouncing to prevent rapid-fire notifications from causing performance issues.
/// </summary>
public interface IDataChangeNotificationService
{
    /// <summary>
    /// Event fired when any data changes that might affect counts/displays
    /// </summary>
    event Action? OnDataChanged;

    /// <summary>
    /// Notify all subscribers that data has changed.
    /// Notifications are debounced - rapid calls within 50ms will be batched into a single notification.
    /// </summary>
    void NotifyDataChanged();
}

public class DataChangeNotificationService : IDataChangeNotificationService, IDisposable
{
    private const int DebounceDelayMs = 50;

    public event Action? OnDataChanged;

    private CancellationTokenSource? _debounceToken;
    private readonly object _lock = new();
    private bool _hasPendingNotification;

    public void NotifyDataChanged()
    {
        lock (_lock)
        {
            _hasPendingNotification = true;
            _debounceToken?.Cancel();
            _debounceToken = new CancellationTokenSource();
        }

        // Fire and forget the debounced notification
        _ = DebounceNotifyAsync(_debounceToken.Token);
    }

    private async Task DebounceNotifyAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(DebounceDelayMs, token);

            lock (_lock)
            {
                if (!_hasPendingNotification) return;
                _hasPendingNotification = false;
            }

            OnDataChanged?.Invoke();
        }
        catch (TaskCanceledException)
        {
            // Expected when debounce is reset by another call
        }
    }

    public void Dispose()
    {
        _debounceToken?.Cancel();
        _debounceToken?.Dispose();
    }
}
