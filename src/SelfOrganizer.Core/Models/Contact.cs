namespace SelfOrganizer.Core.Models;

/// <summary>
/// Contact - for waiting-for tracking
/// </summary>
public class Contact : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
}
