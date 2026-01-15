using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for managing balance dimensions based on app mode
/// </summary>
public interface IBalanceDimensionService
{
    /// <summary>
    /// Get all balance dimensions applicable to the specified mode
    /// </summary>
    Task<IReadOnlyList<BalanceDimension>> GetDimensionsForModeAsync(AppMode mode);

    /// <summary>
    /// Get the current app mode from user preferences
    /// </summary>
    Task<AppMode> GetCurrentModeAsync();

    /// <summary>
    /// Set the app mode and optionally seed contexts for the new mode
    /// </summary>
    Task SetModeAsync(AppMode mode, bool seedContexts = true);

    /// <summary>
    /// Suggest balance dimensions for a goal based on its category and content
    /// </summary>
    Task<List<string>> SuggestDimensionsForGoalAsync(Goal goal);

    /// <summary>
    /// Get all goals linked to a specific balance dimension
    /// </summary>
    Task<IReadOnlyList<Goal>> GetGoalsForDimensionAsync(string dimensionId);

    /// <summary>
    /// Analyze a dimension to provide insights about goal coverage and balance
    /// </summary>
    Task<DimensionAnalysis> AnalyzeDimensionAsync(string dimensionId);

    /// <summary>
    /// Get the user's enabled dimensions (or all for mode if not customized)
    /// </summary>
    Task<IReadOnlyList<BalanceDimension>> GetEnabledDimensionsAsync();

    /// <summary>
    /// Get balance ratings for the current mode
    /// </summary>
    Task<Dictionary<string, int>> GetBalanceRatingsAsync();

    /// <summary>
    /// Save balance ratings for the current mode
    /// </summary>
    Task SaveBalanceRatingsAsync(Dictionary<string, int> ratings);
}

/// <summary>
/// Analysis results for a balance dimension
/// </summary>
public class DimensionAnalysis
{
    /// <summary>Dimension being analyzed</summary>
    public string DimensionId { get; set; } = string.Empty;

    /// <summary>Number of active goals linked to this dimension</summary>
    public int ActiveGoalCount { get; set; }

    /// <summary>Number of completed goals in the last 90 days</summary>
    public int RecentCompletedCount { get; set; }

    /// <summary>Average progress across active goals (0-100)</summary>
    public int AverageProgress { get; set; }

    /// <summary>Current rating from user (1-10)</summary>
    public int? CurrentRating { get; set; }

    /// <summary>Trend compared to last assessment (-1, 0, 1)</summary>
    public int Trend { get; set; }

    /// <summary>Whether this dimension needs attention</summary>
    public bool NeedsAttention { get; set; }

    /// <summary>Suggestion for improvement</summary>
    public string? Suggestion { get; set; }
}
