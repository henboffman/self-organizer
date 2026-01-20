namespace SelfOrganizer.Core.Models;

/// <summary>
/// A quick-start settings preset for specific needs
/// </summary>
public class SettingsPreset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Icon { get; init; }

    /// <summary>
    /// Searchable tags/keywords for this preset
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Human-readable descriptions of what this preset changes
    /// </summary>
    public List<SettingChange> Changes { get; init; } = new();

    public required Action<UserPreferences> ApplyToPreferences { get; init; }
    public required Action<AccessibilitySettings> ApplyToAccessibility { get; init; }

    /// <summary>
    /// Check if this preset matches a search query
    /// </summary>
    public bool MatchesSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchableText = $"{Name} {Description} {Category} {string.Join(" ", Tags)}".ToLowerInvariant();

        return terms.All(term => searchableText.Contains(term));
    }
}

/// <summary>
/// Represents a single setting change with human-readable description
/// </summary>
public class SettingChange
{
    public required string Category { get; init; }
    public required string Setting { get; init; }
    public required string Description { get; init; }
    public string? OldValueHint { get; init; } // Optional hint about default
    public required string NewValue { get; init; }
    public bool IsEnabled { get; init; } = true; // For toggle-style changes
}

/// <summary>
/// Provides predefined settings presets for various needs
/// </summary>
public static class SettingsPresets
{
    public static IReadOnlyList<SettingsPreset> All => _presets;

    private static readonly List<SettingsPreset> _presets = new()
    {
        // ===== ADHD Presets =====
        new SettingsPreset
        {
            Id = "adhd-focus",
            Name = "ADHD - Focus Mode",
            Description = "Maximize focus with timers, reduced distractions, and dopamine rewards. Great for getting things done.",
            Category = "ADHD",
            Icon = "target",
            Tags = new() { "adhd", "focus", "pomodoro", "timer", "productivity", "concentration", "distraction", "dopamine", "rewards", "streaks" },
            Changes = new()
            {
                new() { Category = "Focus Timer", Setting = "Enable Focus Timer", Description = "25-minute Pomodoro sessions with breaks", NewValue = "On (25 min)" },
                new() { Category = "Focus Timer", Setting = "Session Prompts", Description = "Set intentions before and reflect after each session", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Distraction Tracking", Description = "Log when you get distracted during focus", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Focus Streaks", Description = "Track consecutive successful focus sessions", NewValue = "On" },
                new() { Category = "Task View", Setting = "Visible Tasks", Description = "Limit visible tasks to reduce overwhelm", NewValue = "5 tasks" },
                new() { Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Elapsed time, completion estimates, hyperfocus alerts", NewValue = "On" },
                new() { Category = "Time Helpers", Setting = "Hyperfocus Alert", Description = "Remind you to take breaks after extended focus", NewValue = "90 min" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Visual rewards when completing tasks", NewValue = "On (Medium)" },
                new() { Category = "Motivation", Setting = "Micro-rewards", Description = "Small dopamine hits for progress", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Daily Goal", Description = "Set and track daily task completion goals", NewValue = "5 tasks" },
                new() { Category = "Executive Function", Setting = "Pick For Me", Description = "Button to randomly select a task when stuck", NewValue = "On" },
                new() { Category = "Executive Function", Setting = "Task Chunking", Description = "Break large tasks into 25-min chunks", NewValue = "On" },
                new() { Category = "Transitions", Setting = "Context Switch Warnings", Description = "Alert when switching between different types of work", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                // Focus Timer
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.BreakReminderMinutes = 5;
                p.LongBreakMinutes = 15;
                p.SessionsBeforeLongBreak = 4;
                p.ShowSessionIntentionPrompt = true;
                p.ShowPostSessionReflection = true;
                p.TrackDistractions = true;
                p.EnableFocusStreaks = true;
                p.AutoStartBreakTimer = true;
                p.PlayTimerCompletionSound = true;
                p.ShowTimerNotification = true;

                // Task Presentation - reduce overwhelm
                p.MaxVisibleTasks = 5;
                p.OneTaskAtATimeMode = false;
                p.HideTaskCounts = false;
                p.SimplifiedTaskView = false;

                // Time Perception
                p.EnableTimeBlindnessHelpers = true;
                p.ShowElapsedTime = true;
                p.ShowEstimatedCompletion = true;
                p.EnableHyperfocusAlerts = true;
                p.HyperfocusAlertMinutes = 90;
                p.TimeAwarenessIntervalMinutes = 30;
                p.ShowTimeSinceLastBreak = true;

                // Motivation
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 2;
                p.EnableMicroRewards = true;
                p.EnableProgressVisualization = true;
                p.ShowEncouragingMessages = true;
                p.EnableDailyGoalSetting = true;
                p.DailyTaskGoal = 5;
                p.ShowDailyProgressBar = true;

                // Executive Function
                p.EnablePickForMe = true;
                p.EnableTaskChunking = true;
                p.MaxTaskChunkMinutes = 25;
                p.SuggestTaskBreakdown = true;
                p.TaskBreakdownThresholdMinutes = 45;
                p.EnableQuickCaptureMode = true;

                // Transitions
                p.EnableTaskTransitions = true;
                p.TransitionBufferMinutes = 2;
                p.EnableContextSwitchWarnings = true;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.RespectSystem;
            }
        },

        new SettingsPreset
        {
            Id = "adhd-low-stim",
            Name = "ADHD - Low Stimulation",
            Description = "Minimal distractions, calm interface, one thing at a time. For when you need less noise.",
            Category = "ADHD",
            Icon = "moon",
            Tags = new() { "adhd", "calm", "quiet", "minimal", "low-stim", "sensory", "overwhelm", "peace", "simple", "one-task" },
            Changes = new()
            {
                new() { Category = "Task View", Setting = "One Task Mode", Description = "Show only one task at a time", NewValue = "On" },
                new() { Category = "Task View", Setting = "Visible Tasks", Description = "Maximum tasks shown when browsing", NewValue = "3 tasks" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Hide extra details and metadata", NewValue = "On" },
                new() { Category = "Task View", Setting = "Hide Counts", Description = "Don't show task/project counts", NewValue = "On" },
                new() { Category = "Task View", Setting = "Hide Due Dates", Description = "Remove due date pressure from cards", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Timer Duration", Description = "Longer sessions, fewer interruptions", NewValue = "45 min" },
                new() { Category = "Focus Timer", Setting = "Session Prompts", Description = "No prompts or reflection screens", NewValue = "Off" },
                new() { Category = "Time Helpers", Setting = "Time Pressure", Description = "Hide elapsed time and estimates", NewValue = "Off" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Very subtle completion feedback", NewValue = "Subtle" },
                new() { Category = "Motivation", Setting = "Encouraging Messages", Description = "No pop-up messages", NewValue = "Off" },
                new() { Category = "Audio", Setting = "All Sounds", Description = "Complete silence", NewValue = "Muted" },
                new() { Category = "Motion", Setting = "Animations", Description = "Reduce all motion and transitions", NewValue = "Reduced" },
            },
            ApplyToPreferences = p =>
            {
                // Focus Timer - longer sessions, fewer interruptions
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 45;
                p.BreakReminderMinutes = 10;
                p.ShowSessionIntentionPrompt = false;
                p.ShowPostSessionReflection = false;
                p.TrackDistractions = false;
                p.PlayTimerCompletionSound = false;

                // Minimal task view
                p.MaxVisibleTasks = 3;
                p.OneTaskAtATimeMode = true;
                p.HideTaskCounts = true;
                p.HideDueDatesOnCards = true;
                p.SimplifiedTaskView = true;
                p.EnableMinimalMode = true;

                // Reduce time pressure
                p.EnableTimeBlindnessHelpers = false;
                p.ShowElapsedTime = false;
                p.ShowEstimatedCompletion = false;
                p.EnableHyperfocusAlerts = false;

                // Subtle rewards
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 1; // Subtle
                p.EnableMicroRewards = false;
                p.ShowEncouragingMessages = false;
                p.ShowDailyProgressBar = false;

                // Still helpful features
                p.EnablePickForMe = true;
                p.EnableQuickCaptureMode = true;
                p.EnableGentleReminders = true;

                // Quiet notifications
                p.MuteAllSounds = true;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
                a.TooltipDelayMs = 1000;
            }
        },

        new SettingsPreset
        {
            Id = "adhd-accountability",
            Name = "ADHD - Accountability Mode",
            Description = "Structure and accountability with body doubling, commitment tracking, and morning planning.",
            Category = "ADHD",
            Icon = "check",
            Tags = new() { "adhd", "accountability", "structure", "routine", "body-doubling", "commitment", "planning", "morning", "evening", "blocker" },
            Changes = new()
            {
                new() { Category = "Focus Timer", Setting = "Pause on Tab Switch", Description = "Timer pauses when you leave the app", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Session Prompts", Description = "Set intentions and reflect on sessions", NewValue = "On" },
                new() { Category = "Accountability", Setting = "Body Doubling", Description = "Virtual co-working presence indicator", NewValue = "On" },
                new() { Category = "Accountability", Setting = "Commitment Mode", Description = "Must complete or explicitly abandon tasks", NewValue = "On" },
                new() { Category = "Accountability", Setting = "Abandon Reason", Description = "Explain why when abandoning a task", NewValue = "Required" },
                new() { Category = "Structure", Setting = "Morning Planning", Description = "Daily planning prompt at 9 AM", NewValue = "On" },
                new() { Category = "Structure", Setting = "Evening Wind-down", Description = "End-of-day reminder at 5 PM", NewValue = "On" },
                new() { Category = "Structure", Setting = "Blocker Prompts", Description = "Ask what's blocking stuck tasks", NewValue = "On" },
                new() { Category = "Progress", Setting = "Daily Progress Bar", Description = "Visual progress toward daily goal", NewValue = "On" },
                new() { Category = "Progress", Setting = "Weekly Summary", Description = "Achievement summary each week", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                // Focus with accountability
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.PauseTimerOnWindowBlur = true; // Accountability
                p.ShowSessionIntentionPrompt = true;
                p.ShowPostSessionReflection = true;
                p.TrackDistractions = true;

                // Accountability features
                p.EnableBodyDoubling = true;
                p.EnableCommitmentMode = true;
                p.RequireAbandonReason = true;
                p.EnableMorningPlanningPrompt = true;
                p.MorningPlanningHour = 9;
                p.EnableEveningWindDown = true;
                p.EveningWindDownHour = 17;

                // Blocker support
                p.EnableBlockerPrompts = true;
                p.StaleTakPromptDays = 2;

                // Visible progress
                p.EnableProgressVisualization = true;
                p.ShowDailyProgressBar = true;
                p.EnableDailyGoalSetting = true;
                p.DailyTaskGoal = 5;
                p.EnableWeeklySummary = true;

                // Structure
                p.EnableTaskTransitions = true;
                p.TransitionBufferMinutes = 3;
            },
            ApplyToAccessibility = a => { }
        },

        // ===== Anxiety Presets =====
        new SettingsPreset
        {
            Id = "anxiety-calm",
            Name = "Anxiety - Calm & Gentle",
            Description = "Reduce overwhelm with hidden counts, gentle reminders, and no time pressure.",
            Category = "Anxiety",
            Icon = "cloud",
            Tags = new() { "anxiety", "calm", "gentle", "stress", "overwhelm", "pressure", "peaceful", "soothing", "no-rush", "forgiving" },
            Changes = new()
            {
                new() { Category = "Task View", Setting = "Hide Task Counts", Description = "Don't show how many tasks remain", NewValue = "On" },
                new() { Category = "Task View", Setting = "Hide Due Dates", Description = "Remove deadline pressure from cards", NewValue = "On" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Clean, minimal task presentation", NewValue = "On" },
                new() { Category = "Time Pressure", Setting = "Time Displays", Description = "Hide elapsed time and estimates", NewValue = "Off" },
                new() { Category = "Time Pressure", Setting = "Daily Goals", Description = "No pressure to hit task targets", NewValue = "Off" },
                new() { Category = "Time Pressure", Setting = "Grace Period", Description = "15 minutes before task is 'late'", NewValue = "15 min" },
                new() { Category = "Support", Setting = "Gentle Reminders", Description = "Soft, non-urgent nudges", NewValue = "On" },
                new() { Category = "Support", Setting = "Encouraging Messages", Description = "Positive affirmations", NewValue = "On" },
                new() { Category = "Support", Setting = "Pick For Me", Description = "Let the app choose when stuck", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Subtle rewards for completion", NewValue = "Subtle" },
                new() { Category = "Audio", Setting = "All Sounds", Description = "Silent operation", NewValue = "Muted" },
                new() { Category = "Motion", Setting = "Animations", Description = "Reduce movement for calm", NewValue = "Reduced" },
            },
            ApplyToPreferences = p =>
            {
                // Hide anxiety-inducing elements
                p.HideTaskCounts = true;
                p.HideDueDatesOnCards = true;
                p.MaxVisibleTasks = 5;
                p.SimplifiedTaskView = true;

                // Remove time pressure
                p.ShowElapsedTime = false;
                p.ShowEstimatedCompletion = false;
                p.EnableTimeBlindnessHelpers = false;
                p.ShowTimeRemainingInBlock = false;
                p.EnableHyperfocusAlerts = false;

                // Gentle approach
                p.EnableGentleReminders = true;
                p.ShowEncouragingMessages = true;
                p.TaskStartGracePeriod = 15; // Long grace period

                // Reduce pressure to perform
                p.EnableDailyGoalSetting = false;
                p.ShowDailyProgressBar = false;
                p.EnableCommitmentMode = false;
                p.EnableBlockerPrompts = false;

                // Still celebrate wins
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 1; // Subtle
                p.EnableMicroRewards = true;

                // Easy task management
                p.EnablePickForMe = true;
                p.EnableQuickCaptureMode = true;
                p.SuggestTaskBreakdown = true;
                p.TaskBreakdownThresholdMinutes = 30;

                // Quiet
                p.MuteAllSounds = true;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
                a.TooltipDelayMs = 1500;
            }
        },

        new SettingsPreset
        {
            Id = "anxiety-structured",
            Name = "Anxiety - Structured Routine",
            Description = "Predictable routines and clear structure to reduce uncertainty.",
            Category = "Anxiety",
            Icon = "calendar",
            Tags = new() { "anxiety", "structure", "routine", "predictable", "schedule", "planning", "certainty", "organized", "consistent" },
            Changes = new()
            {
                new() { Category = "Structure", Setting = "Morning Planning", Description = "Start each day with a plan at 8 AM", NewValue = "On" },
                new() { Category = "Structure", Setting = "Evening Wind-down", Description = "Closure routine at 6 PM", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Predictable 25-min work, 5-min break", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Auto-start Breaks", Description = "Consistent break timing", NewValue = "On" },
                new() { Category = "Task View", Setting = "Hide Counts", Description = "Reduce number anxiety", NewValue = "On" },
                new() { Category = "Goals", Setting = "Daily Goal", Description = "Achievable target of 3 tasks", NewValue = "3 tasks" },
                new() { Category = "Support", Setting = "Gentle Reminders", Description = "Soft prompts to stay on track", NewValue = "On" },
                new() { Category = "Support", Setting = "Task Transitions", Description = "Buffer time between tasks", NewValue = "5 min" },
                new() { Category = "Progress", Setting = "Progress Bar", Description = "Visual sense of accomplishment", NewValue = "On" },
                new() { Category = "Motion", Setting = "Animations", Description = "Calm, reduced motion", NewValue = "Reduced" },
            },
            ApplyToPreferences = p =>
            {
                // Clear structure
                p.EnableMorningPlanningPrompt = true;
                p.MorningPlanningHour = 8;
                p.EnableEveningWindDown = true;
                p.EveningWindDownHour = 18;
                p.EnableDailyGoalSetting = true;
                p.DailyTaskGoal = 3; // Achievable goal

                // Predictable timer
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.BreakReminderMinutes = 5;
                p.AutoStartBreakTimer = true;

                // Visible but not overwhelming
                p.MaxVisibleTasks = 5;
                p.HideTaskCounts = true;
                p.SimplifiedTaskView = false;

                // Supportive features
                p.EnableGentleReminders = true;
                p.ShowEncouragingMessages = true;
                p.EnableTaskTransitions = true;
                p.TransitionBufferMinutes = 5;

                // Progress without pressure
                p.EnableProgressVisualization = true;
                p.ShowDailyProgressBar = true;
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 1;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
            }
        },

        // ===== Depression Presets =====
        new SettingsPreset
        {
            Id = "depression-gentle",
            Name = "Depression - Gentle Start",
            Description = "Low-pressure environment with encouragement and small wins. Every step counts.",
            Category = "Depression",
            Icon = "heart",
            Tags = new() { "depression", "gentle", "low-energy", "encouragement", "small-wins", "compassion", "self-care", "forgiving", "easy", "kind" },
            Changes = new()
            {
                new() { Category = "Task View", Setting = "One Task Mode", Description = "Focus on just one thing", NewValue = "On" },
                new() { Category = "Task View", Setting = "Visible Tasks", Description = "Show only 3 tasks at a time", NewValue = "3 tasks" },
                new() { Category = "Task View", Setting = "Hide Counts", Description = "No overwhelming numbers", NewValue = "On" },
                new() { Category = "Task View", Setting = "Hide Due Dates", Description = "Remove deadline pressure", NewValue = "On" },
                new() { Category = "Goals", Setting = "Daily Goal", Description = "Just complete one thing", NewValue = "1 task" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Extra celebration for every win", NewValue = "Extra" },
                new() { Category = "Motivation", Setting = "Micro-rewards", Description = "Small wins feel good", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive affirmations", NewValue = "On" },
                new() { Category = "Support", Setting = "Gentle Reminders", Description = "Soft, compassionate nudges", NewValue = "On" },
                new() { Category = "Support", Setting = "Grace Period", Description = "30 minutes before 'late'", NewValue = "30 min" },
                new() { Category = "Support", Setting = "Pick For Me", Description = "Decision support when stuck", NewValue = "On" },
                new() { Category = "Pressure", Setting = "Commitment Mode", Description = "No forced completion", NewValue = "Off" },
                new() { Category = "Focus Timer", Setting = "Timer", Description = "Optional, not required", NewValue = "Off (optional 10 min)" },
            },
            ApplyToPreferences = p =>
            {
                // Very low pressure
                p.MaxVisibleTasks = 3;
                p.OneTaskAtATimeMode = true;
                p.HideTaskCounts = true;
                p.HideDueDatesOnCards = true;
                p.SimplifiedTaskView = true;

                // No time pressure
                p.ShowElapsedTime = false;
                p.ShowEstimatedCompletion = false;
                p.EnableTimeBlindnessHelpers = false;
                p.EnableHyperfocusAlerts = false;

                // Lots of encouragement
                p.ShowEncouragingMessages = true;
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 3; // Extra celebration!
                p.EnableMicroRewards = true;

                // Very small achievable goals
                p.EnableDailyGoalSetting = true;
                p.DailyTaskGoal = 1; // Just one thing
                p.ShowDailyProgressBar = true;

                // Gentle approach
                p.EnableGentleReminders = true;
                p.TaskStartGracePeriod = 30; // Very forgiving

                // Make starting easy
                p.EnablePickForMe = true;
                p.EnableQuickCaptureMode = true;

                // No pressure features
                p.EnableCommitmentMode = false;
                p.EnableBlockerPrompts = false;
                p.EnableMorningPlanningPrompt = false;

                // Optional short focus
                p.EnableFocusTimer = false; // Opt-in
                p.FocusTimerMinutes = 10; // Very short if used
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.RespectSystem;
            }
        },

        new SettingsPreset
        {
            Id = "depression-momentum",
            Name = "Depression - Building Momentum",
            Description = "Gamified progress tracking to build momentum. Streaks, rewards, and visible progress.",
            Category = "Depression",
            Icon = "graph",
            Tags = new() { "depression", "momentum", "progress", "gamification", "streaks", "rewards", "motivation", "achievement", "visible-progress" },
            Changes = new()
            {
                new() { Category = "Progress", Setting = "Progress Visualization", Description = "See your progress visually", NewValue = "On" },
                new() { Category = "Progress", Setting = "Daily Progress Bar", Description = "Track daily completion", NewValue = "On" },
                new() { Category = "Progress", Setting = "Weekly Summary", Description = "Celebrate weekly achievements", NewValue = "On" },
                new() { Category = "Progress", Setting = "Focus Streaks", Description = "Build consecutive session streaks", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Big celebrations for wins", NewValue = "Extra" },
                new() { Category = "Motivation", Setting = "Micro-rewards", Description = "Frequent small rewards", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive reinforcement", NewValue = "On" },
                new() { Category = "Goals", Setting = "Daily Goal", Description = "Achievable 3-task target", NewValue = "3 tasks" },
                new() { Category = "Focus Timer", Setting = "Timer", Description = "Short 15-min sessions", NewValue = "15 min" },
                new() { Category = "Support", Setting = "Pick For Me", Description = "Help with decisions", NewValue = "On" },
                new() { Category = "Support", Setting = "Task Breakdown", Description = "Suggest smaller subtasks", NewValue = "20 min threshold" },
            },
            ApplyToPreferences = p =>
            {
                // Visible progress
                p.EnableProgressVisualization = true;
                p.ShowDailyProgressBar = true;
                p.EnableWeeklySummary = true;
                p.EnableFocusStreaks = true;

                // Lots of rewards
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 3;
                p.EnableMicroRewards = true;
                p.ShowEncouragingMessages = true;

                // Achievable goals
                p.EnableDailyGoalSetting = true;
                p.DailyTaskGoal = 3;

                // Short focus sessions
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 15;
                p.BreakReminderMinutes = 5;
                p.ShowSessionIntentionPrompt = true;

                // Manageable task view
                p.MaxVisibleTasks = 5;
                p.HideTaskCounts = false; // See progress
                p.SimplifiedTaskView = false;

                // Support features
                p.EnablePickForMe = true;
                p.SuggestTaskBreakdown = true;
                p.TaskBreakdownThresholdMinutes = 20;
                p.EnableQuickCaptureMode = true;
            },
            ApplyToAccessibility = a => { }
        },

        // ===== Autism Presets =====
        new SettingsPreset
        {
            Id = "autism-sensory",
            Name = "Autism - Sensory Friendly",
            Description = "Reduced sensory input with no animations, muted sounds, and clean interface.",
            Category = "Autism",
            Icon = "eye",
            Tags = new() { "autism", "sensory", "asd", "quiet", "clean", "minimal", "no-animation", "predictable", "overstimulation" },
            Changes = new()
            {
                new() { Category = "Audio", Setting = "All Sounds", Description = "Complete silence", NewValue = "Muted" },
                new() { Category = "Motion", Setting = "Animations", Description = "No motion or transitions", NewValue = "Always Reduced" },
                new() { Category = "Visual", Setting = "Celebrations", Description = "No visual effects", NewValue = "Off" },
                new() { Category = "Visual", Setting = "Micro-rewards", Description = "No popup rewards", NewValue = "Off" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Clean, minimal interface", NewValue = "On" },
                new() { Category = "Task View", Setting = "Minimal Mode", Description = "Hide non-essential elements", NewValue = "On" },
                new() { Category = "Notifications", Setting = "Encouraging Messages", Description = "No surprise popups", NewValue = "Off" },
                new() { Category = "Notifications", Setting = "Timer Notification", Description = "No popup alerts", NewValue = "Off" },
                new() { Category = "Structure", Setting = "Focus Timer", Description = "Predictable work intervals", NewValue = "On (25 min)" },
                new() { Category = "Time Helpers", Setting = "Time Displays", Description = "Clear elapsed/remaining time", NewValue = "On" },
                new() { Category = "Accessibility", Setting = "Link Underlines", Description = "Always visible link indicators", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                // Quiet and calm
                p.MuteAllSounds = true;
                p.EnableCelebrationEffects = false;
                p.EnableMicroRewards = false;

                // Clean interface
                p.SimplifiedTaskView = true;
                p.EnableMinimalMode = true;
                p.MaxVisibleTasks = 5;

                // No surprises
                p.ShowEncouragingMessages = false;
                p.EnableGentleReminders = false;
                p.ShowTimerNotification = false;

                // Predictable structure
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.AutoStartBreakTimer = true;

                // Clear time awareness
                p.ShowElapsedTime = true;
                p.ShowEstimatedCompletion = true;
                p.ShowTimeRemainingInBlock = true;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
                a.TooltipDelayMs = 1000;
                a.AlwaysShowLinkUnderlines = true;
            }
        },

        new SettingsPreset
        {
            Id = "autism-routine",
            Name = "Autism - Routine Focused",
            Description = "Strong routine support with structured days and predictable transitions.",
            Category = "Autism",
            Icon = "list",
            Tags = new() { "autism", "routine", "asd", "structure", "predictable", "schedule", "transitions", "consistency", "planning" },
            Changes = new()
            {
                new() { Category = "Structure", Setting = "Morning Planning", Description = "Daily planning prompt at 8 AM", NewValue = "On" },
                new() { Category = "Structure", Setting = "Evening Wind-down", Description = "Closure routine at 6 PM", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Consistent 25/5/15 min pattern", NewValue = "On" },
                new() { Category = "Focus Timer", Setting = "Auto-start Breaks", Description = "Predictable break timing", NewValue = "On" },
                new() { Category = "Transitions", Setting = "Task Transitions", Description = "5-min buffer between tasks", NewValue = "On (5 min)" },
                new() { Category = "Transitions", Setting = "Context Switch Warnings", Description = "Alert before work type changes", NewValue = "On" },
                new() { Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Time awareness throughout day", NewValue = "On" },
                new() { Category = "Time Helpers", Setting = "Ambient Time Display", Description = "Always-visible time indicator", NewValue = "On" },
                new() { Category = "Progress", Setting = "Progress Visualization", Description = "Track routine completion", NewValue = "On" },
                new() { Category = "Audio", Setting = "All Sounds", Description = "Silent operation", NewValue = "Muted" },
                new() { Category = "Visual", Setting = "Celebrations", Description = "No surprise animations", NewValue = "Off" },
                new() { Category = "Motion", Setting = "Animations", Description = "Reduced motion", NewValue = "Always Reduced" },
            },
            ApplyToPreferences = p =>
            {
                // Strong routine
                p.EnableMorningPlanningPrompt = true;
                p.MorningPlanningHour = 8;
                p.EnableEveningWindDown = true;
                p.EveningWindDownHour = 18;

                // Predictable timer
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.BreakReminderMinutes = 5;
                p.LongBreakMinutes = 15;
                p.SessionsBeforeLongBreak = 4;
                p.AutoStartBreakTimer = true;

                // Clear transitions
                p.EnableTaskTransitions = true;
                p.TransitionBufferMinutes = 5;
                p.EnableContextSwitchWarnings = true;

                // Time awareness
                p.EnableTimeBlindnessHelpers = true;
                p.ShowElapsedTime = true;
                p.ShowEstimatedCompletion = true;
                p.EnableAmbientTimeDisplay = true;
                p.TimeAwarenessIntervalMinutes = 30;

                // Structured view
                p.MaxVisibleTasks = 7;
                p.SimplifiedTaskView = false;
                p.EnableProgressVisualization = true;

                // Quiet
                p.MuteAllSounds = true;
                p.EnableCelebrationEffects = false;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
            }
        },

        // ===== Dyslexia Presets =====
        new SettingsPreset
        {
            Id = "dyslexia-reading",
            Name = "Dyslexia - Reading Optimized",
            Description = "Dyslexia-friendly fonts, increased spacing, and visual aids for better reading.",
            Category = "Dyslexia",
            Icon = "book",
            Tags = new() { "dyslexia", "reading", "font", "spacing", "text", "accessibility", "opendyslexic", "visibility", "legibility" },
            Changes = new()
            {
                new() { Category = "Reading", Setting = "Dyslexia-Friendly Font", Description = "OpenDyslexic or similar font", NewValue = "On" },
                new() { Category = "Reading", Setting = "Line Height", Description = "Extra space between lines", NewValue = "1.8x" },
                new() { Category = "Reading", Setting = "Letter Spacing", Description = "Wider letter spacing", NewValue = "0.12em" },
                new() { Category = "Reading", Setting = "Text Size", Description = "Larger default text", NewValue = "110%" },
                new() { Category = "Visual", Setting = "Link Underlines", Description = "Always visible link indicators", NewValue = "On" },
                new() { Category = "Visual", Setting = "Focus Indicator", Description = "Larger focus outlines", NewValue = "3px" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Less text to process", NewValue = "On" },
                new() { Category = "Progress", Setting = "Progress Visualization", Description = "Visual instead of text-based", NewValue = "On" },
                new() { Category = "Progress", Setting = "Progress Bar", Description = "Visual daily progress", NewValue = "On" },
                new() { Category = "Audio", Setting = "Timer Sound", Description = "Audio cues for completion", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                // Simple visual presentation
                p.SimplifiedTaskView = true;
                p.MaxVisibleTasks = 5;

                // Visual progress
                p.EnableProgressVisualization = true;
                p.ShowDailyProgressBar = true;

                // Audio feedback option
                p.PlayTimerCompletionSound = true;
                p.ShowTimerNotification = true;
            },
            ApplyToAccessibility = a =>
            {
                a.DyslexiaFriendlyFont = true;
                a.LineHeightMultiplier = 1.8;
                a.LetterSpacing = 0.12;
                a.TextScalingPercent = 110;
                a.AlwaysShowLinkUnderlines = true;
                a.FocusIndicatorSize = 3;
            }
        },

        // ===== Vision Presets =====
        new SettingsPreset
        {
            Id = "low-vision",
            Name = "Low Vision - High Visibility",
            Description = "Large text, high contrast, and enhanced focus indicators for better visibility.",
            Category = "Vision",
            Icon = "zoom-in",
            Tags = new() { "vision", "low-vision", "large-text", "high-contrast", "visibility", "accessibility", "magnification", "sight" },
            Changes = new()
            {
                new() { Category = "Vision", Setting = "High Contrast Mode", Description = "Maximum color contrast", NewValue = "On" },
                new() { Category = "Vision", Setting = "Text Size", Description = "50% larger text", NewValue = "150%" },
                new() { Category = "Vision", Setting = "Line Height", Description = "Extra line spacing", NewValue = "1.6x" },
                new() { Category = "Vision", Setting = "Focus Indicator", Description = "Large, visible focus outlines", NewValue = "4px" },
                new() { Category = "Vision", Setting = "Larger Click Targets", Description = "Bigger buttons and links", NewValue = "On" },
                new() { Category = "Vision", Setting = "Link Underlines", Description = "Always visible links", NewValue = "On" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Cleaner, easier to see", NewValue = "On" },
                new() { Category = "Audio", Setting = "Timer Sound", Description = "Audio feedback", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                p.SimplifiedTaskView = true;
                p.MaxVisibleTasks = 5;

                // Audio feedback
                p.PlayTimerCompletionSound = true;
            },
            ApplyToAccessibility = a =>
            {
                a.HighContrastMode = true;
                a.TextScalingPercent = 150;
                a.LineHeightMultiplier = 1.6;
                a.FocusIndicatorSize = 4;
                a.LargerClickTargets = true;
                a.AlwaysShowLinkUnderlines = true;
            }
        },

        new SettingsPreset
        {
            Id = "colorblind-deuteranopia",
            Name = "Color Blind - Red-Green (Deuteranopia)",
            Description = "Optimized colors for deuteranopia (green-blind) color vision.",
            Category = "Vision",
            Icon = "aperture",
            Tags = new() { "colorblind", "color-blind", "deuteranopia", "green-blind", "red-green", "vision", "accessibility", "cvd" },
            Changes = new()
            {
                new() { Category = "Vision", Setting = "Color Mode", Description = "Deuteranopia-safe color palette", NewValue = "Deuteranopia" },
                new() { Category = "Vision", Setting = "Link Underlines", Description = "Non-color link indicators", NewValue = "On" },
            },
            ApplyToPreferences = p => { },
            ApplyToAccessibility = a =>
            {
                a.ColorBlindnessMode = ColorBlindnessMode.Deuteranopia;
                a.AlwaysShowLinkUnderlines = true;
            }
        },

        new SettingsPreset
        {
            Id = "colorblind-protanopia",
            Name = "Color Blind - Red-Green (Protanopia)",
            Description = "Optimized colors for protanopia (red-blind) color vision.",
            Category = "Vision",
            Icon = "aperture",
            Tags = new() { "colorblind", "color-blind", "protanopia", "red-blind", "red-green", "vision", "accessibility", "cvd" },
            Changes = new()
            {
                new() { Category = "Vision", Setting = "Color Mode", Description = "Protanopia-safe color palette", NewValue = "Protanopia" },
                new() { Category = "Vision", Setting = "Link Underlines", Description = "Non-color link indicators", NewValue = "On" },
            },
            ApplyToPreferences = p => { },
            ApplyToAccessibility = a =>
            {
                a.ColorBlindnessMode = ColorBlindnessMode.Protanopia;
                a.AlwaysShowLinkUnderlines = true;
            }
        },

        // ===== Motor/Physical Presets =====
        new SettingsPreset
        {
            Id = "motor-accessibility",
            Name = "Motor - Easy Interaction",
            Description = "Larger click targets and reduced precision requirements for easier interaction.",
            Category = "Motor",
            Icon = "move",
            Tags = new() { "motor", "physical", "dexterity", "click-targets", "tremor", "accessibility", "ease-of-use", "mobility" },
            Changes = new()
            {
                new() { Category = "Interaction", Setting = "Larger Click Targets", Description = "Bigger buttons and links", NewValue = "On" },
                new() { Category = "Interaction", Setting = "Focus Indicator", Description = "Large, visible focus outlines", NewValue = "4px" },
                new() { Category = "Interaction", Setting = "Tooltip Delay", Description = "More time before tooltips appear", NewValue = "1000ms" },
                new() { Category = "Task View", Setting = "Simplified View", Description = "Fewer small elements to click", NewValue = "On" },
                new() { Category = "Motion", Setting = "Animations", Description = "Reduced motion", NewValue = "Always Reduced" },
            },
            ApplyToPreferences = p =>
            {
                p.SimplifiedTaskView = true;
            },
            ApplyToAccessibility = a =>
            {
                a.LargerClickTargets = true;
                a.FocusIndicatorSize = 4;
                a.TooltipDelayMs = 1000;
                a.ReducedMotionMode = ReducedMotionMode.AlwaysReduce;
            }
        },

        // ===== Combination Presets =====
        new SettingsPreset
        {
            Id = "balanced-neurodivergent",
            Name = "Balanced Neurodivergent",
            Description = "A balanced starting point with helpful features for various neurodivergent needs.",
            Category = "General",
            Icon = "dial",
            Tags = new() { "balanced", "neurodivergent", "general", "starter", "default", "moderate", "flexible", "all-purpose" },
            Changes = new()
            {
                new() { Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Standard 25-min sessions", NewValue = "On (25 min)" },
                new() { Category = "Focus Timer", Setting = "Session Intentions", Description = "Set focus before starting", NewValue = "On" },
                new() { Category = "Task View", Setting = "Visible Tasks", Description = "Balanced task visibility", NewValue = "7 tasks" },
                new() { Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Time awareness helpers", NewValue = "On" },
                new() { Category = "Time Helpers", Setting = "Completion Estimates", Description = "Show when you'll finish", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Celebrations", Description = "Medium celebrations", NewValue = "On (Medium)" },
                new() { Category = "Motivation", Setting = "Micro-rewards", Description = "Small progress rewards", NewValue = "On" },
                new() { Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive reinforcement", NewValue = "On" },
                new() { Category = "Support", Setting = "Pick For Me", Description = "Decision help when stuck", NewValue = "On" },
                new() { Category = "Support", Setting = "Gentle Reminders", Description = "Soft nudges", NewValue = "On" },
                new() { Category = "Support", Setting = "Task Breakdown", Description = "Suggest smaller subtasks", NewValue = "On" },
                new() { Category = "Transitions", Setting = "Task Transitions", Description = "Buffer between tasks", NewValue = "On" },
                new() { Category = "Motion", Setting = "Animations", Description = "Respect system preference", NewValue = "System" },
                new() { Category = "Visual", Setting = "Link Underlines", Description = "Visible link indicators", NewValue = "On" },
            },
            ApplyToPreferences = p =>
            {
                // Moderate focus support
                p.EnableFocusTimer = true;
                p.FocusTimerMinutes = 25;
                p.BreakReminderMinutes = 5;
                p.ShowSessionIntentionPrompt = true;

                // Moderate task visibility
                p.MaxVisibleTasks = 7;
                p.HideTaskCounts = false;
                p.SimplifiedTaskView = false;

                // Time awareness without pressure
                p.EnableTimeBlindnessHelpers = true;
                p.ShowEstimatedCompletion = true;

                // Encouraging but not overwhelming
                p.EnableCelebrationEffects = true;
                p.CelebrationIntensity = 2;
                p.EnableMicroRewards = true;
                p.ShowEncouragingMessages = true;
                p.EnableProgressVisualization = true;

                // Helpful features
                p.EnablePickForMe = true;
                p.EnableQuickCaptureMode = true;
                p.SuggestTaskBreakdown = true;
                p.EnableGentleReminders = true;
                p.EnableTaskTransitions = true;
            },
            ApplyToAccessibility = a =>
            {
                a.ReducedMotionMode = ReducedMotionMode.RespectSystem;
                a.AlwaysShowLinkUnderlines = true;
            }
        }
    };

    /// <summary>
    /// Get presets by category
    /// </summary>
    public static IEnumerable<SettingsPreset> GetByCategory(string category)
        => _presets.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get all unique categories
    /// </summary>
    public static IEnumerable<string> GetCategories()
        => _presets.Select(p => p.Category).Distinct();

    /// <summary>
    /// Get a preset by ID
    /// </summary>
    public static SettingsPreset? GetById(string id)
        => _presets.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
