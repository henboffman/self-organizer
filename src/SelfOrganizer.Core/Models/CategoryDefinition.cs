namespace SelfOrganizer.Core.Models;

/// <summary>
/// Category with dictionary terms for auto-categorization
/// </summary>
public class CategoryDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public List<string> MatchTerms { get; set; } = new(); // Keywords to auto-detect
    public int DefaultPrepMinutes { get; set; }
    public int DefaultDecompressMinutes { get; set; }
    public int DefaultEnergyRequired { get; set; }
    public bool TypicallyRequiresFollowUp { get; set; }
}
