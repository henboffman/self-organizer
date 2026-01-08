using Microsoft.JSInterop;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Represents the current state of the focus timer
/// </summary>
public class FocusTimerStateData
{
    public Guid? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public bool IsRunning { get; set; }
    public bool IsBreak { get; set; }
    public int RemainingSeconds { get; set; }
    public int TotalSeconds { get; set; }
    public int FocusMinutes { get; set; } = 25;
    public int BreakMinutes { get; set; } = 5;
    public int SessionsCompleted { get; set; }
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Service interface for managing focus timer state across windows
/// </summary>
public interface IFocusTimerState
{
    /// <summary>
    /// Current timer state
    /// </summary>
    FocusTimerStateData State { get; }

    /// <summary>
    /// Event fired when timer state changes
    /// </summary>
    event Action? OnStateChanged;

    /// <summary>
    /// Start focusing on a specific task
    /// </summary>
    Task StartFocusAsync(TodoTask? task, int? durationMinutes = null);

    /// <summary>
    /// Start a free focus session without a task
    /// </summary>
    Task StartFreeFocusAsync(int durationMinutes = 25);

    /// <summary>
    /// Start the timer
    /// </summary>
    Task PlayAsync();

    /// <summary>
    /// Pause the timer
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Reset the timer
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Set focus duration in minutes
    /// </summary>
    Task SetDurationAsync(int minutes);

    /// <summary>
    /// Set break duration in minutes
    /// </summary>
    Task SetBreakDurationAsync(int minutes);

    /// <summary>
    /// Clear the current focus session
    /// </summary>
    Task ClearFocusAsync();

    /// <summary>
    /// Open the mini timer window
    /// </summary>
    Task OpenMiniWindowAsync();

    /// <summary>
    /// Close the mini timer window
    /// </summary>
    Task CloseMiniWindowAsync();

    /// <summary>
    /// Check if mini window is open
    /// </summary>
    bool IsMiniWindowOpen { get; }

    /// <summary>
    /// Initialize the service (call after JS runtime is available)
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Sync state from external source (e.g., another window via BroadcastChannel)
    /// </summary>
    void SyncState(FocusTimerStateData state);
}

/// <summary>
/// Singleton service that manages focus timer state and syncs across windows
/// </summary>
public class FocusTimerState : IFocusTimerState, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<FocusTimerState>? _dotNetRef;
    private System.Threading.Timer? _timer;
    private bool _initialized;
    private bool _miniWindowOpen;

    public FocusTimerStateData State { get; private set; } = new();
    public event Action? OnStateChanged;
    public bool IsMiniWindowOpen => _miniWindowOpen;

    public FocusTimerState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        State.RemainingSeconds = State.FocusMinutes * 60;
        State.TotalSeconds = State.RemainingSeconds;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("focusTimerInterop.initialize", _dotNetRef);
            _initialized = true;
        }
        catch
        {
            // JS interop not available yet
        }
    }

    public async Task StartFocusAsync(TodoTask? task, int? durationMinutes = null)
    {
        State.TaskId = task?.Id;
        State.TaskTitle = task?.Title ?? "Free Focus";

        var duration = durationMinutes ?? (task?.EstimatedMinutes > 0 && task.EstimatedMinutes <= 60
            ? task.EstimatedMinutes
            : State.FocusMinutes);

        State.FocusMinutes = duration;
        State.RemainingSeconds = duration * 60;
        State.TotalSeconds = State.RemainingSeconds;
        State.IsBreak = false;
        State.IsRunning = false;
        State.LastUpdated = DateTime.UtcNow;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public Task StartFreeFocusAsync(int durationMinutes = 25)
    {
        return StartFocusAsync(null, durationMinutes);
    }

    public async Task PlayAsync()
    {
        if (State.IsRunning) return;

        State.IsRunning = true;
        State.LastUpdated = DateTime.UtcNow;

        _timer?.Dispose();
        _timer = new System.Threading.Timer(TimerTick, null, 1000, 1000);

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task PauseAsync()
    {
        if (!State.IsRunning) return;

        State.IsRunning = false;
        State.LastUpdated = DateTime.UtcNow;

        _timer?.Dispose();
        _timer = null;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task ResetAsync()
    {
        await PauseAsync();

        State.IsBreak = false;
        State.RemainingSeconds = State.FocusMinutes * 60;
        State.TotalSeconds = State.RemainingSeconds;
        State.LastUpdated = DateTime.UtcNow;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task SetDurationAsync(int minutes)
    {
        if (State.IsRunning) return;

        State.FocusMinutes = minutes;
        if (!State.IsBreak)
        {
            State.RemainingSeconds = minutes * 60;
            State.TotalSeconds = State.RemainingSeconds;
        }
        State.LastUpdated = DateTime.UtcNow;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task SetBreakDurationAsync(int minutes)
    {
        State.BreakMinutes = minutes;
        if (State.IsBreak && !State.IsRunning)
        {
            State.RemainingSeconds = minutes * 60;
            State.TotalSeconds = State.RemainingSeconds;
        }
        State.LastUpdated = DateTime.UtcNow;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task ClearFocusAsync()
    {
        await PauseAsync();

        State.TaskId = null;
        State.TaskTitle = null;
        State.IsBreak = false;
        State.RemainingSeconds = State.FocusMinutes * 60;
        State.TotalSeconds = State.RemainingSeconds;
        State.LastUpdated = DateTime.UtcNow;

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    public async Task OpenMiniWindowAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("focusTimerInterop.openMiniWindow");
            _miniWindowOpen = true;
        }
        catch
        {
            // Mini window opening failed
        }
    }

    public async Task CloseMiniWindowAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("focusTimerInterop.closeMiniWindow");
            _miniWindowOpen = false;
        }
        catch
        {
            // Mini window close failed
        }
    }

    /// <summary>
    /// Called from JavaScript when receiving state from another window
    /// </summary>
    [JSInvokable]
    public void OnStateReceived(FocusTimerStateData newState)
    {
        SyncState(newState);
    }

    /// <summary>
    /// Called from JavaScript when mini window is opened/closed
    /// </summary>
    [JSInvokable]
    public void OnMiniWindowStateChanged(bool isOpen)
    {
        _miniWindowOpen = isOpen;
        NotifyStateChanged();
    }

    public void SyncState(FocusTimerStateData newState)
    {
        // Only sync if incoming state is newer
        if (newState.LastUpdated.HasValue &&
            (!State.LastUpdated.HasValue || newState.LastUpdated > State.LastUpdated))
        {
            var wasRunning = State.IsRunning;

            State.TaskId = newState.TaskId;
            State.TaskTitle = newState.TaskTitle;
            State.IsRunning = newState.IsRunning;
            State.IsBreak = newState.IsBreak;
            State.RemainingSeconds = newState.RemainingSeconds;
            State.TotalSeconds = newState.TotalSeconds;
            State.FocusMinutes = newState.FocusMinutes;
            State.BreakMinutes = newState.BreakMinutes;
            State.SessionsCompleted = newState.SessionsCompleted;
            State.LastUpdated = newState.LastUpdated;

            // Handle timer state changes
            if (newState.IsRunning && !wasRunning)
            {
                _timer?.Dispose();
                _timer = new System.Threading.Timer(TimerTick, null, 1000, 1000);
            }
            else if (!newState.IsRunning && wasRunning)
            {
                _timer?.Dispose();
                _timer = null;
            }

            NotifyStateChanged();
        }
    }

    private async void TimerTick(object? state)
    {
        State.RemainingSeconds--;
        State.LastUpdated = DateTime.UtcNow;

        if (State.RemainingSeconds <= 0)
        {
            _timer?.Dispose();
            _timer = null;
            State.IsRunning = false;

            if (!State.IsBreak)
            {
                // Focus session complete
                State.SessionsCompleted++;
                State.IsBreak = true;
                State.RemainingSeconds = State.BreakMinutes * 60;
                State.TotalSeconds = State.RemainingSeconds;

                // Play notification sound
                try
                {
                    await _jsRuntime.InvokeVoidAsync("focusTimerInterop.playNotification", "Focus session complete! Time for a break.");
                }
                catch { }
            }
            else
            {
                // Break complete
                State.IsBreak = false;
                State.RemainingSeconds = State.FocusMinutes * 60;
                State.TotalSeconds = State.RemainingSeconds;

                try
                {
                    await _jsRuntime.InvokeVoidAsync("focusTimerInterop.playNotification", "Break over! Ready for another session?");
                }
                catch { }
            }
        }

        await BroadcastStateAsync();
        NotifyStateChanged();
    }

    private async Task BroadcastStateAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("focusTimerInterop.broadcastState", State);
        }
        catch
        {
            // JS not available
        }
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _dotNetRef?.Dispose();

        try
        {
            await _jsRuntime.InvokeVoidAsync("focusTimerInterop.dispose");
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}
