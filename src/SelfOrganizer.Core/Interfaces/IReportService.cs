namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for generating productivity reports and statistics
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets the number of tasks completed per week for the specified number of weeks
    /// </summary>
    /// <param name="weeks">Number of weeks to include (default: 8)</param>
    /// <returns>Dictionary with week start date as key and completion count as value</returns>
    Task<Dictionary<DateOnly, int>> GetTasksCompletedPerWeekAsync(int weeks = 8);

    /// <summary>
    /// Gets the time breakdown between meetings and deep work for the specified period
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 30)</param>
    /// <returns>Tuple containing total meeting minutes and deep work minutes</returns>
    Task<(int MeetingMinutes, int DeepWorkMinutes)> GetMeetingVsDeepWorkTimeAsync(int days = 30);

    /// <summary>
    /// Gets time allocation by category for the specified period
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 30)</param>
    /// <returns>Dictionary with category name as key and minutes as value</returns>
    Task<Dictionary<string, int>> GetCategoryTimeBreakdownAsync(int days = 30);

    /// <summary>
    /// Gets productivity trend data including tasks completed, hours worked, etc.
    /// </summary>
    /// <param name="days">Number of days to include (default: 30)</param>
    /// <returns>List of daily productivity metrics</returns>
    Task<IEnumerable<DailyProductivityMetrics>> GetProductivityTrendsAsync(int days = 30);

    /// <summary>
    /// Calculates the current inbox zero streak (consecutive days with inbox cleared)
    /// </summary>
    /// <returns>Number of consecutive days the inbox was cleared to zero</returns>
    Task<int> GetInboxZeroStreakAsync();

    /// <summary>
    /// Gets a summary of productivity statistics
    /// </summary>
    /// <returns>Summary statistics object</returns>
    Task<ProductivitySummary> GetProductivitySummaryAsync();
}

/// <summary>
/// Daily productivity metrics for trend analysis
/// </summary>
public class DailyProductivityMetrics
{
    public DateOnly Date { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksCreated { get; set; }
    public int TotalMinutesWorked { get; set; }
    public int MeetingMinutes { get; set; }
    public int DeepWorkMinutes { get; set; }
    public int CapturesProcessed { get; set; }
    public bool InboxCleared { get; set; }
}

/// <summary>
/// Summary of productivity statistics
/// </summary>
public class ProductivitySummary
{
    public int TotalTasksCompleted { get; set; }
    public int TasksCompletedThisWeek { get; set; }
    public int TasksCompletedLastWeek { get; set; }
    public double AverageTasksPerDay { get; set; }
    public int TotalMeetingMinutes { get; set; }
    public int TotalDeepWorkMinutes { get; set; }
    public double MeetingToDeepWorkRatio { get; set; }
    public int InboxZeroStreak { get; set; }
    public int LongestInboxZeroStreak { get; set; }
    public int CurrentWeekProductivityScore { get; set; }
    public int PreviousWeekProductivityScore { get; set; }
}
