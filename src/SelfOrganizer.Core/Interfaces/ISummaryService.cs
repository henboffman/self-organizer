using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for generating summary reports over time periods
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Generate a comprehensive summary report for the specified time period
    /// </summary>
    Task<SummaryReport> GenerateSummaryAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Export the summary report to Markdown format
    /// </summary>
    string ExportToMarkdown(SummaryReport report);

    /// <summary>
    /// Export the summary report to HTML format (for PDF printing)
    /// </summary>
    string ExportToHtml(SummaryReport report);
}

/// <summary>
/// Comprehensive summary report containing all aggregated data
/// </summary>
public class SummaryReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Task Statistics
    public int TotalTasksCompleted { get; set; }
    public int TotalEstimatedMinutes { get; set; }
    public int TotalActualMinutes { get; set; }
    public List<CompletedTaskSummary> CompletedTasks { get; set; } = new();

    // Project Statistics
    public int ProjectsWorkedOn { get; set; }
    public List<ProjectSummary> Projects { get; set; } = new();

    // Time Allocation
    public Dictionary<string, int> TimeByContext { get; set; } = new();
    public Dictionary<string, int> TimeByProject { get; set; } = new();
    public Dictionary<string, int> TimeByCategory { get; set; } = new();

    // Capture Items / Ideas
    public int TotalCaptureItems { get; set; }
    public List<CaptureItemSummary> CaptureItems { get; set; } = new();

    // Goal Progress
    public List<GoalProgressSummary> GoalProgress { get; set; } = new();

    // Additional Insights
    public int DeepWorkTasksCompleted { get; set; }
    public int HighPriorityTasksCompleted { get; set; }
    public double AverageTaskCompletionRate { get; set; }
    public List<string> TopTags { get; set; } = new();
    public Dictionary<DayOfWeek, int> TasksCompletedByDayOfWeek { get; set; } = new();

    // Focus Sessions
    public int TotalFocusSessions { get; set; }
    public int TotalFocusMinutes { get; set; }
    public double AverageFocusRating { get; set; }
    public int FocusSessionsDistracted { get; set; }
    public List<FocusSessionSummary> FocusSessions { get; set; } = new();

    // Habits
    public int TotalHabitsTracked { get; set; }
    public int TotalHabitCompletions { get; set; }
    public double HabitCompletionRate { get; set; }
    public List<HabitSummary> HabitProgress { get; set; } = new();

    // Calendar Events
    public int TotalMeetings { get; set; }
    public int TotalMeetingMinutes { get; set; }
    public Dictionary<string, int> MeetingsByCategory { get; set; } = new();
}

public class CompletedTaskSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public DateTime CompletedAt { get; set; }
    public int EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public List<string> Contexts { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool RequiresDeepWork { get; set; }
    public int Priority { get; set; }
}

public class ProjectSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public ProjectStatus Status { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksRemaining { get; set; }
    public int TotalMinutesSpent { get; set; }
    public bool WasCompletedInPeriod { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CaptureItemSummary
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public bool IsProcessed { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class GoalProgressSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public GoalCategory Category { get; set; }
    public GoalStatus Status { get; set; }
    public int StartProgressPercent { get; set; }
    public int EndProgressPercent { get; set; }
    public int ProgressChange { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool WasCompletedInPeriod { get; set; }
    public int LinkedTasksCompleted { get; set; }
    public int LinkedProjectsWorkedOn { get; set; }

    /// <summary>
    /// Habits linked to this goal with their completion info
    /// </summary>
    public List<LinkedHabitInfo> LinkedHabits { get; set; } = new();
}

/// <summary>
/// Summary info for a habit linked to a goal
/// </summary>
public class LinkedHabitInfo
{
    public Guid HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CompletionsInPeriod { get; set; }
    public double CompletionRate { get; set; }
    public int CurrentStreak { get; set; }
    public bool IsAiSuggested { get; set; }
}

public class FocusSessionSummary
{
    public Guid Id { get; set; }
    public string? TaskTitle { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartedAt { get; set; }
    public int FocusRating { get; set; }
    public bool WasDistracted { get; set; }
    public bool TaskCompleted { get; set; }
    public string? Context { get; set; }
}

public class HabitSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public HabitFrequency Frequency { get; set; }
    public int TargetCount { get; set; }
    public int CompletionsInPeriod { get; set; }
    public int DaysTracked { get; set; }
    public double CompletionRate { get; set; }
    public int CurrentStreak { get; set; }
    public int ActiveDaysInPeriod { get; set; }
    public DateTime HabitStartDate { get; set; }

    /// <summary>
    /// Whether this habit was AI-suggested
    /// </summary>
    public bool IsAiSuggested { get; set; }

    /// <summary>
    /// Titles of goals this habit is linked to
    /// </summary>
    public List<string> LinkedGoalTitles { get; set; } = new();
}
