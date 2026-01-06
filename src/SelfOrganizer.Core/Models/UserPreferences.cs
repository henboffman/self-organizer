namespace SelfOrganizer.Core.Models;

/// <summary>
/// User preferences for scheduling and workflow
/// </summary>
public class UserPreferences : BaseEntity
{
    // Onboarding
    public bool OnboardingCompleted { get; set; } = false;
    public CalendarProvider? PreferredCalendarProvider { get; set; }

    // Work Schedule Settings
    public TimeSpan WorkDayStart { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan WorkDayEnd { get; set; } = TimeSpan.FromHours(17);
    public List<DayOfWeek> WorkDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };
    public int DefaultTaskDurationMinutes { get; set; } = 30;
    public int MinimumUsableBlockMinutes { get; set; } = 15;
    public int DeepWorkMinimumMinutes { get; set; } = 60;
    public int DefaultBreakMinutes { get; set; } = 10;
    public int MaxConsecutiveMeetingMinutes { get; set; } = 180;
    public int BufferBetweenMeetingsMinutes { get; set; } = 5;
    public int MorningEnergyPeak { get; set; } = 10; // Hour of day
    public int AfternoonEnergyPeak { get; set; } = 15;
    public bool AutoScheduleEnabled { get; set; } = true;
    public int DailyReviewReminderHour { get; set; } = 17;
    public int WeeklyReviewDay { get; set; } = 5; // Friday

    // ADHD & Neurodivergent Accommodations
    public bool EnableCelebrationEffects { get; set; } = true;
    public bool EnableFocusTimer { get; set; } = false;
    public int FocusTimerMinutes { get; set; } = 25; // Pomodoro-style
    public bool EnableTimeBlindnessHelpers { get; set; } = true;
    public bool EnableTaskChunking { get; set; } = false;
    public int MaxTaskChunkMinutes { get; set; } = 25;
    public bool EnableHyperfocusAlerts { get; set; } = false;
    public int HyperfocusAlertMinutes { get; set; } = 90;
    public bool EnableContextSwitchWarnings { get; set; } = false;
    public bool EnableMinimalMode { get; set; } = false;
    public bool EnablePickForMe { get; set; } = true; // "Just pick one" button

    // Schedule Optimization Weights (0-100)
    public int ContextGroupingWeight { get; set; } = 50; // Group tasks by @context
    public int SimilarWorkGroupingWeight { get; set; } = 50; // Group by category/project
    public int EnergyMatchingWeight { get; set; } = 50; // Match task energy to time of day
    public int DueDateUrgencyWeight { get; set; } = 70; // Prioritize approaching due dates
    public int StakeholderGroupingWeight { get; set; } = 30; // Group tasks by WhoFor
    public int TagSimilarityWeight { get; set; } = 40; // Group tasks with similar tags
    public int DeepWorkPreferenceWeight { get; set; } = 60; // Prefer morning for deep work
    public int BlockedTaskPenalty { get; set; } = 100; // Deprioritize blocked tasks
}
