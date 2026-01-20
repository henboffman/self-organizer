using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Provides cached access to user preferences for efficient querying
/// </summary>
public interface IUserPreferencesProvider
{
    /// <summary>
    /// Gets the current user preferences (cached for performance)
    /// </summary>
    Task<UserPreferences> GetPreferencesAsync();

    /// <summary>
    /// Checks if sample data should be shown based on user preferences
    /// </summary>
    Task<bool> ShowSampleDataAsync();

    /// <summary>
    /// Invalidates the cached preferences, forcing a reload on next access
    /// </summary>
    void InvalidateCache();
}
