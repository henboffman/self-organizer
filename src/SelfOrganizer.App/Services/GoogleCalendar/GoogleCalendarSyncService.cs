using System.Net.Http.Json;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.GoogleCalendar;

public class GoogleCalendarSyncService : IGoogleCalendarSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IGoogleCalendarAuthService _authService;
    private readonly ICalendarService _calendarService;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    public GoogleCalendarSyncService(
        HttpClient httpClient,
        IGoogleCalendarAuthService authService,
        ICalendarService calendarService,
        IRepository<UserPreferences> preferencesRepository)
    {
        _httpClient = httpClient;
        _authService = authService;
        _calendarService = calendarService;
        _preferencesRepository = preferencesRepository;
    }

    public async Task<List<GoogleCalendarDto>> GetCalendarsAsync()
    {
        var accessToken = await _authService.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new List<GoogleCalendarDto>();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/google-calendar/calendars");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<GoogleCalendarDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleCalendarListResponse>();
            if (result == null || !result.Success)
            {
                return new List<GoogleCalendarDto>();
            }

            // Get selected calendar IDs from preferences
            var prefs = await GetPreferencesAsync();
            var selectedIds = prefs?.GoogleCalendarSelectedCalendarIds ?? new List<string>();

            return result.Calendars.Select(c => new GoogleCalendarDto
            {
                Id = c.Id,
                Summary = c.Summary,
                Description = c.Description,
                BackgroundColor = c.BackgroundColor,
                Primary = c.Primary,
                Selected = selectedIds.Contains(c.Id) || (selectedIds.Count == 0 && c.Primary)
            }).ToList();
        }
        catch
        {
            return new List<GoogleCalendarDto>();
        }
    }

    public async Task<CalendarSyncResult> SyncCalendarsAsync(IEnumerable<string> calendarIds, int pastDays, int futureDays)
    {
        var accessToken = await _authService.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new CalendarSyncResult
            {
                Success = false,
                Error = "Not authenticated. Please reconnect your Google Calendar."
            };
        }

        var calendarIdList = calendarIds.ToList();
        if (calendarIdList.Count == 0)
        {
            return new CalendarSyncResult
            {
                Success = false,
                Error = "No calendars selected"
            };
        }

        var startDate = DateTime.UtcNow.AddDays(-pastDays);
        var endDate = DateTime.UtcNow.AddDays(futureDays);

        var totalSynced = 0;
        var totalCreated = 0;
        var totalUpdated = 0;

        try
        {
            foreach (var calendarId in calendarIdList)
            {
                var eventsResult = await GetEventsAsync(accessToken, calendarId, startDate, endDate);
                if (!eventsResult.Success)
                {
                    continue;
                }

                foreach (var googleEvent in eventsResult.Events)
                {
                    var syncResult = await SyncEventAsync(googleEvent, calendarId);
                    totalSynced++;
                    if (syncResult.Created) totalCreated++;
                    if (syncResult.Updated) totalUpdated++;
                }
            }

            // Update preferences with selected calendars and sync time
            var prefs = await GetOrCreatePreferencesAsync();
            prefs.GoogleCalendarSelectedCalendarIds = calendarIdList;
            prefs.GoogleCalendarSyncPastDays = pastDays;
            prefs.GoogleCalendarSyncFutureDays = futureDays;
            prefs.GoogleCalendarLastSyncTime = DateTime.UtcNow;
            await _preferencesRepository.UpdateAsync(prefs);

            return new CalendarSyncResult
            {
                Success = true,
                EventsSynced = totalSynced,
                EventsCreated = totalCreated,
                EventsUpdated = totalUpdated
            };
        }
        catch (Exception ex)
        {
            return new CalendarSyncResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs?.GoogleCalendarLastSyncTime;
    }

    private async Task<GoogleEventsResponse> GetEventsAsync(string accessToken, string calendarId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var url = $"/api/google-calendar/events?" +
                $"calendarId={Uri.EscapeDataString(calendarId)}" +
                $"&startDate={startDate:yyyy-MM-ddTHH:mm:ssZ}" +
                $"&endDate={endDate:yyyy-MM-ddTHH:mm:ssZ}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new GoogleEventsResponse { Success = false };
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleEventsResponse>();
            return result ?? new GoogleEventsResponse { Success = false };
        }
        catch
        {
            return new GoogleEventsResponse { Success = false };
        }
    }

    private async Task<SyncEventResult> SyncEventAsync(GoogleEventServerInfo googleEvent, string calendarId)
    {
        // Check if event already exists by external ID
        var existingEvents = await _calendarService.GetEventsForRangeAsync(
            googleEvent.Start.AddMinutes(-1),
            googleEvent.End.AddMinutes(1));

        var externalEventId = $"google:{calendarId}:{googleEvent.Id}";
        var existing = existingEvents.FirstOrDefault(e => e.ExternalId == externalEventId);

        var calendarEvent = new CalendarEvent
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            Title = googleEvent.Summary ?? "(No title)",
            Description = googleEvent.Description,
            Location = googleEvent.Location,
            StartTime = googleEvent.Start,
            EndTime = googleEvent.End,
            IsAllDay = googleEvent.AllDay,
            ExternalId = externalEventId,
            Source = "GoogleCalendar",
            Attendees = googleEvent.Attendees ?? new List<string>()
        };

        // Determine meeting category from title
        calendarEvent.AutoCategory = DetermineMeetingCategory(googleEvent.Summary, googleEvent.Attendees?.Count ?? 0);

        if (existing == null)
        {
            await _calendarService.CreateAsync(calendarEvent);
            return new SyncEventResult { Created = true };
        }
        else
        {
            calendarEvent.Id = existing.Id;
            await _calendarService.UpdateAsync(calendarEvent);
            return new SyncEventResult { Updated = true };
        }
    }

    private static MeetingCategory DetermineMeetingCategory(string? title, int attendeeCount)
    {
        if (string.IsNullOrEmpty(title))
            return MeetingCategory.Other;

        var lowerTitle = title.ToLowerInvariant();

        if (lowerTitle.Contains("1:1") || lowerTitle.Contains("1-1") || lowerTitle.Contains("one on one") || lowerTitle.Contains("1-on-1"))
            return MeetingCategory.OneOnOne;

        if (lowerTitle.Contains("standup") || lowerTitle.Contains("stand-up") || lowerTitle.Contains("status"))
            return MeetingCategory.StatusUpdate;

        if (lowerTitle.Contains("interview"))
            return MeetingCategory.Interview;

        if (lowerTitle.Contains("training") || lowerTitle.Contains("workshop"))
            return MeetingCategory.Training;

        if (lowerTitle.Contains("brainstorm"))
            return MeetingCategory.BrainStorming;

        if (lowerTitle.Contains("review") || lowerTitle.Contains("retrospective"))
            return MeetingCategory.Review;

        if (lowerTitle.Contains("planning") || lowerTitle.Contains("sprint"))
            return MeetingCategory.Planning;

        if (lowerTitle.Contains("lunch") || lowerTitle.Contains("coffee") || lowerTitle.Contains("happy hour"))
            return MeetingCategory.Social;

        if (lowerTitle.Contains("focus") || lowerTitle.Contains("heads down") || lowerTitle.Contains("no meetings"))
            return MeetingCategory.Focus;

        if (lowerTitle.Contains("break") || lowerTitle.Contains("blocked"))
            return MeetingCategory.Break;

        if (attendeeCount <= 2)
            return MeetingCategory.OneOnOne;

        if (attendeeCount <= 5)
            return MeetingCategory.TeamMeeting;

        return MeetingCategory.Other;
    }

    private async Task<UserPreferences?> GetPreferencesAsync()
    {
        var prefs = await _preferencesRepository.GetAllAsync();
        return prefs.FirstOrDefault();
    }

    private async Task<UserPreferences> GetOrCreatePreferencesAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (prefs == null)
        {
            prefs = new UserPreferences();
            prefs = await _preferencesRepository.AddAsync(prefs);
        }
        return prefs;
    }

    private class SyncEventResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
    }

    // Server response DTOs
    private class GoogleCalendarListResponse
    {
        public bool Success { get; set; }
        public List<GoogleCalendarServerInfo> Calendars { get; set; } = new();
        public string? Error { get; set; }
    }

    private class GoogleCalendarServerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BackgroundColor { get; set; }
        public bool Primary { get; set; }
    }

    private class GoogleEventsResponse
    {
        public bool Success { get; set; }
        public List<GoogleEventServerInfo> Events { get; set; } = new();
        public string? Error { get; set; }
    }

    private class GoogleEventServerInfo
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
        public List<string>? Attendees { get; set; }
        public string? Organizer { get; set; }
    }
}
