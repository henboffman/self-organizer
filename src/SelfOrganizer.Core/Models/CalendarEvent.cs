namespace SelfOrganizer.Core.Models;

/// <summary>
/// Calendar event (mock for now, real from Graph later)
/// </summary>
public class CalendarEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
    public string? ExternalId { get; set; } // For Graph sync later
    public string Source { get; set; } = "Manual"; // Manual, MicrosoftGraph
    public MeetingCategory? AutoCategory { get; set; }
    public MeetingCategory? OverrideCategory { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? DecompressTimeMinutes { get; set; }
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<string> Attendees { get; set; } = new();
    public bool RequiresPrep { get; set; }
    public bool RequiresFollowUp { get; set; }

    // Entity linking fields
    /// <summary>
    /// Project linked to this calendar event (for project-focused time tracking)
    /// </summary>
    public Guid? LinkedProjectId { get; set; }

    /// <summary>
    /// Goals linked to this calendar event
    /// </summary>
    public List<Guid> LinkedGoalIds { get; set; } = new();

    /// <summary>
    /// Ideas linked to this calendar event
    /// </summary>
    public List<Guid> LinkedIdeaIds { get; set; } = new();

    /// <summary>
    /// Whether the entity links were automatically determined by pattern matching
    /// </summary>
    public bool IsAutoLinked { get; set; }

    /// <summary>
    /// When the event was last analyzed for entity linking
    /// </summary>
    public DateTime? LastLinkAnalysisAt { get; set; }

    /// <summary>
    /// Tags extracted from description or manually added.
    /// Used for grouping, filtering, and smart scheduling.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    public MeetingCategory EffectiveCategory => OverrideCategory ?? AutoCategory ?? MeetingCategory.Other;
}
