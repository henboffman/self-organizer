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
}
