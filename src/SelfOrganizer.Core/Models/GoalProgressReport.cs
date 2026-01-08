namespace SelfOrganizer.Core.Models;

/// <summary>
/// Report on goal progress for scheduling and dashboard display
/// </summary>
public class GoalProgressReport
{
    public Goal Goal { get; set; } = default!;
    public double ProgressPercent { get; set; }
    public int DaysRemaining { get; set; }
    public double RequiredDailyProgress { get; set; }
    public bool IsOnTrack { get; set; }
    public string? Recommendation { get; set; }
    public List<TodoTask> OverdueTasks { get; set; } = new();
    public List<TodoTask> UpcomingTasks { get; set; } = new();
    public List<TodoTask> AllLinkedTasks { get; set; } = new();
    public int TotalTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }

    /// <summary>
    /// Calculate if the goal is at risk based on progress vs time remaining
    /// </summary>
    public GoalRiskLevel RiskLevel
    {
        get
        {
            if (Goal.Status == GoalStatus.Completed)
                return GoalRiskLevel.None;

            if (DaysRemaining < 0)
                return GoalRiskLevel.Overdue;

            if (!Goal.TargetDate.HasValue)
                return GoalRiskLevel.None;

            // Calculate expected progress based on time elapsed
            var totalDays = Goal.StartDate.HasValue
                ? (Goal.TargetDate.Value - Goal.StartDate.Value).TotalDays
                : 90; // Default to quarter if no start date

            var daysElapsed = totalDays - DaysRemaining;
            var expectedProgress = (daysElapsed / totalDays) * 100;

            // Compare actual vs expected
            var progressDelta = ProgressPercent - expectedProgress;

            if (progressDelta >= 0)
                return GoalRiskLevel.None;
            if (progressDelta >= -10)
                return GoalRiskLevel.Low;
            if (progressDelta >= -25)
                return GoalRiskLevel.Medium;
            return GoalRiskLevel.High;
        }
    }
}

/// <summary>
/// Risk level for goal completion
/// </summary>
public enum GoalRiskLevel
{
    None,
    Low,
    Medium,
    High,
    Overdue
}
