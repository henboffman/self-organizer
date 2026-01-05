namespace SelfOrganizer.Core.Models;

/// <summary>
/// Context definition - where/how tasks can be done
/// </summary>
public class Context : BaseEntity
{
    public string Name { get; set; } = string.Empty; // @home, @work, @phone, @computer, @errands
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
