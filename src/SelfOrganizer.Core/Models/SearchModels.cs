namespace SelfOrganizer.Core.Models;

public class SearchResult
{
    public string Type { get; set; } = string.Empty;  // "task", "project", "event", "goal", "capture"
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? MatchedField { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string? NavigationUrl { get; set; }
}

public class SearchResults
{
    public List<SearchResult> Results { get; set; } = new();
    public int TotalCount => Results.Count;
    public Dictionary<string, int> CountByType { get; set; } = new();
}

public class SearchOptions
{
    public string[]? FilterTypes { get; set; }
    public int MaxResults { get; set; } = 50;
}

public class QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Shortcut { get; set; }
    public string Icon { get; set; } = string.Empty;
    /// <summary>
    /// Category for grouping actions in command palette (e.g., "Navigation", "Actions", "Task Actions")
    /// </summary>
    public string? Category { get; set; }
}
