using System.Net.Http.Json;
using System.Text.Json;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.OutlookCalendar;

/// <summary>
/// Service for syncing with Microsoft Outlook calendars via Microsoft Graph API.
/// Follows the Petri net and statechart models from PETRI-MODELS.md and STATECHARTS.md.
/// </summary>
public class OutlookCalendarSyncService : IOutlookCalendarSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IEntraAuthService _authService;
    private readonly ICalendarService _calendarService;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private static readonly string[] CalendarScopes = new[] { "Calendars.Read", "Calendars.ReadWrite" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OutlookCalendarSyncService(
        HttpClient httpClient,
        IEntraAuthService authService,
        ICalendarService calendarService,
        IRepository<UserPreferences> preferencesRepository)
    {
        _httpClient = httpClient;
        _authService = authService;
        _calendarService = calendarService;
        _preferencesRepository = preferencesRepository;
    }

    /// <summary>
    /// Gets the list of user's Outlook calendars.
    /// Petri net transition: P1 (Idle) → T1 (FetchCalendars) → P2 (CalendarsFetched)
    /// </summary>
    public async Task<List<OutlookCalendarDto>> GetCalendarsAsync()
    {
        var accessToken = await GetCalendarAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new List<OutlookCalendarDto>();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{GraphBaseUrl}/me/calendars");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<OutlookCalendarDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<GraphCalendarListResponse>(JsonOptions);
            if (result?.Value == null)
            {
                return new List<OutlookCalendarDto>();
            }

            // Get selected calendar IDs from preferences
            var prefs = await GetPreferencesAsync();
            var selectedIds = prefs?.OutlookCalendarSelectedIds ?? new List<string>();

            return result.Value.Select(c => new OutlookCalendarDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = MapGraphColor(c.Color),
                IsDefaultCalendar = c.IsDefaultCalendar,
                CanEdit = c.CanEdit,
                Owner = c.Owner?.Name ?? string.Empty,
                Selected = selectedIds.Contains(c.Id) || (selectedIds.Count == 0 && c.IsDefaultCalendar)
            }).ToList();
        }
        catch
        {
            return new List<OutlookCalendarDto>();
        }
    }

    /// <summary>
    /// Syncs events from selected Outlook calendars.
    /// Petri net transition: P2 (CalendarsFetched) → T2 (SyncEvents) → P3 (EventsSynced)
    /// </summary>
    public async Task<CalendarSyncResultCore> SyncCalendarsAsync(IEnumerable<string> calendarIds, int pastDays, int futureDays)
    {
        var accessToken = await GetCalendarAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new CalendarSyncResultCore
            {
                Success = false,
                Error = "Not authenticated. Please sign in with your Microsoft account."
            };
        }

        var calendarIdList = calendarIds.ToList();
        if (calendarIdList.Count == 0)
        {
            return new CalendarSyncResultCore
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
                var events = await GetEventsAsync(accessToken, calendarId, startDate, endDate);

                foreach (var outlookEvent in events)
                {
                    var syncResult = await SyncEventAsync(outlookEvent, calendarId);
                    totalSynced++;
                    if (syncResult.Created) totalCreated++;
                    if (syncResult.Updated) totalUpdated++;
                }
            }

            // Update preferences with selected calendars and sync time
            var prefs = await GetOrCreatePreferencesAsync();
            prefs.OutlookCalendarSelectedIds = calendarIdList;
            prefs.OutlookCalendarSyncPastDays = pastDays;
            prefs.OutlookCalendarSyncFutureDays = futureDays;
            prefs.OutlookCalendarLastSyncTime = DateTime.UtcNow;
            await _preferencesRepository.UpdateAsync(prefs);

            return new CalendarSyncResultCore
            {
                Success = true,
                EventsSynced = totalSynced,
                EventsCreated = totalCreated,
                EventsUpdated = totalUpdated
            };
        }
        catch (Exception ex)
        {
            return new CalendarSyncResultCore
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs?.OutlookCalendarLastSyncTime;
    }

    /// <summary>
    /// Creates a new event in Outlook calendar.
    /// </summary>
    public async Task<OutlookEventResult> CreateEventAsync(string calendarId, OutlookEventDto eventDto)
    {
        var accessToken = await GetCalendarAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = "Not authenticated."
            };
        }

        try
        {
            var graphEvent = MapToGraphEvent(eventDto);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphBaseUrl}/me/calendars/{calendarId}/events");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(graphEvent, options: JsonOptions);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new OutlookEventResult
                {
                    Success = false,
                    Error = $"Failed to create event: {error}"
                };
            }

            var createdEvent = await response.Content.ReadFromJsonAsync<GraphEvent>(JsonOptions);

            return new OutlookEventResult
            {
                Success = true,
                EventId = createdEvent?.Id,
                WebLink = createdEvent?.WebLink
            };
        }
        catch (Exception ex)
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Updates an existing event in Outlook calendar.
    /// </summary>
    public async Task<OutlookEventResult> UpdateEventAsync(string calendarId, string eventId, OutlookEventDto eventDto)
    {
        var accessToken = await GetCalendarAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = "Not authenticated."
            };
        }

        try
        {
            var graphEvent = MapToGraphEvent(eventDto);

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{GraphBaseUrl}/me/calendars/{calendarId}/events/{eventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(graphEvent, options: JsonOptions);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new OutlookEventResult
                {
                    Success = false,
                    Error = $"Failed to update event: {error}"
                };
            }

            var updatedEvent = await response.Content.ReadFromJsonAsync<GraphEvent>(JsonOptions);

            return new OutlookEventResult
            {
                Success = true,
                EventId = updatedEvent?.Id,
                WebLink = updatedEvent?.WebLink
            };
        }
        catch (Exception ex)
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Deletes an event from Outlook calendar.
    /// </summary>
    public async Task<OutlookEventResult> DeleteEventAsync(string calendarId, string eventId)
    {
        var accessToken = await GetCalendarAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = "Not authenticated."
            };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{GraphBaseUrl}/me/calendars/{calendarId}/events/{eventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new OutlookEventResult
                {
                    Success = false,
                    Error = $"Failed to delete event: {error}"
                };
            }

            return new OutlookEventResult
            {
                Success = true,
                EventId = eventId
            };
        }
        catch (Exception ex)
        {
            return new OutlookEventResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    #region Private Helper Methods

    private async Task<string?> GetCalendarAccessTokenAsync()
    {
        if (!_authService.IsEnabled || _authService.State != AuthState.Authenticated)
        {
            return null;
        }

        return await _authService.GetAccessTokenAsync(CalendarScopes);
    }

    private async Task<List<GraphEvent>> GetEventsAsync(string accessToken, string calendarId, DateTime startDate, DateTime endDate)
    {
        var events = new List<GraphEvent>();

        try
        {
            var url = $"{GraphBaseUrl}/me/calendars/{calendarId}/calendarView" +
                $"?startDateTime={startDate:yyyy-MM-ddTHH:mm:ssZ}" +
                $"&endDateTime={endDate:yyyy-MM-ddTHH:mm:ssZ}" +
                $"&$select=id,subject,body,start,end,location,attendees,organizer,isAllDay,showAs,importance,webLink,onlineMeeting,isCancelled" +
                $"&$orderby=start/dateTime" +
                $"&$top=250";

            while (!string.IsNullOrEmpty(url))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Prefer", "outlook.timezone=\"UTC\"");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                var result = await response.Content.ReadFromJsonAsync<GraphEventListResponse>(JsonOptions);
                if (result?.Value != null)
                {
                    events.AddRange(result.Value.Where(e => !e.IsCancelled));
                }

                url = result?.ODataNextLink;
            }
        }
        catch
        {
            // Return whatever we've collected so far
        }

        return events;
    }

    private async Task<SyncEventResult> SyncEventAsync(GraphEvent outlookEvent, string calendarId)
    {
        // Parse start and end times
        var startTime = ParseGraphDateTime(outlookEvent.Start);
        var endTime = ParseGraphDateTime(outlookEvent.End);

        // Check if event already exists by external ID
        var existingEvents = await _calendarService.GetEventsForRangeAsync(
            startTime.AddMinutes(-1),
            endTime.AddMinutes(1));

        var externalEventId = $"outlook:{calendarId}:{outlookEvent.Id}";
        var existing = existingEvents.FirstOrDefault(e => e.ExternalId == externalEventId);

        var calendarEvent = new CalendarEvent
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            Title = outlookEvent.Subject ?? "(No title)",
            Description = outlookEvent.Body?.Content,
            Location = outlookEvent.Location?.DisplayName,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = outlookEvent.IsAllDay,
            ExternalId = externalEventId,
            Source = "OutlookCalendar",
            Attendees = outlookEvent.Attendees?.Select(a => a.EmailAddress?.Address ?? string.Empty)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList() ?? new List<string>()
        };

        // Determine meeting category from subject
        calendarEvent.AutoCategory = DetermineMeetingCategory(
            outlookEvent.Subject,
            outlookEvent.Attendees?.Count ?? 0);

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

    private static DateTime ParseGraphDateTime(GraphDateTimeTimeZone? dateTime)
    {
        if (dateTime == null)
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateTime.DateTime, out var result))
        {
            // If timezone is UTC, return as-is
            if (dateTime.TimeZone == "UTC" || dateTime.TimeZone == "Etc/GMT")
            {
                return DateTime.SpecifyKind(result, DateTimeKind.Utc);
            }

            // Otherwise, try to convert
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(dateTime.TimeZone ?? "UTC");
                return TimeZoneInfo.ConvertTimeToUtc(result, tz);
            }
            catch
            {
                return DateTime.SpecifyKind(result, DateTimeKind.Utc);
            }
        }

        return DateTime.UtcNow;
    }

    private static object MapToGraphEvent(OutlookEventDto eventDto)
    {
        var timeZone = eventDto.TimeZone ?? "UTC";

        var graphEvent = new
        {
            subject = eventDto.Subject,
            body = string.IsNullOrEmpty(eventDto.Body) ? null : new
            {
                contentType = "text",
                content = eventDto.Body
            },
            start = new
            {
                dateTime = eventDto.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
                timeZone = timeZone
            },
            end = new
            {
                dateTime = eventDto.End.ToString("yyyy-MM-ddTHH:mm:ss"),
                timeZone = timeZone
            },
            location = string.IsNullOrEmpty(eventDto.Location) ? null : new
            {
                displayName = eventDto.Location
            },
            isAllDay = eventDto.IsAllDay,
            showAs = eventDto.ShowAs.ToString().ToLowerInvariant(),
            importance = eventDto.Importance.ToString().ToLowerInvariant(),
            isOnlineMeeting = eventDto.IsOnlineMeeting,
            onlineMeetingProvider = eventDto.IsOnlineMeeting ? "teamsForBusiness" : null,
            attendees = eventDto.Attendees?.Select(email => new
            {
                emailAddress = new { address = email },
                type = "required"
            }).ToList()
        };

        return graphEvent;
    }

    private static MeetingCategory DetermineMeetingCategory(string? subject, int attendeeCount)
    {
        if (string.IsNullOrEmpty(subject))
            return MeetingCategory.Other;

        var lowerSubject = subject.ToLowerInvariant();

        if (lowerSubject.Contains("1:1") || lowerSubject.Contains("1-1") || lowerSubject.Contains("one on one") || lowerSubject.Contains("1-on-1"))
            return MeetingCategory.OneOnOne;

        if (lowerSubject.Contains("standup") || lowerSubject.Contains("stand-up") || lowerSubject.Contains("status"))
            return MeetingCategory.StatusUpdate;

        if (lowerSubject.Contains("interview"))
            return MeetingCategory.Interview;

        if (lowerSubject.Contains("training") || lowerSubject.Contains("workshop"))
            return MeetingCategory.Training;

        if (lowerSubject.Contains("brainstorm"))
            return MeetingCategory.BrainStorming;

        if (lowerSubject.Contains("review") || lowerSubject.Contains("retrospective"))
            return MeetingCategory.Review;

        if (lowerSubject.Contains("planning") || lowerSubject.Contains("sprint"))
            return MeetingCategory.Planning;

        if (lowerSubject.Contains("lunch") || lowerSubject.Contains("coffee") || lowerSubject.Contains("happy hour"))
            return MeetingCategory.Social;

        if (lowerSubject.Contains("focus") || lowerSubject.Contains("heads down") || lowerSubject.Contains("no meetings"))
            return MeetingCategory.Focus;

        if (lowerSubject.Contains("break") || lowerSubject.Contains("blocked"))
            return MeetingCategory.Break;

        if (attendeeCount <= 2)
            return MeetingCategory.OneOnOne;

        if (attendeeCount <= 5)
            return MeetingCategory.TeamMeeting;

        return MeetingCategory.Other;
    }

    private static string? MapGraphColor(string? graphColor)
    {
        if (string.IsNullOrEmpty(graphColor))
            return null;

        // Map Graph calendar colors to hex
        return graphColor.ToLowerInvariant() switch
        {
            "auto" => null,
            "lightblue" => "#0078D4",
            "lightgreen" => "#107C10",
            "lightorange" => "#FF8C00",
            "lightgray" => "#A0A0A0",
            "lightyellow" => "#FFB900",
            "lightred" => "#D83B01",
            "maxcolor" => "#8764B8",
            _ => null
        };
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

    #endregion

    #region Helper Classes

    private class SyncEventResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
    }

    private class GraphCalendarListResponse
    {
        public List<GraphCalendar>? Value { get; set; }
    }

    private class GraphCalendar
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsDefaultCalendar { get; set; }
        public bool CanEdit { get; set; }
        public GraphEmailAddress? Owner { get; set; }
    }

    private class GraphEventListResponse
    {
        public List<GraphEvent>? Value { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@odata.nextLink")]
        public string? ODataNextLink { get; set; }
    }

    private class GraphEvent
    {
        public string Id { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public GraphItemBody? Body { get; set; }
        public GraphDateTimeTimeZone? Start { get; set; }
        public GraphDateTimeTimeZone? End { get; set; }
        public GraphLocation? Location { get; set; }
        public List<GraphAttendee>? Attendees { get; set; }
        public GraphEmailAddress? Organizer { get; set; }
        public bool IsAllDay { get; set; }
        public string? ShowAs { get; set; }
        public string? Importance { get; set; }
        public string? WebLink { get; set; }
        public GraphOnlineMeeting? OnlineMeeting { get; set; }
        public bool IsCancelled { get; set; }
    }

    private class GraphDateTimeTimeZone
    {
        public string DateTime { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
    }

    private class GraphItemBody
    {
        public string? ContentType { get; set; }
        public string? Content { get; set; }
    }

    private class GraphLocation
    {
        public string? DisplayName { get; set; }
    }

    private class GraphAttendee
    {
        public GraphEmailAddress? EmailAddress { get; set; }
        public string? Type { get; set; }
    }

    private class GraphEmailAddress
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
    }

    private class GraphOnlineMeeting
    {
        public string? JoinUrl { get; set; }
    }

    #endregion
}
