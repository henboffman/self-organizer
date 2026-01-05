namespace SelfOrganizer.Core.Models;

/// <summary>
/// Main task/action item following GTD methodology
/// </summary>
public class TodoTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public TodoTaskStatus Status { get; set; } = TodoTaskStatus.NextAction;
    public List<string> Contexts { get; set; } = new(); // @home, @work, @phone, etc.
    public string? Category { get; set; }
    public int? EnergyLevel { get; set; } // 1-5, required energy
    public int EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? WaitingForContactId { get; set; }
    public string? WaitingForNote { get; set; }
    public DateTime? WaitingForSince { get; set; }
    public int Priority { get; set; } = 2; // 1=High, 2=Normal, 3=Low
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<Guid> LinkedMeetingIds { get; set; } = new();
    public bool RequiresDeepWork { get; set; }
    public string? Notes { get; set; }
    public List<string> Tags { get; set; } = new();

    // Hierarchical task structure - subtasks
    /// <summary>
    /// Parent task ID if this is a subtask
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>
    /// IDs of subtasks belonging to this task
    /// </summary>
    public List<Guid> SubtaskIds { get; set; } = new();

    // Task dependencies
    /// <summary>
    /// IDs of tasks that must be completed before this task can be worked on
    /// </summary>
    public List<Guid> BlockedByTaskIds { get; set; } = new();

    // Recurring tasks
    /// <summary>
    /// Whether this task recurs on a schedule
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Recurrence pattern (e.g., Daily, Weekly, Monthly, Custom)
    /// </summary>
    public RecurrencePattern? RecurrencePattern { get; set; }

    /// <summary>
    /// Custom recurrence interval in days (used when RecurrencePattern is Custom)
    /// </summary>
    public int? RecurrenceIntervalDays { get; set; }

    /// <summary>
    /// The last date this recurring task was completed and regenerated
    /// </summary>
    public DateTime? LastRecurrenceDate { get; set; }

    // External references
    /// <summary>
    /// External links/URLs related to this task
    /// </summary>
    public List<string> Links { get; set; } = new();

    // Stakeholder
    /// <summary>
    /// Who this task is for (beneficiary/stakeholder)
    /// </summary>
    public string? WhoFor { get; set; }

    // Computed properties for optimizer
    /// <summary>
    /// Returns true if this task is blocked by incomplete tasks
    /// </summary>
    public bool IsBlocked => BlockedByTaskIds.Any();

    /// <summary>
    /// Returns true if this task has subtasks
    /// </summary>
    public bool HasSubtasks => SubtaskIds.Any();

    /// <summary>
    /// Returns true if this is a subtask of another task
    /// </summary>
    public bool IsSubtask => ParentTaskId.HasValue;
}
