namespace SelfOrganizer.Core.Models;

/// <summary>
/// Represents a habit to track on a regular basis
/// </summary>
public class Habit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; } // CSS icon class or emoji
    public string? Color { get; set; } // Hex color for display

    /// <summary>
    /// How often should this habit be performed
    /// </summary>
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    /// <summary>
    /// Target number of completions per frequency period
    /// (e.g., 3 times per week)
    /// </summary>
    public int TargetCount { get; set; } = 1;

    /// <summary>
    /// Which days of the week this habit should be tracked (for weekly habits)
    /// </summary>
    public List<DayOfWeek> TrackedDays { get; set; } = new();

    /// <summary>
    /// Optional: Time of day to do this habit
    /// </summary>
    public TimeOnly? PreferredTime { get; set; }

    /// <summary>
    /// Category for grouping (e.g., Health, Productivity, Personal)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether the habit is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the habit was started
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: When the habit should end
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Goals this habit supports
    /// </summary>
    public List<Guid> LinkedGoalIds { get; set; } = new();

    /// <summary>
    /// Skills this habit helps develop (bidirectional with Skill.LinkedHabitIds)
    /// </summary>
    public List<Guid> LinkedSkillIds { get; set; } = new();

    /// <summary>
    /// If AI-generated, the rationale for why this habit helps
    /// </summary>
    public string? AiRationale { get; set; }

    /// <summary>
    /// Whether this habit was AI-suggested (vs manually created)
    /// </summary>
    public bool IsAiSuggested { get; set; } = false;
}

/// <summary>
/// Record of a habit completion
/// </summary>
public class HabitLog : BaseEntity
{
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public bool Completed { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum HabitFrequency
{
    Daily,
    Weekly,
    Weekdays,
    Weekends,
    Monthly,
    Custom
}
