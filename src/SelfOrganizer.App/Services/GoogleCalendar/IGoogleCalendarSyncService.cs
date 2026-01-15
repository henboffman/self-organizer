namespace SelfOrganizer.App.Services.GoogleCalendar;

/// <summary>
/// Service for syncing Google Calendar events
/// </summary>
public interface IGoogleCalendarSyncService
{
    /// <summary>
    /// Get list of user's calendars
    /// </summary>
    Task<List<GoogleCalendarDto>> GetCalendarsAsync();

    /// <summary>
    /// Sync events from selected calendars
    /// </summary>
    Task<CalendarSyncResult> SyncCalendarsAsync(IEnumerable<string> calendarIds, int pastDays, int futureDays);

    /// <summary>
    /// Get the last sync time
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();
}
