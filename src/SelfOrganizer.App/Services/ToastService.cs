namespace SelfOrganizer.App.Services;

/// <summary>
/// Service for displaying toast notifications to users
/// </summary>
public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnToastRequested;
    public event Action<Guid>? OnToastDismissed;

    public void ShowSuccess(string message, string? title = null, int durationMs = 4000)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Success,
            Title = title ?? "Success",
            Message = message,
            DurationMs = durationMs
        });
    }

    public void ShowError(string message, string? title = null, int durationMs = 6000)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Error,
            Title = title ?? "Error",
            Message = message,
            DurationMs = durationMs
        });
    }

    public void ShowWarning(string message, string? title = null, int durationMs = 5000)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Warning,
            Title = title ?? "Warning",
            Message = message,
            DurationMs = durationMs
        });
    }

    public void ShowInfo(string message, string? title = null, int durationMs = 4000)
    {
        Show(new ToastMessage
        {
            Type = ToastType.Info,
            Title = title ?? "Info",
            Message = message,
            DurationMs = durationMs
        });
    }

    public void Show(ToastMessage toast)
    {
        OnToastRequested?.Invoke(toast);
    }

    public void Dismiss(Guid toastId)
    {
        OnToastDismissed?.Invoke(toastId);
    }
}

public interface IToastService
{
    event Action<ToastMessage>? OnToastRequested;
    event Action<Guid>? OnToastDismissed;

    void ShowSuccess(string message, string? title = null, int durationMs = 4000);
    void ShowError(string message, string? title = null, int durationMs = 6000);
    void ShowWarning(string message, string? title = null, int durationMs = 5000);
    void ShowInfo(string message, string? title = null, int durationMs = 4000);
    void Show(ToastMessage toast);
    void Dismiss(Guid toastId);
}

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ToastType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int DurationMs { get; set; } = 4000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
