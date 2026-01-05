using Microsoft.AspNetCore.Components;

namespace SelfOrganizer.App.Components.Shared;

public partial class CategoryBadge
{
    [Parameter]
    public string? Category { get; set; }

    [Parameter]
    public string? Color { get; set; }

    private string GetStyleAttribute()
    {
        // Use CSS custom property for dynamic color
        var color = Color ?? "var(--text-muted)";
        return $"--badge-bg: {color}; background-color: var(--badge-bg);";
    }
}
