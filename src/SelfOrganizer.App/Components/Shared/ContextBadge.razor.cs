using Microsoft.AspNetCore.Components;

namespace SelfOrganizer.App.Components.Shared;

public partial class ContextBadge
{
    [Parameter]
    public string Context { get; set; } = string.Empty;

    [Parameter]
    public string? Color { get; set; }

    private string GetStyleAttribute()
    {
        // Use CSS custom property for dynamic color
        return Color != null
            ? $"--badge-bg: {Color}; background-color: var(--badge-bg);"
            : "";
    }
}
