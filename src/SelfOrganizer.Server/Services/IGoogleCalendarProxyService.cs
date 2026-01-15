namespace SelfOrganizer.Server.Services;

/// <summary>
/// Service for handling Google Calendar OAuth and API calls
/// </summary>
public interface IGoogleCalendarProxyService
{
    /// <summary>
    /// Generate OAuth authorization URL with PKCE
    /// </summary>
    GoogleAuthUrlResponse GenerateAuthUrl(string state);

    /// <summary>
    /// Exchange authorization code for tokens
    /// </summary>
    Task<GoogleTokenResponse> ExchangeCodeAsync(string code, string codeVerifier);

    /// <summary>
    /// Refresh the access token using the refresh token
    /// </summary>
    Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Get list of user's calendars
    /// </summary>
    Task<GoogleCalendarListResponse> GetCalendarsAsync(string accessToken);

    /// <summary>
    /// Get calendar events within a date range
    /// </summary>
    Task<GoogleEventsResponse> GetEventsAsync(string accessToken, string calendarId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Check if Google Calendar is configured
    /// </summary>
    bool IsConfigured();
}

public class GoogleAuthUrlResponse
{
    public bool Success { get; set; }
    public string AuthUrl { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class GoogleTokenResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? TokenType { get; set; }
    public string? Scope { get; set; }
    public string? Error { get; set; }
}

public class GoogleCalendarListResponse
{
    public bool Success { get; set; }
    public List<GoogleCalendarInfo> Calendars { get; set; } = new();
    public string? Error { get; set; }
}

public class GoogleCalendarInfo
{
    public string Id { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BackgroundColor { get; set; }
    public bool Primary { get; set; }
    public string? AccessRole { get; set; }
}

public class GoogleEventsResponse
{
    public bool Success { get; set; }
    public List<GoogleEventInfo> Events { get; set; } = new();
    public string? NextPageToken { get; set; }
    public string? Error { get; set; }
}

public class GoogleEventInfo
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
    public string? RecurringEventId { get; set; }
}
