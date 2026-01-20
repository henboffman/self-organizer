using System.Text.Json.Serialization;

namespace SelfOrganizer.Core.Models;

/// <summary>
/// AI-generated balance recommendation result containing suggested tasks, goals, and insights
/// </summary>
public class BalanceRecommendationResult
{
    [JsonPropertyName("overallAssessment")]
    public string? OverallAssessment { get; set; }

    [JsonPropertyName("suggestedTasks")]
    public List<SuggestedTask> SuggestedTasks { get; set; } = new();

    [JsonPropertyName("suggestedGoals")]
    public List<SuggestedGoal> SuggestedGoals { get; set; } = new();

    [JsonPropertyName("insights")]
    public List<BalanceInsight> Insights { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// AI-suggested task to improve a balance dimension
/// </summary>
public class SuggestedTask
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dimensionId")]
    public string DimensionId { get; set; } = string.Empty;

    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; } = 30;

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 2;

    [JsonPropertyName("suggestedContexts")]
    public List<string> SuggestedContexts { get; set; } = new();

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    /// <summary>
    /// Used by UI for checkbox selection
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; } = true;
}

/// <summary>
/// AI-suggested goal to address a neglected balance dimension
/// </summary>
public class SuggestedGoal
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("desiredOutcome")]
    public string? DesiredOutcome { get; set; }

    [JsonPropertyName("dimensionId")]
    public string DimensionId { get; set; } = string.Empty;

    [JsonPropertyName("suggestedTimeframe")]
    public string SuggestedTimeframe { get; set; } = "Quarter";

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    /// <summary>
    /// Used by UI for checkbox selection
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; } = true;
}

/// <summary>
/// AI-generated insight about balance patterns
/// </summary>
public class BalanceInsight
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Pattern, Strength, Concern, Opportunity

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("affectedDimensions")]
    public List<string> AffectedDimensions { get; set; } = new();
}

/// <summary>
/// Request model for generating balance recommendations
/// </summary>
public class BalanceRecommendationRequest
{
    public Dictionary<string, int> Ratings { get; set; } = new();
    public IReadOnlyList<BalanceDimension> Dimensions { get; set; } = Array.Empty<BalanceDimension>();
    public IReadOnlyList<Goal> ActiveGoals { get; set; } = Array.Empty<Goal>();
    public IReadOnlyList<TodoTask> ActiveTasks { get; set; } = Array.Empty<TodoTask>();
    public AppMode CurrentMode { get; set; } = AppMode.Balanced;

    // Preference settings
    public bool SuggestTasks { get; set; } = true;
    public bool SuggestGoals { get; set; } = true;
    public bool ShowInsights { get; set; } = true;
    public int Threshold { get; set; } = 5;
}
