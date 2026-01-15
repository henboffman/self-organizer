using System.Text.Json;
using System.Text.RegularExpressions;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.App.Services.Intelligence;

namespace SelfOrganizer.App.Services.Domain;

public class ExternalCalendarService : IExternalCalendarService
{
    private readonly IRepository<CalendarEvent> _calendarRepository;
    private readonly IEntityLinkingService _entityLinkingService;

    public ExternalCalendarService(
        IRepository<CalendarEvent> calendarRepository,
        IEntityLinkingService entityLinkingService)
    {
        _calendarRepository = calendarRepository;
        _entityLinkingService = entityLinkingService;
    }

    public async Task<IEnumerable<ExternalCalendarEvent>> ImportFromFileAsync(string content, CalendarProvider provider)
    {
        // Detect format and parse accordingly
        content = content.Trim();

        // ICS format starts with BEGIN:VCALENDAR
        if (content.StartsWith("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
        {
            return await ParseIcsContent(content, provider);
        }

        // JSON format
        return provider switch
        {
            CalendarProvider.Google => await ParseGoogleCalendarExport(content),
            CalendarProvider.Outlook => await ParseOutlookExport(content),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    public Task<IEnumerable<ExternalCalendarEvent>> ParseGoogleCalendarExport(string jsonContent)
    {
        var events = new List<ExternalCalendarEvent>();

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Google Calendar export has "items" array
            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var evt = ParseGoogleEvent(item);
                    if (evt != null)
                        events.Add(evt);
                }
            }
            // Or it might be just an array of events
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var evt = ParseGoogleEvent(item);
                    if (evt != null)
                        events.Add(evt);
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Google Calendar JSON: {ex.Message}", ex);
        }

        return Task.FromResult<IEnumerable<ExternalCalendarEvent>>(events);
    }

    public Task<IEnumerable<ExternalCalendarEvent>> ParseOutlookExport(string jsonContent)
    {
        var events = new List<ExternalCalendarEvent>();

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Outlook/Graph API export has "value" array
            if (root.TryGetProperty("value", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    var evt = ParseOutlookEvent(item);
                    if (evt != null)
                        events.Add(evt);
                }
            }
            // Or it might be just an array
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var evt = ParseOutlookEvent(item);
                    if (evt != null)
                        events.Add(evt);
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Outlook Calendar JSON: {ex.Message}", ex);
        }

        return Task.FromResult<IEnumerable<ExternalCalendarEvent>>(events);
    }

    public Task<IEnumerable<ExternalCalendarEvent>> ParseIcsContent(string icsContent, CalendarProvider provider)
    {
        var events = new List<ExternalCalendarEvent>();
        var eventBlocks = Regex.Split(icsContent, @"(?=BEGIN:VEVENT)");

        foreach (var block in eventBlocks)
        {
            if (!block.Contains("BEGIN:VEVENT"))
                continue;

            var evt = ParseIcsEvent(block, provider);
            if (evt != null)
                events.Add(evt);
        }

        return Task.FromResult<IEnumerable<ExternalCalendarEvent>>(events);
    }

    public async Task<CalendarImportResult> SaveImportedEventsAsync(
        IEnumerable<ExternalCalendarEvent> externalEvents,
        bool skipDuplicates = true)
    {
        var result = new CalendarImportResult();
        var existingEvents = await _calendarRepository.GetAllAsync();
        var existingExternalIds = existingEvents
            .Where(e => !string.IsNullOrEmpty(e.ExternalId))
            .Select(e => e.ExternalId)
            .ToHashSet();

        foreach (var extEvent in externalEvents)
        {
            result.TotalProcessed++;

            try
            {
                // Skip duplicates if requested
                if (skipDuplicates && existingExternalIds.Contains(extEvent.ExternalId))
                {
                    result.SkippedDuplicates++;
                    continue;
                }

                // Skip cancelled events
                if (extEvent.IsCancelled)
                {
                    result.SkippedDuplicates++;
                    continue;
                }

                var calendarEvent = extEvent.ToCalendarEvent();
                await _calendarRepository.AddAsync(calendarEvent);

                result.ImportedCount++;
                result.ImportedEvents.Add(calendarEvent);
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Failed to import '{extEvent.Title}': {ex.Message}");
            }
        }

        // Auto-link imported events to entities based on patterns
        if (result.ImportedEvents.Any())
        {
            try
            {
                var linkResults = await _entityLinkingService.AnalyzeAndLinkEventsAsync(result.ImportedEvents);
                result.LinkedEventsCount = linkResults.Count(r => r.WasModified);
            }
            catch
            {
                // Don't fail import if linking fails
            }
        }

        result.Success = result.ErrorCount == 0;
        return result;
    }

    private ExternalCalendarEvent? ParseGoogleEvent(JsonElement item)
    {
        try
        {
            var evt = new ExternalCalendarEvent
            {
                Source = CalendarProvider.Google,
                ExternalId = GetStringValue(item, "id") ?? Guid.NewGuid().ToString(),
                Title = GetStringValue(item, "summary") ?? "Untitled Event",
                Description = GetStringValue(item, "description"),
                Location = GetStringValue(item, "location"),
                ICalUid = GetStringValue(item, "iCalUID"),
                ExternalWebLink = GetStringValue(item, "htmlLink")
            };

            // Parse start time
            if (item.TryGetProperty("start", out var start))
            {
                if (start.TryGetProperty("dateTime", out var startDateTime))
                {
                    evt.StartTime = DateTime.Parse(startDateTime.GetString()!);
                    evt.StartTimeZone = GetStringValue(start, "timeZone");
                }
                else if (start.TryGetProperty("date", out var startDate))
                {
                    evt.StartTime = DateTime.Parse(startDate.GetString()!);
                    evt.IsAllDay = true;
                }
            }

            // Parse end time
            if (item.TryGetProperty("end", out var end))
            {
                if (end.TryGetProperty("dateTime", out var endDateTime))
                {
                    evt.EndTime = DateTime.Parse(endDateTime.GetString()!);
                    evt.EndTimeZone = GetStringValue(end, "timeZone");
                }
                else if (end.TryGetProperty("date", out var endDate))
                {
                    evt.EndTime = DateTime.Parse(endDate.GetString()!);
                }
            }

            // Parse organizer
            if (item.TryGetProperty("organizer", out var organizer))
            {
                evt.OrganizerEmail = GetStringValue(organizer, "email");
                evt.OrganizerName = GetStringValue(organizer, "displayName");
                evt.IsOrganizer = GetBoolValue(organizer, "self");
            }

            // Parse attendees
            if (item.TryGetProperty("attendees", out var attendees))
            {
                foreach (var attendee in attendees.EnumerateArray())
                {
                    evt.Attendees.Add(new ExternalAttendee
                    {
                        Email = GetStringValue(attendee, "email"),
                        Name = GetStringValue(attendee, "displayName"),
                        IsOptional = GetBoolValue(attendee, "optional"),
                        IsOrganizer = GetBoolValue(attendee, "organizer"),
                        ResponseStatus = ParseGoogleResponseStatus(GetStringValue(attendee, "responseStatus"))
                    });
                }
            }

            // Parse recurrence
            if (item.TryGetProperty("recurrence", out var recurrence) &&
                recurrence.ValueKind == JsonValueKind.Array)
            {
                evt.IsRecurring = true;
                var rules = recurrence.EnumerateArray()
                    .Select(r => r.GetString())
                    .Where(r => r?.StartsWith("RRULE:") == true);
                evt.RecurrenceRule = string.Join(";", rules);
            }
            evt.RecurringEventId = GetStringValue(item, "recurringEventId");

            // Parse status
            var status = GetStringValue(item, "status");
            evt.IsCancelled = status == "cancelled";

            // Parse visibility
            var visibility = GetStringValue(item, "visibility");
            evt.Visibility = visibility switch
            {
                "public" => EventVisibility.Public,
                "private" => EventVisibility.Private,
                "confidential" => EventVisibility.Confidential,
                _ => EventVisibility.Default
            };

            // Parse conference data for online meeting URL
            if (item.TryGetProperty("conferenceData", out var conf) &&
                conf.TryGetProperty("entryPoints", out var entryPoints))
            {
                foreach (var ep in entryPoints.EnumerateArray())
                {
                    var type = GetStringValue(ep, "entryPointType");
                    if (type == "video")
                    {
                        evt.OnlineMeetingUrl = GetStringValue(ep, "uri");
                        break;
                    }
                }
            }

            // Parse timestamps
            var created = GetStringValue(item, "created");
            if (!string.IsNullOrEmpty(created))
                evt.ExternalCreatedAt = DateTime.Parse(created);

            var updated = GetStringValue(item, "updated");
            if (!string.IsNullOrEmpty(updated))
                evt.ExternalModifiedAt = DateTime.Parse(updated);

            return evt;
        }
        catch
        {
            return null;
        }
    }

    private ExternalCalendarEvent? ParseOutlookEvent(JsonElement item)
    {
        try
        {
            var evt = new ExternalCalendarEvent
            {
                Source = CalendarProvider.Outlook,
                ExternalId = GetStringValue(item, "id") ?? Guid.NewGuid().ToString(),
                Title = GetStringValue(item, "subject") ?? "Untitled Event",
                ICalUid = GetStringValue(item, "iCalUId"),
                ExternalWebLink = GetStringValue(item, "webLink")
            };

            // Parse body/description
            if (item.TryGetProperty("body", out var body))
            {
                evt.Description = GetStringValue(body, "content");
            }

            // Parse start time
            if (item.TryGetProperty("start", out var start))
            {
                var dateTime = GetStringValue(start, "dateTime");
                if (!string.IsNullOrEmpty(dateTime))
                    evt.StartTime = DateTime.Parse(dateTime);
                evt.StartTimeZone = GetStringValue(start, "timeZone");
            }

            // Parse end time
            if (item.TryGetProperty("end", out var end))
            {
                var dateTime = GetStringValue(end, "dateTime");
                if (!string.IsNullOrEmpty(dateTime))
                    evt.EndTime = DateTime.Parse(dateTime);
                evt.EndTimeZone = GetStringValue(end, "timeZone");
            }

            evt.IsAllDay = GetBoolValue(item, "isAllDay");

            // Parse location
            if (item.TryGetProperty("location", out var location))
            {
                evt.Location = GetStringValue(location, "displayName");
            }

            // Parse organizer
            if (item.TryGetProperty("organizer", out var organizer) &&
                organizer.TryGetProperty("emailAddress", out var orgEmail))
            {
                evt.OrganizerEmail = GetStringValue(orgEmail, "address");
                evt.OrganizerName = GetStringValue(orgEmail, "name");
            }
            evt.IsOrganizer = GetBoolValue(item, "isOrganizer");

            // Parse attendees
            if (item.TryGetProperty("attendees", out var attendees))
            {
                foreach (var attendee in attendees.EnumerateArray())
                {
                    var email = attendee.TryGetProperty("emailAddress", out var ea)
                        ? GetStringValue(ea, "address")
                        : null;
                    var name = attendee.TryGetProperty("emailAddress", out var na)
                        ? GetStringValue(na, "name")
                        : null;

                    evt.Attendees.Add(new ExternalAttendee
                    {
                        Email = email,
                        Name = name,
                        IsOptional = GetStringValue(attendee, "type") == "optional",
                        ResponseStatus = ParseOutlookResponseStatus(
                            attendee.TryGetProperty("status", out var status)
                                ? GetStringValue(status, "response")
                                : null)
                    });
                }
            }

            // Parse recurrence
            evt.IsRecurring = item.TryGetProperty("recurrence", out _);
            evt.RecurringEventId = GetStringValue(item, "seriesMasterId");

            // Parse show as status
            var showAs = GetStringValue(item, "showAs");
            evt.ShowAs = showAs switch
            {
                "free" => ShowAsStatus.Free,
                "tentative" => ShowAsStatus.Tentative,
                "busy" => ShowAsStatus.Busy,
                "oof" => ShowAsStatus.OutOfOffice,
                "workingElsewhere" => ShowAsStatus.WorkingElsewhere,
                _ => ShowAsStatus.Busy
            };

            // Parse online meeting
            evt.OnlineMeetingUrl = GetStringValue(item, "onlineMeetingUrl");
            if (item.TryGetProperty("onlineMeeting", out var onlineMeeting))
            {
                evt.OnlineMeetingUrl ??= GetStringValue(onlineMeeting, "joinUrl");
            }
            evt.OnlineMeetingType = GetStringValue(item, "onlineMeetingProvider");

            // Parse cancelled status
            evt.IsCancelled = GetBoolValue(item, "isCancelled");

            // Parse categories
            if (item.TryGetProperty("categories", out var categories))
            {
                foreach (var cat in categories.EnumerateArray())
                {
                    var catName = cat.GetString();
                    if (!string.IsNullOrEmpty(catName))
                        evt.Categories.Add(catName);
                }
            }

            // Parse timestamps
            var createdAt = GetStringValue(item, "createdDateTime");
            if (!string.IsNullOrEmpty(createdAt))
                evt.ExternalCreatedAt = DateTime.Parse(createdAt);

            var modifiedAt = GetStringValue(item, "lastModifiedDateTime");
            if (!string.IsNullOrEmpty(modifiedAt))
                evt.ExternalModifiedAt = DateTime.Parse(modifiedAt);

            // Parse response status
            var responseStatus = GetStringValue(item, "responseStatus.response");
            if (item.TryGetProperty("responseStatus", out var rs))
            {
                evt.ResponseStatus = ParseOutlookResponseStatus(GetStringValue(rs, "response"));
            }

            return evt;
        }
        catch
        {
            return null;
        }
    }

    private ExternalCalendarEvent? ParseIcsEvent(string eventBlock, CalendarProvider provider)
    {
        try
        {
            var evt = new ExternalCalendarEvent
            {
                Source = provider
            };

            var lines = eventBlock.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex < 0) continue;

                var key = line.Substring(0, colonIndex);
                var value = line.Substring(colonIndex + 1);

                // Handle parameters in key (e.g., DTSTART;TZID=America/New_York)
                var semicolonIndex = key.IndexOf(';');
                if (semicolonIndex > 0)
                    key = key.Substring(0, semicolonIndex);

                switch (key.ToUpperInvariant())
                {
                    case "UID":
                        evt.ExternalId = value;
                        evt.ICalUid = value;
                        break;
                    case "SUMMARY":
                        evt.Title = UnescapeIcsValue(value);
                        break;
                    case "DESCRIPTION":
                        evt.Description = UnescapeIcsValue(value);
                        break;
                    case "LOCATION":
                        evt.Location = UnescapeIcsValue(value);
                        break;
                    case "DTSTART":
                        evt.StartTime = ParseIcsDateTime(value);
                        if (value.Length == 8) // Date only format: YYYYMMDD
                            evt.IsAllDay = true;
                        break;
                    case "DTEND":
                        evt.EndTime = ParseIcsDateTime(value);
                        break;
                    case "ORGANIZER":
                        evt.OrganizerEmail = value.Replace("mailto:", "");
                        break;
                    case "STATUS":
                        evt.IsCancelled = value.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "RRULE":
                        evt.IsRecurring = true;
                        evt.RecurrenceRule = value;
                        break;
                }
            }

            // Set a default external ID if not found
            if (string.IsNullOrEmpty(evt.ExternalId))
                evt.ExternalId = Guid.NewGuid().ToString();

            return evt;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStringValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static bool GetBoolValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) &&
               prop.ValueKind == JsonValueKind.True;
    }

    private static MeetingResponseStatus ParseGoogleResponseStatus(string? status)
    {
        return status switch
        {
            "accepted" => MeetingResponseStatus.Accepted,
            "declined" => MeetingResponseStatus.Declined,
            "tentative" => MeetingResponseStatus.Tentative,
            "needsAction" => MeetingResponseStatus.NotResponded,
            _ => MeetingResponseStatus.None
        };
    }

    private static MeetingResponseStatus ParseOutlookResponseStatus(string? status)
    {
        return status switch
        {
            "accepted" => MeetingResponseStatus.Accepted,
            "declined" => MeetingResponseStatus.Declined,
            "tentativelyAccepted" => MeetingResponseStatus.Tentative,
            "notResponded" => MeetingResponseStatus.NotResponded,
            "none" => MeetingResponseStatus.None,
            _ => MeetingResponseStatus.None
        };
    }

    private static DateTime ParseIcsDateTime(string value)
    {
        // Handle both date-time (YYYYMMDDTHHMMSS) and date (YYYYMMDD) formats
        value = value.TrimEnd('Z'); // Remove UTC indicator for now
        if (value.Length >= 15)
        {
            return DateTime.ParseExact(value.Substring(0, 15), "yyyyMMdd'T'HHmmss", null);
        }
        else if (value.Length >= 8)
        {
            return DateTime.ParseExact(value.Substring(0, 8), "yyyyMMdd", null);
        }
        return DateTime.MinValue;
    }

    private static string UnescapeIcsValue(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\,", ",")
            .Replace("\\;", ";")
            .Replace("\\\\", "\\");
    }
}
