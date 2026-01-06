using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class MeetingInsightService : IMeetingInsightService
{
    private readonly ICalendarService _calendarService;
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly IRepository<CategoryDefinition> _categoryRepository;

    // Defaults when no category-specific settings exist
    private const int DefaultPrepMinutes = 5;
    private const int DefaultDecompressMinutes = 5;
    private const int LunchStartHour = 12;
    private const int LunchEndHour = 13;
    private const int MeetingHeavyThresholdPercent = 60; // Over 60% of workday in meetings

    public MeetingInsightService(
        ICalendarService calendarService,
        IRepository<UserPreferences> preferencesRepository,
        IRepository<CategoryDefinition> categoryRepository)
    {
        _calendarService = calendarService;
        _preferencesRepository = preferencesRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<DayMeetingAnalysis> AnalyzeDayAsync(DateOnly date)
    {
        var events = (await _calendarService.GetEventsForDateAsync(date))
            .Where(e => !e.IsAllDay)
            .OrderBy(e => e.StartTime)
            .ToList();

        var preferences = (await _preferencesRepository.GetAllAsync()).FirstOrDefault() ?? new UserPreferences();
        var categories = (await _categoryRepository.GetAllAsync()).ToList();

        var workDayStart = date.ToDateTime(TimeOnly.FromTimeSpan(preferences.WorkDayStart));
        var workDayEnd = date.ToDateTime(TimeOnly.FromTimeSpan(preferences.WorkDayEnd));
        var totalWorkMinutes = (int)(workDayEnd - workDayStart).TotalMinutes;

        var analysis = new DayMeetingAnalysis
        {
            Date = date,
            TotalMeetings = events.Count,
            TotalMeetingMinutes = events.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes),
            Insights = new List<MeetingInsight>()
        };

        if (!events.Any())
        {
            analysis.AvailableFocusMinutes = totalWorkMinutes;
            return analysis;
        }

        // Analyze back-to-back meetings
        var consecutiveBlocks = IdentifyConsecutiveBlocks(events, preferences.BufferBetweenMeetingsMinutes);
        analysis.BackToBackCount = consecutiveBlocks.Count(b => b.Count > 1);

        // Check for back-to-back warnings
        for (int i = 0; i < events.Count - 1; i++)
        {
            var current = events[i];
            var next = events[i + 1];

            if (AreBackToBack(current, next, preferences.BufferBetweenMeetingsMinutes))
            {
                var gap = (int)(next.StartTime - current.EndTime).TotalMinutes;
                var severity = gap <= 0 ? MeetingInsightSeverity.Critical : MeetingInsightSeverity.Warning;

                analysis.Insights.Add(new MeetingInsight
                {
                    Type = MeetingInsightType.BackToBackMeetings,
                    Severity = severity,
                    Message = gap <= 0
                        ? $"'{current.Title}' and '{next.Title}' overlap or have no gap"
                        : $"Only {gap} min between '{current.Title}' and '{next.Title}'",
                    Suggestion = "Consider rescheduling to add buffer time",
                    RelatedTime = current.EndTime,
                    RelatedEventIds = new List<Guid> { current.Id, next.Id }
                });
            }
        }

        // Check for consecutive meeting time limit
        foreach (var block in consecutiveBlocks)
        {
            var blockMinutes = (int)(block.Last().EndTime - block.First().StartTime).TotalMinutes;
            if (blockMinutes > preferences.MaxConsecutiveMeetingMinutes)
            {
                analysis.Insights.Add(new MeetingInsight
                {
                    Type = MeetingInsightType.ConsecutiveMeetingsExceeded,
                    Severity = MeetingInsightSeverity.Critical,
                    Message = $"{blockMinutes} min of consecutive meetings ({block.Count} meetings) exceeds your {preferences.MaxConsecutiveMeetingMinutes} min limit",
                    Suggestion = "Consider adding a break or rescheduling some meetings",
                    RelatedTime = block.First().StartTime,
                    RelatedEventIds = block.Select(e => e.Id).ToList()
                });

                if (blockMinutes > analysis.MaxConsecutiveMinutes)
                    analysis.MaxConsecutiveMinutes = blockMinutes;
            }
        }

        // Check for missing prep time on meetings that need it
        foreach (var evt in events.Where(e => e.RequiresPrep))
        {
            var prevEvent = events.Where(e => e.EndTime <= evt.StartTime).LastOrDefault();
            var suggestedPrep = GetSuggestedPrepMinutes(evt);
            var availablePrepTime = prevEvent != null
                ? (int)(evt.StartTime - prevEvent.EndTime).TotalMinutes
                : (int)(evt.StartTime - workDayStart).TotalMinutes;

            if (availablePrepTime < suggestedPrep)
            {
                analysis.Insights.Add(new MeetingInsight
                {
                    Type = MeetingInsightType.NoPrepTime,
                    Severity = MeetingInsightSeverity.Warning,
                    Message = $"'{evt.Title}' needs {suggestedPrep} min prep but only {Math.Max(0, availablePrepTime)} min available",
                    Suggestion = $"Block {suggestedPrep} minutes before this meeting for preparation",
                    RelatedTime = evt.StartTime.AddMinutes(-suggestedPrep),
                    RelatedEventIds = new List<Guid> { evt.Id }
                });
            }
        }

        // Check for lunch conflict
        foreach (var evt in events)
        {
            var lunchStart = date.ToDateTime(new TimeOnly(LunchStartHour, 0));
            var lunchEnd = date.ToDateTime(new TimeOnly(LunchEndHour, 0));

            if (evt.StartTime < lunchEnd && evt.EndTime > lunchStart)
            {
                analysis.Insights.Add(new MeetingInsight
                {
                    Type = MeetingInsightType.LunchConflict,
                    Severity = MeetingInsightSeverity.Info,
                    Message = $"'{evt.Title}' overlaps with lunch time",
                    Suggestion = "Consider protecting your lunch hour when possible",
                    RelatedTime = evt.StartTime,
                    RelatedEventIds = new List<Guid> { evt.Id }
                });
            }
        }

        // Calculate available focus time
        var meetingRanges = events.Select(e => (e.StartTime, e.EndTime)).ToList();
        analysis.AvailableFocusMinutes = CalculateAvailableFocusTime(
            meetingRanges, workDayStart, workDayEnd, preferences.MinimumUsableBlockMinutes);

        // Check if day is meeting-heavy
        var meetingPercent = (analysis.TotalMeetingMinutes * 100) / totalWorkMinutes;
        if (meetingPercent >= MeetingHeavyThresholdPercent)
        {
            analysis.Insights.Add(new MeetingInsight
            {
                Type = MeetingInsightType.MeetingHeavyDay,
                Severity = meetingPercent >= 80 ? MeetingInsightSeverity.Critical : MeetingInsightSeverity.Warning,
                Message = $"{meetingPercent}% of your workday is meetings ({analysis.TotalMeetingMinutes} min)",
                Suggestion = "Consider declining some meetings or rescheduling to another day"
            });
        }

        // Check if there's no focus time available
        if (analysis.AvailableFocusMinutes < preferences.DeepWorkMinimumMinutes && events.Any())
        {
            analysis.Insights.Add(new MeetingInsight
            {
                Type = MeetingInsightType.NoFocusTime,
                Severity = MeetingInsightSeverity.Warning,
                Message = $"Only {analysis.AvailableFocusMinutes} min available for focused work today",
                Suggestion = "Try to protect a block of time for deep work"
            });
        }

        // Sort insights by severity (critical first) then by time
        analysis.Insights = analysis.Insights
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.RelatedTime)
            .ToList();

        return analysis;
    }

    public int GetSuggestedPrepMinutes(CalendarEvent meeting)
    {
        // Base prep time on meeting category and size
        var basePrep = meeting.PrepTimeMinutes ?? DefaultPrepMinutes;

        // Add more prep for larger meetings
        if (meeting.Attendees.Count > 5)
            basePrep += 5;
        if (meeting.Attendees.Count > 10)
            basePrep += 10;

        // Add prep for certain categories
        return meeting.EffectiveCategory switch
        {
            MeetingCategory.OneOnOne => Math.Max(basePrep, 5),
            MeetingCategory.TeamMeeting => Math.Max(basePrep, 10),
            MeetingCategory.ClientMeeting => Math.Max(basePrep, 15),
            MeetingCategory.Interview => Math.Max(basePrep, 15),
            MeetingCategory.Presentation => Math.Max(basePrep, 20),
            _ => basePrep
        };
    }

    public int GetSuggestedDecompressMinutes(CalendarEvent meeting)
    {
        var baseDecompress = meeting.DecompressTimeMinutes ?? DefaultDecompressMinutes;

        // More decompression after intense meetings
        return meeting.EffectiveCategory switch
        {
            MeetingCategory.Interview => Math.Max(baseDecompress, 10),
            MeetingCategory.ClientMeeting => Math.Max(baseDecompress, 10),
            MeetingCategory.Presentation => Math.Max(baseDecompress, 15),
            MeetingCategory.OneOnOne => Math.Max(baseDecompress, 5),
            _ => baseDecompress
        };
    }

    public bool AreBackToBack(CalendarEvent first, CalendarEvent second, int bufferMinutes)
    {
        var gap = (second.StartTime - first.EndTime).TotalMinutes;
        return gap < bufferMinutes;
    }

    private List<List<CalendarEvent>> IdentifyConsecutiveBlocks(
        List<CalendarEvent> events, int bufferMinutes)
    {
        if (!events.Any()) return new();

        var blocks = new List<List<CalendarEvent>>();
        var currentBlock = new List<CalendarEvent> { events[0] };

        for (int i = 1; i < events.Count; i++)
        {
            if (AreBackToBack(events[i - 1], events[i], bufferMinutes))
            {
                currentBlock.Add(events[i]);
            }
            else
            {
                blocks.Add(currentBlock);
                currentBlock = new List<CalendarEvent> { events[i] };
            }
        }

        blocks.Add(currentBlock);
        return blocks;
    }

    private int CalculateAvailableFocusTime(
        List<(DateTime Start, DateTime End)> meetingRanges,
        DateTime workStart, DateTime workEnd, int minimumBlockMinutes)
    {
        if (!meetingRanges.Any())
            return (int)(workEnd - workStart).TotalMinutes;

        var sorted = meetingRanges.OrderBy(r => r.Start).ToList();
        var focusMinutes = 0;
        var currentTime = workStart;

        foreach (var (start, end) in sorted)
        {
            if (start > currentTime)
            {
                var gap = (int)(start - currentTime).TotalMinutes;
                if (gap >= minimumBlockMinutes)
                    focusMinutes += gap;
            }
            if (end > currentTime)
                currentTime = end;
        }

        // Time after last meeting
        if (currentTime < workEnd)
        {
            var gap = (int)(workEnd - currentTime).TotalMinutes;
            if (gap >= minimumBlockMinutes)
                focusMinutes += gap;
        }

        return focusMinutes;
    }
}
