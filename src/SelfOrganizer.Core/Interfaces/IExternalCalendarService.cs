namespace SelfOrganizer.Core.Interfaces;

using SelfOrganizer.Core.Models;

/// <summary>
/// Service for importing calendar events from external providers
/// </summary>
public interface IExternalCalendarService
{
    /// <summary>
    /// Import events from a JSON/ICS file exported from an external calendar
    /// </summary>
    /// <param name="content">File content (JSON or ICS)</param>
    /// <param name="provider">Which calendar provider this came from</param>
    /// <returns>Parsed events</returns>
    Task<IEnumerable<ExternalCalendarEvent>> ImportFromFileAsync(string content, CalendarProvider provider);

    /// <summary>
    /// Parse events from Google Calendar JSON export
    /// </summary>
    Task<IEnumerable<ExternalCalendarEvent>> ParseGoogleCalendarExport(string jsonContent);

    /// <summary>
    /// Parse events from Outlook/Exchange JSON export
    /// </summary>
    Task<IEnumerable<ExternalCalendarEvent>> ParseOutlookExport(string jsonContent);

    /// <summary>
    /// Parse events from ICS (iCalendar) format
    /// </summary>
    Task<IEnumerable<ExternalCalendarEvent>> ParseIcsContent(string icsContent, CalendarProvider provider);

    /// <summary>
    /// Convert external events to internal CalendarEvents and save them
    /// </summary>
    /// <param name="externalEvents">Events to import</param>
    /// <param name="skipDuplicates">Whether to skip events that already exist</param>
    /// <returns>Import result with counts and any errors</returns>
    Task<CalendarImportResult> SaveImportedEventsAsync(
        IEnumerable<ExternalCalendarEvent> externalEvents,
        bool skipDuplicates = true);
}

/// <summary>
/// Result of a calendar import operation
/// </summary>
public class CalendarImportResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedDuplicates { get; set; }
    public int ErrorCount { get; set; }
    public int LinkedEventsCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<CalendarEvent> ImportedEvents { get; set; } = new();
}
