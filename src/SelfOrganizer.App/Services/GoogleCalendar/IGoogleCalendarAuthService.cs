using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.GoogleCalendar;

/// <summary>
/// Client-side service for Google Calendar OAuth authentication
/// </summary>
public interface IGoogleCalendarAuthService
{
    /// <summary>
    /// Check if Google Calendar is configured on the server
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Start the OAuth flow - returns the authorization URL
    /// </summary>
    Task<GoogleAuthFlowResult> StartAuthFlowAsync();

    /// <summary>
    /// Complete the OAuth flow after user authorizes
    /// </summary>
    Task<GoogleAuthResult> CompleteAuthFlowAsync(string code);

    /// <summary>
    /// Get a valid access token (refreshes if needed)
    /// </summary>
    Task<string?> GetValidAccessTokenAsync();

    /// <summary>
    /// Check if the user is connected to Google Calendar
    /// </summary>
    Task<bool> IsConnectedAsync();

    /// <summary>
    /// Disconnect from Google Calendar
    /// </summary>
    Task DisconnectAsync();
}

public class GoogleAuthFlowResult
{
    public bool Success { get; set; }
    public string? AuthUrl { get; set; }
    public string? Error { get; set; }
}

public class GoogleAuthResult
{
    public bool Success { get; set; }
    public string? Email { get; set; }
    public string? Error { get; set; }
}

public class GoogleCalendarDto
{
    public string Id { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackgroundColor { get; set; }
    public bool Primary { get; set; }
    public bool Selected { get; set; }
}

public class GoogleEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool AllDay { get; set; }
    public string? Status { get; set; }
    public string? HtmlLink { get; set; }
    public List<string> Attendees { get; set; } = new();
    public string? Organizer { get; set; }
}

public class CalendarSyncResult
{
    public bool Success { get; set; }
    public int EventsSynced { get; set; }
    public int EventsCreated { get; set; }
    public int EventsUpdated { get; set; }
    public string? Error { get; set; }
}
