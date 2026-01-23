namespace SelfOrganizer.Core.Models;

/// <summary>
/// Logs completed focus sessions with reflection data for tracking productivity patterns.
/// </summary>
public class FocusSessionLog : BaseEntity
{
    /// <summary>
    /// The task that was focused on, if any.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Title of the task or session (captured at time of session for historical accuracy).
    /// </summary>
    public string? TaskTitle { get; set; }

    /// <summary>
    /// Duration of the focus session in minutes.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the session ended.
    /// </summary>
    public DateTime EndedAt { get; set; }

    /// <summary>
    /// The user's stated intention for the session.
    /// </summary>
    public string? Intention { get; set; }

    /// <summary>
    /// Self-rated focus level (1-5 scale).
    /// </summary>
    public int FocusRating { get; set; }

    /// <summary>
    /// Whether the user reported being interrupted or distracted.
    /// </summary>
    public bool WasDistracted { get; set; }

    /// <summary>
    /// Optional notes about the session.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the task was completed during this session.
    /// </summary>
    public bool TaskCompleted { get; set; }

    /// <summary>
    /// Energy level at the time of the session (if captured).
    /// </summary>
    public int? EnergyLevel { get; set; }

    /// <summary>
    /// Project associated with the task, if any.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Context/location where the session was completed.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Actual elapsed time in seconds (may differ from planned duration due to extensions/early completion).
    /// </summary>
    public int ActualElapsedSeconds { get; set; }

    /// <summary>
    /// Original estimated time in minutes (from task or selected duration before extensions).
    /// </summary>
    public int OriginalEstimatedMinutes { get; set; }

    /// <summary>
    /// Number of times the timer was extended during this session.
    /// </summary>
    public int ExtensionCount { get; set; }

    /// <summary>
    /// Total minutes added via extensions.
    /// </summary>
    public int TotalExtensionMinutes { get; set; }
}
