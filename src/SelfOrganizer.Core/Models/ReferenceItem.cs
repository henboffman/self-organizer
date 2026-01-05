namespace SelfOrganizer.Core.Models;

/// <summary>
/// Reference item - non-actionable information to keep
/// </summary>
public class ReferenceItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Url { get; set; }
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedTaskIds { get; set; } = new();
}
