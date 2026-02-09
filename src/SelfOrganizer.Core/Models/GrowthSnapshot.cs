namespace SelfOrganizer.Core.Models;

public class GrowthSnapshot : BaseEntity
{
    public DateOnly SnapshotDate { get; set; }
    public SnapshotGranularity Granularity { get; set; } = SnapshotGranularity.Monthly;
    public SnapshotTrigger Trigger { get; set; } = SnapshotTrigger.Manual;
    public string? Notes { get; set; }
    public Guid? CareerPlanId { get; set; }

    // Non-derivable state captured at this moment
    public Dictionary<Guid, int> SkillProficiencies { get; set; } = new();
    public Dictionary<Guid, string> SkillNames { get; set; } = new();
    public Dictionary<Guid, int> GoalProgress { get; set; } = new();
    public Dictionary<Guid, string> GoalTitles { get; set; } = new();
    public Dictionary<string, int> BalanceRatings { get; set; } = new();
    public AppMode? AppModeAtCapture { get; set; }
}
