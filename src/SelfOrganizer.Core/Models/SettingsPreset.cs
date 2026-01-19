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
    public required Action<UserPreferences> ApplyToPreferences { get; init; }
    public required Action<AccessibilitySettings> ApplyToAccessibility { get; init; }
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
