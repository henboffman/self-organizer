using Microsoft.AspNetCore.Components;

namespace SelfOrganizer.App.Components.Shared;

public partial class ContextBadge
{
    [Parameter]
    public string Context { get; set; } = string.Empty;

    [Parameter]
    public string? Color { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public bool ShowRemove { get; set; } = false;

    [Parameter]
    public EventCallback OnRemove { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    // Icon mappings for common contexts
    private static readonly Dictionary<string, string> ContextIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        // Work contexts
        { "deep-work", "🎯" },
        { "meetings", "👥" },
        { "1-on-1", "👤" },
        { "planning", "📅" },
        { "admin", "📁" },
        { "review", "✓" },
        { "email", "📧" },
        { "calls", "📞" },
        { "call", "📞" },
        { "collaborate", "🤝" },
        { "research", "🔍" },
        // Life contexts
        { "home", "🏠" },
        { "errands", "🚗" },
        { "errand", "🚗" },
        { "computer", "💻" },
        { "phone", "📱" },
        { "read", "📚" },
        { "think", "💡" },
        { "weekend", "☀️" },
        { "evening", "🌙" },
        { "outdoors", "🌳" },
        { "anywhere", "🌍" },
        // Generic
        { "work", "💼" },
        { "do", "🔧" },
    };

    // Descriptions for common contexts
    private static readonly Dictionary<string, string> ContextDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "deep-work", "Focused, uninterrupted work sessions" },
        { "meetings", "Group meetings and team discussions" },
        { "1-on-1", "One-on-one meetings and conversations" },
        { "planning", "Strategy and planning activities" },
        { "admin", "Administrative tasks" },
        { "review", "Review and approval tasks" },
        { "email", "Email-based tasks and communications" },
        { "calls", "Phone calls and video calls" },
        { "call", "Phone calls and video calls" },
        { "collaborate", "Collaborative work with others" },
        { "research", "Research and learning" },
        { "home", "Tasks to do at home" },
        { "errands", "Tasks requiring travel or errands" },
        { "errand", "Tasks requiring travel or errands" },
        { "computer", "Computer-based work" },
        { "phone", "Mobile phone tasks" },
        { "read", "Reading and studying" },
        { "think", "Thinking and brainstorming" },
        { "weekend", "Weekend activities" },
        { "evening", "Evening tasks" },
        { "outdoors", "Outdoor activities" },
        { "anywhere", "Location-independent tasks" },
        { "work", "Work-related tasks" },
        { "do", "Physical or hands-on tasks" },
    };

    private string GetStyleAttribute()
    {
        // Use CSS custom property for dynamic color
        return Color != null
            ? $"--badge-bg: {Color}; background-color: var(--badge-bg);"
            : "";
    }

    private string GetIconDisplay()
    {
        // Use provided icon first
        if (!string.IsNullOrEmpty(Icon))
            return Icon;

        // Fall back to default mapping
        if (ContextIcons.TryGetValue(Context, out var emoji))
            return emoji;

        return "";
    }

    private string GetTooltip()
    {
        if (ContextDescriptions.TryGetValue(Context, out var description))
            return $"@{Context}: {description}";
        return $"@{Context}";
    }

    private async Task HandleClick()
    {
        if (ShowRemove && OnRemove.HasDelegate)
        {
            await OnRemove.InvokeAsync();
        }
        else if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
        }
    }
}
