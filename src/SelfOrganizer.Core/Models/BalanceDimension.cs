namespace SelfOrganizer.Core.Models;

/// <summary>
/// Represents a dimension of life/work balance that can be tracked and improved.
/// Different AppModes show different sets of dimensions.
/// </summary>
public class BalanceDimension
{
    /// <summary>Unique identifier for this dimension</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Career Growth", "Health")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Brief description of what this dimension encompasses</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Icon identifier for UI display</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Color for UI display (hex or CSS color name)</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Which app modes this dimension appears in</summary>
    public AppMode[] ApplicableModes { get; set; } = [];

    /// <summary>GoalCategory values that map to this dimension</summary>
    public string[] CategoryMappings { get; set; } = [];

    /// <summary>Keywords for auto-detecting this dimension from goal/task text</summary>
    public string[] KeywordMappings { get; set; } = [];

    /// <summary>Display order within the balance wheel</summary>
    public int SortOrder { get; set; }
}
