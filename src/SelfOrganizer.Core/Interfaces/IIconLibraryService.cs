namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Represents an icon in the library
/// </summary>
public record IconDefinition(string Id, string Name, string Category, bool IsEmoji = false);

/// <summary>
/// Service for managing predefined icons and emoji for projects/goals
/// </summary>
public interface IIconLibraryService
{
    /// <summary>
    /// Gets all available icons organized by category
    /// </summary>
    Dictionary<string, List<IconDefinition>> GetIconsByCategory();

    /// <summary>
    /// Gets all available emoji icons
    /// </summary>
    List<IconDefinition> GetEmojiIcons();

    /// <summary>
    /// Gets all project-specific icons
    /// </summary>
    List<IconDefinition> GetProjectIcons();

    /// <summary>
    /// Gets all goal-specific icons
    /// </summary>
    List<IconDefinition> GetGoalIcons();

    /// <summary>
    /// Gets a specific icon by ID
    /// </summary>
    IconDefinition? GetIconById(string id);

    /// <summary>
    /// Searches icons by name
    /// </summary>
    List<IconDefinition> SearchIcons(string query);
}
