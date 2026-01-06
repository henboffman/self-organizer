namespace SelfOrganizer.Core.Interfaces;

using SelfOrganizer.Core.Models;

/// <summary>
/// Service for analyzing meeting patterns and providing insights/warnings
/// </summary>
public interface IMeetingInsightService
{
    /// <summary>
    /// Analyze meetings for a given date and return insights
    /// </summary>
    Task<DayMeetingAnalysis> AnalyzeDayAsync(DateOnly date);

    /// <summary>
    /// Get suggested prep time before a meeting based on its category and attendees
    /// </summary>
    int GetSuggestedPrepMinutes(CalendarEvent meeting);

    /// <summary>
    /// Get suggested decompress/followup time after a meeting
    /// </summary>
    int GetSuggestedDecompressMinutes(CalendarEvent meeting);

    /// <summary>
    /// Check if two events are back-to-back (no buffer between them)
    /// </summary>
    bool AreBackToBack(CalendarEvent first, CalendarEvent second, int bufferMinutes);
}
