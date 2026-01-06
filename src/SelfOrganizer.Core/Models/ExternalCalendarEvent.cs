namespace SelfOrganizer.Core.Models;

/// <summary>
/// Represents a calendar event imported from an external provider (Google, Outlook)
/// </summary>
public class ExternalCalendarEvent
{
    /// <summary>
    /// External provider's unique ID for this event
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Which calendar provider this event came from
    /// </summary>
    public CalendarProvider Source { get; set; }

    /// <summary>
    /// Event title/subject
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event description/body (may contain HTML for Outlook)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Event start time (local time)
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Event end time (local time)
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Start time zone identifier (e.g., "America/New_York")
    /// </summary>
    public string? StartTimeZone { get; set; }

    /// <summary>
    /// End time zone identifier
    /// </summary>
    public string? EndTimeZone { get; set; }

    /// <summary>
    /// Whether this is an all-day event
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Physical or virtual meeting location
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Online meeting URL (Zoom, Teams, Google Meet)
    /// </summary>
    public string? OnlineMeetingUrl { get; set; }

    /// <summary>
    /// Type of online meeting (zoom, teams, googleMeet, etc.)
    /// </summary>
    public string? OnlineMeetingType { get; set; }

    /// <summary>
    /// User's response status to this meeting
    /// </summary>
    public MeetingResponseStatus ResponseStatus { get; set; }

    /// <summary>
    /// Whether user is the organizer of this meeting
    /// </summary>
    public bool IsOrganizer { get; set; }

    /// <summary>
    /// Email of the meeting organizer
    /// </summary>
    public string? OrganizerEmail { get; set; }

    /// <summary>
    /// Name of the meeting organizer
    /// </summary>
    public string? OrganizerName { get; set; }

    /// <summary>
    /// List of attendee emails
    /// </summary>
    public List<ExternalAttendee> Attendees { get; set; } = new();

    /// <summary>
    /// Whether this is a recurring event
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// ID of the recurring event series (for linking instances)
    /// </summary>
    public string? RecurringEventId { get; set; }

    /// <summary>
    /// Recurrence rule in RRULE format
    /// </summary>
    public string? RecurrenceRule { get; set; }

    /// <summary>
    /// Event visibility/privacy (public, private, etc.)
    /// </summary>
    public EventVisibility Visibility { get; set; }

    /// <summary>
    /// User's show-as status (busy, free, tentative)
    /// </summary>
    public ShowAsStatus ShowAs { get; set; }

    /// <summary>
    /// Whether this event was cancelled
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Categories/labels assigned in the external calendar
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// When this event was created in the external system
    /// </summary>
    public DateTime? ExternalCreatedAt { get; set; }

    /// <summary>
    /// When this event was last modified in the external system
    /// </summary>
    public DateTime? ExternalModifiedAt { get; set; }

    /// <summary>
    /// Link to view this event in the external calendar
    /// </summary>
    public string? ExternalWebLink { get; set; }

    /// <summary>
    /// ICalendar UID if available
    /// </summary>
    public string? ICalUid { get; set; }

    /// <summary>
    /// Raw JSON response from the API (for debugging/extended data)
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Convert this external event to an internal CalendarEvent
    /// </summary>
    public CalendarEvent ToCalendarEvent()
    {
        return new CalendarEvent
        {
            Title = Title,
            Description = Description,
            StartTime = StartTime,
            EndTime = EndTime,
            Location = !string.IsNullOrEmpty(OnlineMeetingUrl) ? OnlineMeetingUrl : Location,
            IsAllDay = IsAllDay,
            ExternalId = ExternalId,
            Source = Source.ToString(),
            Attendees = Attendees.Select(a => a.Email).Where(e => !string.IsNullOrEmpty(e)).ToList()!,
            RequiresPrep = Attendees.Count > 3 || !IsOrganizer, // Suggest prep for larger meetings or meetings you didn't organize
            RequiresFollowUp = IsOrganizer && Attendees.Count > 1 // Organizers often need to follow up
        };
    }
}

/// <summary>
/// Represents an attendee from an external calendar
/// </summary>
public class ExternalAttendee
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public MeetingResponseStatus ResponseStatus { get; set; }
    public bool IsOptional { get; set; }
    public bool IsOrganizer { get; set; }
}

/// <summary>
/// Response status for a meeting invitation
/// </summary>
public enum MeetingResponseStatus
{
    None,
    NotResponded,
    Accepted,
    Declined,
    Tentative
}

/// <summary>
/// Event visibility in the external calendar
/// </summary>
public enum EventVisibility
{
    Default,
    Public,
    Private,
    Confidential
}

/// <summary>
/// Show-as status (busy/free) for calendar time
/// </summary>
public enum ShowAsStatus
{
    Free,
    Tentative,
    Busy,
    OutOfOffice,
    WorkingElsewhere
}
