namespace SelfOrganizer.Core.Models;

/// <summary>
/// Weekly snapshot for reflection and review
/// </summary>
public class WeeklySnapshot : BaseEntity
{
    /// <summary>
    /// The Monday of the week this snapshot represents
    /// </summary>
    public DateOnly WeekStart { get; set; }

    // Stats (computed from DailySnapshots)
    public int TotalTasksCompleted { get; set; }
    public int TotalTasksCreated { get; set; }
    public int TotalMinutesWorked { get; set; }
    public int TotalMeetingMinutes { get; set; }
    public int TotalDeepWorkMinutes { get; set; }
    public int DaysWithMorningCheckin { get; set; }
    public int HabitsCompletedCount { get; set; }

    // Reflection
    public string? BiggestWin { get; set; }
    public List<string> OtherWins { get; set; } = new();
    public string? BiggestChallenge { get; set; }
    public string? LessonsLearned { get; set; }
    public string? GratitudeReflection { get; set; }

    // Energy & Wellbeing
    public int? AverageEnergyLevel { get; set; } // 1-5
    public int? AverageMoodLevel { get; set; } // 1-5
    public int? WorkLifeBalanceRating { get; set; } // 1-5
    public int? ProductivityRating { get; set; } // 1-5

    // GTD Review Status
    public bool InboxCleared { get; set; }
    public bool ProjectsReviewed { get; set; }
    public bool WaitingForReviewed { get; set; }
    public bool SomedayMaybeReviewed { get; set; }
    public bool CalendarReviewed { get; set; }

    // Next Week
    public string? NextWeekFocus { get; set; }
    public List<string> NextWeekPriorities { get; set; } = new();

    // Completion
    public bool ReviewCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}
