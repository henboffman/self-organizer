using System.ComponentModel.DataAnnotations;

namespace SelfOrganizer.Core.Models;

public class CareerPlan : BaseEntity
{
    [Required(ErrorMessage = "Career plan title is required")]
    [MinLength(1, ErrorMessage = "Title cannot be empty")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? CurrentRole { get; set; }
    public string? TargetRole { get; set; }

    public CareerPlanStatus Status { get; set; } = CareerPlanStatus.Draft;
    public DateTime? StartDate { get; set; }
    public DateTime? TargetDate { get; set; }

    public string? Icon { get; set; }
    public string? Color { get; set; }

    public List<CareerMilestone> Milestones { get; set; } = new();

    public List<Guid> LinkedGoalIds { get; set; } = new();
    public List<Guid> LinkedSkillIds { get; set; } = new();
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedHabitIds { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }

    public int ProgressPercent
    {
        get
        {
            if (Milestones.Count == 0) return 0;
            var completed = Milestones.Count(m => m.Status == MilestoneStatus.Completed);
            var relevant = Milestones.Count(m => m.Status != MilestoneStatus.Skipped);
            return relevant == 0 ? 0 : (int)Math.Round((double)completed / relevant * 100);
        }
    }
}

public class CareerMilestone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Milestone title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public MilestoneCategory Category { get; set; } = MilestoneCategory.Other;
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int SortOrder { get; set; }

    public string? Icon { get; set; }
    public string? Color { get; set; }

    public List<Guid> LinkedGoalIds { get; set; } = new();
    public List<Guid> LinkedSkillIds { get; set; } = new();
}
