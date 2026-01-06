namespace SelfOrganizer.Core.Models;

/// <summary>
/// Project - collection of tasks toward an outcome
/// </summary>
public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DesiredOutcome { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string? Category { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Priority { get; set; } = 2;
    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }
    public string? Url { get; set; }
}
