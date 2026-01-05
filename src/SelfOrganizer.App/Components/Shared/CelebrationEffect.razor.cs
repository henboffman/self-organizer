using Microsoft.AspNetCore.Components;

namespace SelfOrganizer.App.Components.Shared;

public partial class CelebrationEffect
{
    [Parameter]
    public string Message { get; set; } = "Great job!";

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private bool _isVisible = false;
    private readonly string[] _colors = new[]
    {
        "var(--color-accent)",
        "var(--color-success)",
        "var(--color-info)",
        "var(--color-warning)",
        "#FFD700",  // Gold
        "#FF69B4",  // Hot pink
        "#00CED1",  // Dark turquoise
        "#9370DB"   // Medium purple
    };

    public async Task ShowAsync(string? customMessage = null)
    {
        if (!string.IsNullOrEmpty(customMessage))
            Message = customMessage;

        _isVisible = true;
        StateHasChanged();

        // Auto-hide after 3 seconds
        await Task.Delay(3000);
        await Hide();
    }

    private async Task Hide()
    {
        _isVisible = false;
        StateHasChanged();
        await OnDismiss.InvokeAsync();
    }
}
