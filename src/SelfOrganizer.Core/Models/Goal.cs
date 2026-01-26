using System.ComponentModel.DataAnnotations;

namespace SelfOrganizer.Core.Models;

public class Goal : BaseEntity
{
    [Required(ErrorMessage = "Goal title is required")]
    [MinLength(1, ErrorMessage = "Goal title cannot be empty")]
    [MaxLength(200, ErrorMessage = "Goal title cannot exceed 200 characters")]
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
    [Range(1, 3, ErrorMessage = "Priority must be between 1 (High) and 3 (Low)")]
    public int Priority { get; set; } = 2;

    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100")]
    public int ProgressPercent { get; set; } = 0;
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<Guid> LinkedHabitIds { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? AiGeneratedPlan { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    // Balance Dimension Integration
    /// <summary>IDs of balance dimensions this goal contributes to</summary>
    public List<string> BalanceDimensionIds { get; set; } = new();

    /// <summary>Primary balance dimension (shown prominently in UI)</summary>
    public string? PrimaryBalanceDimensionId { get; set; }

    /// <summary>How much this goal impacts overall balance (1-5 scale)</summary>
    public int BalanceImpact { get; set; } = 3;

    /// <summary>
    /// Icon identifier (Open Iconic class name like "oi-target", or emoji)
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Base64 data URL for custom uploaded image (64x64)
    /// </summary>
    public string? IconImageUrl { get; set; }
}
