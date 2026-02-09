namespace SelfOrganizer.Core.Models;

public class GrowthContextSummary
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public bool HasSnapshot { get; set; }
    public Guid? SnapshotId { get; set; }
    public string? SnapshotNotes { get; set; }

    // From snapshot (non-derivable)
    public List<SkillDataPoint> Skills { get; set; } = new();
    public List<GoalDataPoint> Goals { get; set; } = new();
    public Dictionary<string, int> BalanceRatings { get; set; } = new();

    // Derived from tasks
    public int TasksCompleted { get; set; }
    public Dictionary<string, int> TasksByContext { get; set; } = new();

    // Derived from habits
    public int HabitCompletionCount { get; set; }
    public double HabitCompletionRate { get; set; }

    // Derived from focus sessions
    public int FocusMinutes { get; set; }
    public int FocusSessionCount { get; set; }

    // Derived from weekly snapshots
    public double? AverageEnergy { get; set; }
    public double? AverageMood { get; set; }
    public double? AverageWorkLifeBalance { get; set; }

    // Derived from career milestones
    public int MilestonesCompletedInPeriod { get; set; }
    public List<string> MilestonesCompletedNames { get; set; } = new();

    // Active projects during this period
    public List<string> ActiveProjectNames { get; set; } = new();
}

public class SkillDataPoint
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = "";
    public int Proficiency { get; set; }
    public int? PreviousProficiency { get; set; }
    public SkillCategory Category { get; set; }
    public string? Color { get; set; }
}

public class GoalDataPoint
{
    public Guid GoalId { get; set; }
    public string Title { get; set; } = "";
    public int ProgressPercent { get; set; }
    public int? PreviousProgressPercent { get; set; }
    public GoalStatus Status { get; set; }
}
