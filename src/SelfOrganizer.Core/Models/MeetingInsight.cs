namespace SelfOrganizer.Core.Models;

/// <summary>
/// Represents insights and warnings about meeting patterns
/// </summary>
public class MeetingInsight
{
    public MeetingInsightType Type { get; set; }
    public MeetingInsightSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public DateTime? RelatedTime { get; set; }
    public List<Guid> RelatedEventIds { get; set; } = new();
}

public enum MeetingInsightType
{
    BackToBackMeetings,
    NoBufferTime,
    ConsecutiveMeetingsExceeded,
    NoPrepTime,
    NoDecompressTime,
    MeetingHeavyDay,
    NoFocusTime,
    LunchConflict
}

public enum MeetingInsightSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Summary of meeting analysis for a day
/// </summary>
public class DayMeetingAnalysis
{
    public DateOnly Date { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalMeetingMinutes { get; set; }
    public int BackToBackCount { get; set; }
    public int AvailableFocusMinutes { get; set; }
    public int MaxConsecutiveMinutes { get; set; }
    public List<MeetingInsight> Insights { get; set; } = new();
    public bool HasCriticalIssues => Insights.Any(i => i.Severity == MeetingInsightSeverity.Critical);
    public bool HasWarnings => Insights.Any(i => i.Severity == MeetingInsightSeverity.Warning);
}
