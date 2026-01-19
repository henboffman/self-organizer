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

    // Extended ADHD Accommodations
    /// <summary>Show virtual co-working presence indicator</summary>
    public bool EnableBodyDoubling { get; set; } = false;

    /// <summary>Show visual progress bars for tasks and projects</summary>
    public bool EnableProgressVisualization { get; set; } = true;

    /// <summary>Small celebrations for completing subtasks</summary>
    public bool EnableMicroRewards { get; set; } = true;

    /// <summary>Minutes before a task is marked as "late" starting</summary>
    public int TaskStartGracePeriod { get; set; } = 5;

    /// <summary>Non-intrusive reminder nudges</summary>
    public bool EnableGentleReminders { get; set; } = true;

    /// <summary>Show estimated completion time (e.g., "Done by 3:30 PM")</summary>
    public bool ShowEstimatedCompletion { get; set; } = true;

    /// <summary>Enable ambient sounds during focus sessions</summary>
    public bool EnableFocusSounds { get; set; } = false;

    /// <summary>Preferred ambient sound type for focus sessions</summary>
    public FocusSoundType PreferredFocusSound { get; set; } = FocusSoundType.None;

    /// <summary>Default break duration in minutes</summary>
    public int BreakReminderMinutes { get; set; } = 5;

    /// <summary>Automatically start break timer when focus session ends</summary>
    public bool AutoStartBreakTimer { get; set; } = true;

    /// <summary>Enable smooth task switching prompts</summary>
    public bool EnableTaskTransitions { get; set; } = true;

    /// <summary>Buffer time in minutes between tasks</summary>
    public int TransitionBufferMinutes { get; set; } = 2;

    // Advanced Focus Timer Settings
    /// <summary>Show session intention prompt before starting focus</summary>
    public bool ShowSessionIntentionPrompt { get; set; } = true;

    /// <summary>Show reflection prompt after focus sessions</summary>
    public bool ShowPostSessionReflection { get; set; } = true;

    /// <summary>Track and display distraction counts during focus</summary>
    public bool TrackDistractions { get; set; } = true;

    /// <summary>Enable streak tracking for consecutive focus sessions</summary>
    public bool EnableFocusStreaks { get; set; } = true;

    /// <summary>Play sound when timer completes</summary>
    public bool PlayTimerCompletionSound { get; set; } = true;

    /// <summary>Show desktop notification when timer completes</summary>
    public bool ShowTimerNotification { get; set; } = true;

    /// <summary>Auto-pause timer when window loses focus (for accountability)</summary>
    public bool PauseTimerOnWindowBlur { get; set; } = false;

    /// <summary>Long break duration in minutes (after multiple focus sessions)</summary>
    public int LongBreakMinutes { get; set; } = 15;

    /// <summary>Number of focus sessions before a long break</summary>
    public int SessionsBeforeLongBreak { get; set; } = 4;

    // Task Presentation & Cognitive Load
    /// <summary>Maximum number of tasks to show at once (to reduce overwhelm)</summary>
    public int MaxVisibleTasks { get; set; } = 10;

    /// <summary>Enable "one task at a time" mode showing only the current task</summary>
    public bool OneTaskAtATimeMode { get; set; } = false;

    /// <summary>Hide task counts to reduce anxiety</summary>
    public bool HideTaskCounts { get; set; } = false;

    /// <summary>Hide due dates on task cards (show only on detail view)</summary>
    public bool HideDueDatesOnCards { get; set; } = false;

    /// <summary>Use simplified task view with less information</summary>
    public bool SimplifiedTaskView { get; set; } = false;

    /// <summary>Show encouraging messages and affirmations</summary>
    public bool ShowEncouragingMessages { get; set; } = true;

    // Time Perception Helpers
    /// <summary>Show elapsed time on active tasks</summary>
    public bool ShowElapsedTime { get; set; } = true;

    /// <summary>Show time remaining in current work block</summary>
    public bool ShowTimeRemainingInBlock { get; set; } = true;

    /// <summary>Interval in minutes for time awareness notifications</summary>
    public int TimeAwarenessIntervalMinutes { get; set; } = 30;

    /// <summary>Enable ambient time display (subtle clock/timer always visible)</summary>
    public bool EnableAmbientTimeDisplay { get; set; } = false;

    /// <summary>Show "time since last break" counter</summary>
    public bool ShowTimeSinceLastBreak { get; set; } = true;

    // Sensory & Environment
    /// <summary>Enable dark mode auto-switch based on time of day</summary>
    public bool AutoDarkMode { get; set; } = false;

    /// <summary>Hour to switch to dark mode (24h format)</summary>
    public int DarkModeStartHour { get; set; } = 18;

    /// <summary>Hour to switch to light mode (24h format)</summary>
    public int DarkModEndHour { get; set; } = 7;

    /// <summary>Volume level for sounds (0-100)</summary>
    public int SoundVolume { get; set; } = 50;

    /// <summary>Mute all sounds</summary>
    public bool MuteAllSounds { get; set; } = false;

    // Motivation & Rewards
    /// <summary>Enable daily goal setting prompt</summary>
    public bool EnableDailyGoalSetting { get; set; } = true;

    /// <summary>Show progress toward daily task goal</summary>
    public bool ShowDailyProgressBar { get; set; } = true;

    /// <summary>Daily task completion goal count</summary>
    public int DailyTaskGoal { get; set; } = 5;

    /// <summary>Enable weekly summary with achievements</summary>
    public bool EnableWeeklySummary { get; set; } = true;

    /// <summary>Show task completion animations intensity (0=none, 1=subtle, 2=normal, 3=extra)</summary>
    public int CelebrationIntensity { get; set; } = 2;

    // Executive Function Support
    /// <summary>Enable task breakdown suggestions for large tasks</summary>
    public bool SuggestTaskBreakdown { get; set; } = true;

    /// <summary>Threshold in minutes for suggesting task breakdown</summary>
    public int TaskBreakdownThresholdMinutes { get; set; } = 60;

    /// <summary>Enable "what's blocking you?" prompts for stuck tasks</summary>
    public bool EnableBlockerPrompts { get; set; } = true;

    /// <summary>Days before prompting about stale tasks</summary>
    public int StaleTakPromptDays { get; set; } = 3;

    /// <summary>Enable quick capture mode (minimal fields for fast entry)</summary>
    public bool EnableQuickCaptureMode { get; set; } = true;

    /// <summary>Auto-expand next actions when project is selected</summary>
    public bool AutoExpandNextActions { get; set; } = true;

    // Accountability & Structure
    /// <summary>Enable daily planning prompt in the morning</summary>
    public bool EnableMorningPlanningPrompt { get; set; } = false;

    /// <summary>Hour for morning planning prompt (24h format)</summary>
    public int MorningPlanningHour { get; set; } = 9;

    /// <summary>Enable evening wind-down reminder</summary>
    public bool EnableEveningWindDown { get; set; } = false;

    /// <summary>Hour for evening wind-down (24h format)</summary>
    public int EveningWindDownHour { get; set; } = 17;

    /// <summary>Enable "commitment mode" where started tasks must be completed or explicitly abandoned</summary>
    public bool EnableCommitmentMode { get; set; } = false;

    /// <summary>Show reason field when abandoning a task</summary>
    public bool RequireAbandonReason { get; set; } = false;

    // Accessibility Settings
    /// <summary>Comprehensive accessibility settings for vision, reading, motion, and interaction</summary>
    public AccessibilitySettings? Accessibility { get; set; }

    // Schedule Optimization Weights (0-100)
    public int ContextGroupingWeight { get; set; } = 50; // Group tasks by @context
    public int SimilarWorkGroupingWeight { get; set; } = 50; // Group by category/project
    public int EnergyMatchingWeight { get; set; } = 50; // Match task energy to time of day
    public int DueDateUrgencyWeight { get; set; } = 70; // Prioritize approaching due dates
    public int StakeholderGroupingWeight { get; set; } = 30; // Group tasks by WhoFor
    public int TagSimilarityWeight { get; set; } = 40; // Group tasks with similar tags
    public int DeepWorkPreferenceWeight { get; set; } = 60; // Prefer morning for deep work
    public int BlockedTaskPenalty { get; set; } = 100; // Deprioritize blocked tasks

    // Life Areas Balance
    public Dictionary<string, int>? LifeAreaRatings { get; set; }
    public DateTime? LifeAreaAssessmentDate { get; set; }

    // App Mode
    /// <summary>Current app mode (Work/Life/Balanced) affecting contexts and dimensions</summary>
    public AppMode AppMode { get; set; } = AppMode.Balanced;

    /// <summary>When the app mode was last changed</summary>
    public DateTime? AppModeSetAt { get; set; }

    /// <summary>Which balance dimensions the user has enabled (null = all for mode)</summary>
    public List<string>? EnabledBalanceDimensions { get; set; }

    /// <summary>Balance ratings stored per mode to preserve when switching</summary>
    public Dictionary<string, Dictionary<string, int>>? BalanceRatingsByMode { get; set; }

    // Google Calendar OAuth
    /// <summary>OAuth access token for Google Calendar API</summary>
    public string? GoogleCalendarAccessToken { get; set; }

    /// <summary>OAuth refresh token for obtaining new access tokens</summary>
    public string? GoogleCalendarRefreshToken { get; set; }

    /// <summary>When the access token expires</summary>
    public DateTime? GoogleCalendarTokenExpiry { get; set; }

    /// <summary>Connected Google account email</summary>
    public string? GoogleCalendarEmail { get; set; }

    /// <summary>IDs of calendars selected for sync</summary>
    public List<string>? GoogleCalendarSelectedCalendarIds { get; set; }

    /// <summary>How many days in the past to sync</summary>
    public int GoogleCalendarSyncPastDays { get; set; } = 7;

    /// <summary>How many days in the future to sync</summary>
    public int GoogleCalendarSyncFutureDays { get; set; } = 30;

    /// <summary>Last successful sync timestamp</summary>
    public DateTime? GoogleCalendarLastSyncTime { get; set; }
}
