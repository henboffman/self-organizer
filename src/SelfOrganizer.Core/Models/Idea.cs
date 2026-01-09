namespace SelfOrganizer.Core.Models;

/// <summary>
/// An idea - a thought, concept, or inspiration that may or may not lead to a task.
/// Ideas are separate from tasks to allow capturing thoughts without the pressure of action.
/// </summary>
public class Idea : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public IdeaStatus Status { get; set; } = IdeaStatus.Active;

    /// <summary>
    /// Optional link to a goal this idea relates to
    /// </summary>
    public Guid? LinkedGoalId { get; set; }

    /// <summary>
    /// Optional link to a project this idea relates to
    /// </summary>
    public Guid? LinkedProjectId { get; set; }

    /// <summary>
    /// If this idea was converted to a task, the task ID
    /// </summary>
    public Guid? ConvertedToTaskId { get; set; }

    /// <summary>
    /// Source or inspiration for this idea
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Priority for sorting/filtering (1=High, 2=Normal, 3=Low)
    /// </summary>
    public int Priority { get; set; } = 2;

    /// <summary>
    /// Whether this idea has potential for action
    /// </summary>
    public bool HasActionPotential { get; set; } = false;

    /// <summary>
    /// Additional notes or context
    /// </summary>
    public string? Notes { get; set; }
}

public enum IdeaStatus
{
    Active,
    Archived,
    ConvertedToTask,
    Dismissed
}
