namespace SelfOrganizer.Core.Models;

/// <summary>
/// Quick capture item (inbox) - raw thoughts captured for later processing
/// </summary>
public class CaptureItem : BaseEntity
{
    public string RawText { get; set; } = string.Empty;
    public bool IsProcessed { get; set; } = false;
    public Guid? ProcessedIntoId { get; set; }
    public ProcessedItemType? ProcessedIntoType { get; set; }

    /// <summary>
    /// Tags extracted from the raw text (case-insensitive, without # prefix)
    /// </summary>
    public List<string> ExtractedTags { get; set; } = new();

    /// <summary>
    /// The cleaned text with tag syntax removed
    /// </summary>
    public string CleanedText { get; set; } = string.Empty;
}
