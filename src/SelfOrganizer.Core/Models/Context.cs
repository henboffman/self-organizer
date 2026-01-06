namespace SelfOrganizer.Core.Models;

/// <summary>
/// Context definition - where/how tasks can be done
/// </summary>
public class Context : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Examples: think, do, email, read, call, errand
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? LastUsedAt { get; set; } // For MRU sorting
    public int UsageCount { get; set; } // Track how often this context is used
    public bool IsBuiltIn { get; set; } // Built-in contexts cannot be deleted
}
