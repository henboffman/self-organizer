namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for syncing with Microsoft Outlook calendars via Microsoft Graph API.
/// Requires Entra authentication with Calendar scopes.
/// </summary>
public interface IOutlookCalendarSyncService
{
    /// <summary>
    /// Gets the list of user's Outlook calendars.
    /// </summary>
    Task<List<OutlookCalendarDto>> GetCalendarsAsync();

    /// <summary>
    /// Syncs events from selected Outlook calendars.
    /// </summary>
    Task<CalendarSyncResultCore> SyncCalendarsAsync(IEnumerable<string> calendarIds, int pastDays, int futureDays);

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();

    /// <summary>
    /// Creates a new event in Outlook calendar.
    /// </summary>
    Task<OutlookEventResult> CreateEventAsync(string calendarId, OutlookEventDto eventDto);

    /// <summary>
    /// Updates an existing event in Outlook calendar.
    /// </summary>
    Task<OutlookEventResult> UpdateEventAsync(string calendarId, string eventId, OutlookEventDto eventDto);

    /// <summary>
    /// Deletes an event from Outlook calendar.
    /// </summary>
    Task<OutlookEventResult> DeleteEventAsync(string calendarId, string eventId);
}

/// <summary>
/// DTO for Outlook calendar information.
/// </summary>
public class OutlookCalendarDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsDefaultCalendar { get; set; }
    public bool CanEdit { get; set; }
    public string Owner { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

/// <summary>
/// DTO for Outlook event creation/update.
/// </summary>
public class OutlookEventDto
{
    public string Subject { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public string? TimeZone { get; set; }
    public List<string>? Attendees { get; set; }
    public OutlookEventRecurrence? Recurrence { get; set; }
    public bool IsOnlineMeeting { get; set; }
    public OutlookShowAs ShowAs { get; set; } = OutlookShowAs.Busy;
    public OutlookImportance Importance { get; set; } = OutlookImportance.Normal;
}

/// <summary>
/// Recurrence pattern for Outlook events.
/// </summary>
public class OutlookEventRecurrence
{
    public OutlookRecurrenceType Type { get; set; }
    public int Interval { get; set; } = 1;
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime? EndDate { get; set; }
    public int? NumberOfOccurrences { get; set; }
}

public enum OutlookRecurrenceType
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public enum OutlookShowAs
{
    Free,
    Tentative,
    Busy,
    Oof,
    WorkingElsewhere
}

public enum OutlookImportance
{
    Low,
    Normal,
    High
}

/// <summary>
/// Result of an Outlook event operation.
/// </summary>
public class OutlookEventResult
{
    public bool Success { get; set; }
    public string? EventId { get; set; }
    public string? WebLink { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of a calendar sync operation.
/// Shared by Google and Outlook calendar sync services.
/// </summary>
public class CalendarSyncResultCore
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int EventsSynced { get; set; }
    public int EventsCreated { get; set; }
    public int EventsUpdated { get; set; }
    public int EventsDeleted { get; set; }
}
