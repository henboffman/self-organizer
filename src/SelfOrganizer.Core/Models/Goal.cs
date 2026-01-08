namespace SelfOrganizer.Core.Models;

public class Goal : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DesiredOutcome { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? Obstacles { get; set; }
    public string? Resources { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public GoalCategory Category { get; set; } = GoalCategory.Personal;
    public GoalTimeframe Timeframe { get; set; } = GoalTimeframe.Quarter;
    public DateTime? TargetDate { get; set; }
    public DateTime? StartDate { get; set; }
    public int Priority { get; set; } = 2;
    public int ProgressPercent { get; set; } = 0;
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? AiGeneratedPlan { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
