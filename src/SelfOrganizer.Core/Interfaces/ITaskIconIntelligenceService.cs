using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Result of task icon analysis
/// </summary>
public record TaskIconAnalysisResult(
    TaskIconCategory Category,
    string Icon,
    double Confidence,
    string[] MatchedKeywords
);

/// <summary>
/// Service for intelligently detecting task icons based on content
/// </summary>
public interface ITaskIconIntelligenceService
{
    /// <summary>
    /// Analyzes a task and returns the detected category and icon
    /// </summary>
    /// <param name="title">Task title</param>
    /// <param name="description">Optional task description</param>
    /// <param name="contexts">Task contexts (@home, @work, etc.)</param>
    /// <returns>Analysis result with category, icon, and confidence</returns>
    TaskIconAnalysisResult AnalyzeTask(string title, string? description, List<string>? contexts);

    /// <summary>
    /// Gets the icon for a specific category
    /// </summary>
    string GetIconForCategory(TaskIconCategory category);

    /// <summary>
    /// Gets all category to icon mappings
    /// </summary>
    Dictionary<TaskIconCategory, string> GetCategoryIconMappings();
}
