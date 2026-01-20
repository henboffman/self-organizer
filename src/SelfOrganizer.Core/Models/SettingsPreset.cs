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

    /// <summary>
    /// Detailed explanation of what this setting does
    /// </summary>
    public string? Tooltip { get; init; }

    /// <summary>
    /// Explains why this setting specifically helps with the condition/profile
    /// </summary>
    public string? WhyItHelps { get; init; }
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
                new() {
                    Category = "Focus Timer", Setting = "Enable Focus Timer", Description = "25-minute Pomodoro sessions with breaks", NewValue = "On (25 min)",
                    Tooltip = "The Pomodoro Technique uses timed work intervals (typically 25 minutes) followed by short breaks to maintain focus and prevent mental fatigue.",
                    WhyItHelps = "External time structure compensates for difficulty with self-regulation. The defined endpoint makes starting easier and prevents the 'infinite task' feeling."
                },
                new() {
                    Category = "Focus Timer", Setting = "Session Prompts", Description = "Set intentions before and reflect after each session", NewValue = "On",
                    Tooltip = "Before each focus session, you'll be prompted to state what you want to accomplish. Afterward, you can note what worked and what didn't.",
                    WhyItHelps = "Externalizing intentions creates accountability and helps engage the prefrontal cortex. Reflection builds metacognitive awareness over time."
                },
                new() {
                    Category = "Focus Timer", Setting = "Distraction Tracking", Description = "Log when you get distracted during focus", NewValue = "On",
                    Tooltip = "During focus sessions, quickly log distractions with a single click. Review patterns later to identify your common triggers.",
                    WhyItHelps = "Awareness of distraction patterns is the first step to managing them. This creates data for understanding your unique focus challenges."
                },
                new() {
                    Category = "Focus Timer", Setting = "Focus Streaks", Description = "Track consecutive successful focus sessions", NewValue = "On",
                    Tooltip = "Build streaks by completing focus sessions back-to-back. Visual streak counter provides motivation to maintain consistency.",
                    WhyItHelps = "Streaks leverage the dopamine reward system—maintaining a streak becomes intrinsically motivating and makes breaking it feel costly."
                },
                new() {
                    Category = "Task View", Setting = "Visible Tasks", Description = "Limit visible tasks to reduce overwhelm", NewValue = "5 tasks",
                    Tooltip = "Only show a limited number of tasks at once. The full list is still there, but hidden until you're ready.",
                    WhyItHelps = "Reduces choice paralysis and overwhelm. Seeing 50 tasks triggers avoidance; seeing 5 feels manageable and actionable."
                },
                new() {
                    Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Elapsed time, completion estimates, hyperfocus alerts", NewValue = "On",
                    Tooltip = "Shows how long you've been working, estimates when you'll finish, and alerts you during extended sessions.",
                    WhyItHelps = "Time blindness (difficulty perceiving time passing) is common with ADHD. External time cues act as a 'prosthetic' for your internal clock."
                },
                new() {
                    Category = "Time Helpers", Setting = "Hyperfocus Alert", Description = "Remind you to take breaks after extended focus", NewValue = "90 min",
                    Tooltip = "Get a gentle alert after 90 minutes of continuous work, reminding you to take a break and check in with yourself.",
                    WhyItHelps = "Hyperfocus can lead to neglecting basic needs. This provides an external check when your internal sense of time is unreliable."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Visual rewards when completing tasks", NewValue = "On (Medium)",
                    Tooltip = "Completing tasks triggers celebratory animations and effects. Intensity can be adjusted from subtle to extra.",
                    WhyItHelps = "Provides immediate external reward that the brain craves. Bridges the gap when internal satisfaction isn't enough to motivate action."
                },
                new() {
                    Category = "Motivation", Setting = "Micro-rewards", Description = "Small dopamine hits for progress", NewValue = "On",
                    Tooltip = "Small positive feedback for incremental progress—not just task completion, but steps along the way.",
                    WhyItHelps = "Creates a reward gradient rather than all-or-nothing. Keeps dopamine flowing during the task, not just at completion."
                },
                new() {
                    Category = "Motivation", Setting = "Daily Goal", Description = "Set and track daily task completion goals", NewValue = "5 tasks",
                    Tooltip = "Set a target number of tasks to complete each day and track your progress toward that goal.",
                    WhyItHelps = "Transforms vague 'be productive' into concrete 'complete 5 tasks.' Clear targets are easier to pursue than abstract goals."
                },
                new() {
                    Category = "Executive Function", Setting = "Pick For Me", Description = "Button to randomly select a task when stuck", NewValue = "On",
                    Tooltip = "When you can't decide what to do, this button randomly selects a task for you. Removes the burden of choice.",
                    WhyItHelps = "Decision paralysis is a common executive function challenge. Outsourcing the decision removes a major barrier to starting."
                },
                new() {
                    Category = "Executive Function", Setting = "Task Chunking", Description = "Break large tasks into 25-min chunks", NewValue = "On",
                    Tooltip = "Automatically suggests breaking tasks estimated over 25 minutes into smaller subtasks.",
                    WhyItHelps = "Large tasks feel overwhelming and trigger procrastination. Smaller chunks feel achievable and match focus session length."
                },
                new() {
                    Category = "Transitions", Setting = "Context Switch Warnings", Description = "Alert when switching between different types of work", NewValue = "On",
                    Tooltip = "Get a heads-up when you're about to switch from one type of work to another (e.g., creative to administrative).",
                    WhyItHelps = "Context switching has a higher cognitive cost for ADHD brains. Warnings help you prepare mentally for the shift."
                },
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
                new() {
                    Category = "Task View", Setting = "One Task Mode", Description = "Show only one task at a time", NewValue = "On",
                    Tooltip = "Displays only your current task. Other tasks are hidden until you complete or skip this one.",
                    WhyItHelps = "Eliminates visual competition for attention. When there's only one option, deciding what to work on becomes effortless."
                },
                new() {
                    Category = "Task View", Setting = "Visible Tasks", Description = "Maximum tasks shown when browsing", NewValue = "3 tasks",
                    Tooltip = "When browsing tasks, only 3 are shown at once. Scroll or filter to see more.",
                    WhyItHelps = "Minimizes cognitive load and visual stimulation. Fewer items means less mental processing required."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Hide extra details and metadata", NewValue = "On",
                    Tooltip = "Shows only task titles and essential info. Hides tags, dates, estimates, and other metadata.",
                    WhyItHelps = "Reduces visual noise and information overload. A cleaner interface requires less mental filtering."
                },
                new() {
                    Category = "Task View", Setting = "Hide Counts", Description = "Don't show task/project counts", NewValue = "On",
                    Tooltip = "Hides numbers like '47 tasks remaining' from the interface.",
                    WhyItHelps = "Large numbers can trigger anxiety and overwhelm. Hiding them lets you focus on the task at hand, not the mountain."
                },
                new() {
                    Category = "Task View", Setting = "Hide Due Dates", Description = "Remove due date pressure from cards", NewValue = "On",
                    Tooltip = "Task cards won't display due dates. You can still see them in task details if needed.",
                    WhyItHelps = "Removes constant time pressure reminders. Allows focus on the work itself rather than looming deadlines."
                },
                new() {
                    Category = "Focus Timer", Setting = "Timer Duration", Description = "Longer sessions, fewer interruptions", NewValue = "45 min",
                    Tooltip = "Extended 45-minute focus sessions instead of standard 25 minutes.",
                    WhyItHelps = "Fewer transitions means less stimulation from stopping/starting. Once focused, staying in flow is easier than re-entering it."
                },
                new() {
                    Category = "Focus Timer", Setting = "Session Prompts", Description = "No prompts or reflection screens", NewValue = "Off",
                    Tooltip = "Timer starts and ends quietly. No intention-setting or reflection prompts.",
                    WhyItHelps = "Removes extra interaction requirements. Simpler workflow = less stimulation and fewer decisions."
                },
                new() {
                    Category = "Time Helpers", Setting = "Time Pressure", Description = "Hide elapsed time and estimates", NewValue = "Off",
                    Tooltip = "Hides time-related information like elapsed time and completion estimates.",
                    WhyItHelps = "Time awareness can add stress. Some brains work better without constant reminders of time passing."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Very subtle completion feedback", NewValue = "Subtle",
                    Tooltip = "Task completion shows minimal visual feedback—a simple checkmark rather than animations.",
                    WhyItHelps = "Provides confirmation without overwhelming visual input. Acknowledges progress quietly."
                },
                new() {
                    Category = "Motivation", Setting = "Encouraging Messages", Description = "No pop-up messages", NewValue = "Off",
                    Tooltip = "Disables motivational pop-up messages that appear during use.",
                    WhyItHelps = "Unexpected pop-ups can feel jarring. A predictable, quiet interface reduces sensory surprises."
                },
                new() {
                    Category = "Audio", Setting = "All Sounds", Description = "Complete silence", NewValue = "Muted",
                    Tooltip = "All app sounds are disabled—no notifications, no timer sounds, no feedback sounds.",
                    WhyItHelps = "Eliminates auditory stimulation entirely. Audio-sensitive individuals can work in complete silence."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Reduce all motion and transitions", NewValue = "Reduced",
                    Tooltip = "Disables or minimizes all animations, transitions, and moving elements.",
                    WhyItHelps = "Motion can be distracting or overstimulating. Static interfaces are easier to process and less tiring."
                },
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
                new() {
                    Category = "Focus Timer", Setting = "Pause on Tab Switch", Description = "Timer pauses when you leave the app", NewValue = "On",
                    Tooltip = "Focus timer automatically pauses when you switch to another tab or application, and resumes when you return.",
                    WhyItHelps = "Creates accountability by preventing 'phantom' focus time. The timer reflects actual focused work, not distracted time."
                },
                new() {
                    Category = "Focus Timer", Setting = "Session Prompts", Description = "Set intentions and reflect on sessions", NewValue = "On",
                    Tooltip = "Before each session, state your intention. Afterward, reflect on how it went and what you accomplished.",
                    WhyItHelps = "Written commitments increase follow-through. Reflection builds self-awareness about work patterns over time."
                },
                new() {
                    Category = "Accountability", Setting = "Body Doubling", Description = "Virtual co-working presence indicator", NewValue = "On",
                    Tooltip = "Shows when others are also working, creating a sense of shared focus. You can see that you're 'working alongside' others.",
                    WhyItHelps = "Body doubling—working in the presence of others—provides external accountability. Even virtual presence activates the social motivation system."
                },
                new() {
                    Category = "Accountability", Setting = "Commitment Mode", Description = "Must complete or explicitly abandon tasks", NewValue = "On",
                    Tooltip = "Tasks can't just be ignored—you must either complete them or explicitly abandon them with a reason.",
                    WhyItHelps = "Prevents tasks from disappearing into the void. Making abandonment explicit creates friction that encourages completion."
                },
                new() {
                    Category = "Accountability", Setting = "Abandon Reason", Description = "Explain why when abandoning a task", NewValue = "Required",
                    Tooltip = "When you abandon a task, you must provide a brief reason. This is logged for later review.",
                    WhyItHelps = "Adding friction to quitting makes completion more likely. Understanding patterns in abandonment helps improve task selection."
                },
                new() {
                    Category = "Structure", Setting = "Morning Planning", Description = "Daily planning prompt at 9 AM", NewValue = "On",
                    Tooltip = "Each morning, you'll be prompted to select which tasks you intend to work on today.",
                    WhyItHelps = "Starting the day with clear intentions reduces decision fatigue later. Morning commitment increases afternoon follow-through."
                },
                new() {
                    Category = "Structure", Setting = "Evening Wind-down", Description = "End-of-day reminder at 5 PM", NewValue = "On",
                    Tooltip = "A reminder to review your day, close out work, and prepare for tomorrow.",
                    WhyItHelps = "Defined work endings prevent endless work anxiety. Closure rituals help transition out of work mode."
                },
                new() {
                    Category = "Structure", Setting = "Blocker Prompts", Description = "Ask what's blocking stuck tasks", NewValue = "On",
                    Tooltip = "When tasks have been waiting too long, you'll be prompted to identify what's blocking progress.",
                    WhyItHelps = "Naming blockers is the first step to removing them. External prompts catch tasks that might otherwise be forgotten."
                },
                new() {
                    Category = "Progress", Setting = "Daily Progress Bar", Description = "Visual progress toward daily goal", NewValue = "On",
                    Tooltip = "A progress bar shows how close you are to completing your daily task goal.",
                    WhyItHelps = "Visual progress provides motivation and a sense of accomplishment. Seeing the bar fill up is satisfying and encouraging."
                },
                new() {
                    Category = "Progress", Setting = "Weekly Summary", Description = "Achievement summary each week", NewValue = "On",
                    Tooltip = "Receive a weekly summary of what you accomplished—tasks completed, focus time, streaks maintained.",
                    WhyItHelps = "Regular review builds awareness of patterns. Seeing cumulative progress combats the feeling of 'not getting anything done.'"
                },
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
                new() {
                    Category = "Task View", Setting = "Hide Task Counts", Description = "Don't show how many tasks remain", NewValue = "On",
                    Tooltip = "Hides all task and project counts from the interface. No more '47 tasks remaining.'",
                    WhyItHelps = "Numbers can trigger anxiety spirals ('So much to do!'). Removing them keeps focus on the current moment, not the overwhelming whole."
                },
                new() {
                    Category = "Task View", Setting = "Hide Due Dates", Description = "Remove deadline pressure from cards", NewValue = "On",
                    Tooltip = "Task cards don't display their due dates. You can still see dates if you open task details.",
                    WhyItHelps = "Constant deadline visibility creates persistent low-level stress. Hiding dates allows you to engage with tasks without time pressure."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Clean, minimal task presentation", NewValue = "On",
                    Tooltip = "Task cards show only essential information—title and maybe priority. Metadata is hidden.",
                    WhyItHelps = "Less information means less to process and worry about. A calm, clean interface reduces cognitive load and stress."
                },
                new() {
                    Category = "Time Pressure", Setting = "Time Displays", Description = "Hide elapsed time and estimates", NewValue = "Off",
                    Tooltip = "Hides all time-related displays including elapsed time and completion estimates.",
                    WhyItHelps = "Time awareness can fuel anxiety ('I've been working on this for HOW long?'). Hiding time lets you work at your own pace."
                },
                new() {
                    Category = "Time Pressure", Setting = "Daily Goals", Description = "No pressure to hit task targets", NewValue = "Off",
                    Tooltip = "Disables the daily task goal feature. No targets, no pressure.",
                    WhyItHelps = "Goals can become another source of failure anxiety. Removing them allows accomplishment without the pressure of quotas."
                },
                new() {
                    Category = "Time Pressure", Setting = "Grace Period", Description = "15 minutes before task is 'late'", NewValue = "15 min",
                    Tooltip = "Tasks aren't marked as 'late' until 15 minutes after their scheduled time.",
                    WhyItHelps = "A forgiving buffer reduces the anxiety of running slightly behind. Perfectionism often means 'one minute late' feels catastrophic."
                },
                new() {
                    Category = "Support", Setting = "Gentle Reminders", Description = "Soft, non-urgent nudges", NewValue = "On",
                    Tooltip = "Reminders use calm, gentle language and appear as suggestions, not demands.",
                    WhyItHelps = "Harsh reminders can trigger shame and avoidance. Gentle language invites action without activating the threat response."
                },
                new() {
                    Category = "Support", Setting = "Encouraging Messages", Description = "Positive affirmations", NewValue = "On",
                    Tooltip = "Occasional supportive messages appear, acknowledging your efforts and progress.",
                    WhyItHelps = "Counters the inner critic with external validation. Kind words can interrupt negative thought patterns."
                },
                new() {
                    Category = "Support", Setting = "Pick For Me", Description = "Let the app choose when stuck", NewValue = "On",
                    Tooltip = "When you can't decide, this button picks a task for you. Removes the burden of choice.",
                    WhyItHelps = "Decision anxiety ('What if I pick wrong?') can be paralyzing. Delegating the choice removes this source of stress."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Subtle rewards for completion", NewValue = "Subtle",
                    Tooltip = "Completing tasks shows minimal visual feedback—a gentle acknowledgment, not a party.",
                    WhyItHelps = "Intense celebrations can feel jarring or even mocking during hard times. Subtle feedback respects where you're at."
                },
                new() {
                    Category = "Audio", Setting = "All Sounds", Description = "Silent operation", NewValue = "Muted",
                    Tooltip = "All app sounds are disabled. Complete silence.",
                    WhyItHelps = "Unexpected sounds can trigger startles and anxiety spikes. A silent app is a predictable, safe app."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Reduce movement for calm", NewValue = "Reduced",
                    Tooltip = "Minimizes or disables all animations and motion effects.",
                    WhyItHelps = "Motion can feel activating or even agitating. Still, calm interfaces match a need for calm internal states."
                },
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
                new() {
                    Category = "Structure", Setting = "Morning Planning", Description = "Start each day with a plan at 8 AM", NewValue = "On",
                    Tooltip = "Each morning at 8 AM, you'll be prompted to review and select today's tasks. A consistent daily start.",
                    WhyItHelps = "Predictable routines reduce uncertainty-driven anxiety. Knowing what to expect helps the mind settle into the day."
                },
                new() {
                    Category = "Structure", Setting = "Evening Wind-down", Description = "Closure routine at 6 PM", NewValue = "On",
                    Tooltip = "At 6 PM, receive a prompt to wrap up, review the day, and prepare for tomorrow.",
                    WhyItHelps = "Defined endings prevent work from bleeding into personal time. Closure rituals signal 'safe to stop worrying.'"
                },
                new() {
                    Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Predictable 25-min work, 5-min break", NewValue = "On",
                    Tooltip = "Standard Pomodoro rhythm: 25 minutes of work, then 5 minutes of break. Consistent and predictable.",
                    WhyItHelps = "The structured rhythm reduces uncertainty ('When should I stop?'). You always know a break is coming."
                },
                new() {
                    Category = "Focus Timer", Setting = "Auto-start Breaks", Description = "Consistent break timing", NewValue = "On",
                    Tooltip = "Break timer starts automatically when work timer ends. No decisions needed.",
                    WhyItHelps = "Automatic transitions remove decision points that can trigger anxiety ('Should I take a break or keep going?')."
                },
                new() {
                    Category = "Task View", Setting = "Hide Counts", Description = "Reduce number anxiety", NewValue = "On",
                    Tooltip = "Hides task and project counts from the interface.",
                    WhyItHelps = "Numbers can fuel worry ('I still have 23 tasks!'). Hiding them keeps focus on the process, not the backlog."
                },
                new() {
                    Category = "Goals", Setting = "Daily Goal", Description = "Achievable target of 3 tasks", NewValue = "3 tasks",
                    Tooltip = "A modest, achievable daily goal of 3 tasks. Success is designed to be likely.",
                    WhyItHelps = "Achievable goals build confidence. Consistent small wins are better for anxiety than ambitious targets that risk failure."
                },
                new() {
                    Category = "Support", Setting = "Gentle Reminders", Description = "Soft prompts to stay on track", NewValue = "On",
                    Tooltip = "Reminders use kind, non-urgent language. Suggestions, not demands.",
                    WhyItHelps = "Supportive reminders feel like a friend, not a drill sergeant. They guide without triggering defensive anxiety."
                },
                new() {
                    Category = "Support", Setting = "Task Transitions", Description = "Buffer time between tasks", NewValue = "5 min",
                    Tooltip = "5 minutes of buffer time between tasks. Time to close one thing and prepare for the next.",
                    WhyItHelps = "Transitions without buffers feel rushed and stressful. Built-in pause time allows mental reset."
                },
                new() {
                    Category = "Progress", Setting = "Progress Bar", Description = "Visual sense of accomplishment", NewValue = "On",
                    Tooltip = "A visual bar showing progress toward your daily goal.",
                    WhyItHelps = "Visible progress provides reassurance ('I am making headway'). Counters the anxious feeling of 'getting nowhere.'"
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Calm, reduced motion", NewValue = "Reduced",
                    Tooltip = "Animations and transitions are minimized throughout the app.",
                    WhyItHelps = "Reduced motion creates a calmer visual environment. Predictable, still interfaces feel safer."
                },
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
                new() {
                    Category = "Task View", Setting = "One Task Mode", Description = "Focus on just one thing", NewValue = "On",
                    Tooltip = "Shows only one task at a time. Just this one thing to focus on right now.",
                    WhyItHelps = "When energy is low, even seeing multiple tasks feels overwhelming. One thing is manageable. One thing is enough."
                },
                new() {
                    Category = "Task View", Setting = "Visible Tasks", Description = "Show only 3 tasks at a time", NewValue = "3 tasks",
                    Tooltip = "When browsing, only 3 tasks are visible at once. The world of tasks stays small.",
                    WhyItHelps = "A smaller world of tasks feels safer and more manageable. Less to consider, less to feel guilty about."
                },
                new() {
                    Category = "Task View", Setting = "Hide Counts", Description = "No overwhelming numbers", NewValue = "On",
                    Tooltip = "No numbers showing how many tasks or projects exist. Just what's in front of you.",
                    WhyItHelps = "Numbers invite comparison and guilt ('I should have done more'). Hiding them removes that source of self-criticism."
                },
                new() {
                    Category = "Task View", Setting = "Hide Due Dates", Description = "Remove deadline pressure", NewValue = "On",
                    Tooltip = "Due dates don't appear on task cards. Less pressure, more focus on just doing.",
                    WhyItHelps = "Deadlines can feel punishing when you're struggling. Removing them makes tasks feel like invitations, not demands."
                },
                new() {
                    Category = "Goals", Setting = "Daily Goal", Description = "Just complete one thing", NewValue = "1 task",
                    Tooltip = "The daily goal is just one task. If you do one thing today, that's a complete success.",
                    WhyItHelps = "On hard days, one task completed is genuinely an achievement. Tiny goals make success possible even when everything feels hard."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Extra celebration for every win", NewValue = "Extra",
                    Tooltip = "Completing any task triggers extra-celebratory effects. Big confetti, happy animations.",
                    WhyItHelps = "External positive feedback counters the inner voice saying accomplishments don't matter. Every win deserves celebration."
                },
                new() {
                    Category = "Motivation", Setting = "Micro-rewards", Description = "Small wins feel good", NewValue = "On",
                    Tooltip = "Small positive feedback for any progress, not just completion. Every step counts.",
                    WhyItHelps = "Progress feels invisible when you're struggling. External acknowledgment of small steps makes movement visible."
                },
                new() {
                    Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive affirmations", NewValue = "On",
                    Tooltip = "Kind, supportive messages appear occasionally—acknowledging effort and offering encouragement.",
                    WhyItHelps = "Counteracts negative self-talk with external kindness. Sometimes you need someone (or something) to be gentle with you."
                },
                new() {
                    Category = "Support", Setting = "Gentle Reminders", Description = "Soft, compassionate nudges", NewValue = "On",
                    Tooltip = "Reminders are worded kindly, as invitations rather than demands. No guilt trips.",
                    WhyItHelps = "Harsh reminders feel like criticism. Gentle nudges feel like support from someone who understands."
                },
                new() {
                    Category = "Support", Setting = "Grace Period", Description = "30 minutes before 'late'", NewValue = "30 min",
                    Tooltip = "Tasks aren't marked late until 30 minutes past their time. Generous buffer for hard days.",
                    WhyItHelps = "Extra forgiveness reduces shame about being behind. You deserve grace, especially when things are difficult."
                },
                new() {
                    Category = "Support", Setting = "Pick For Me", Description = "Decision support when stuck", NewValue = "On",
                    Tooltip = "When you can't decide or can't start, this button picks something for you.",
                    WhyItHelps = "Decision-making takes energy that may be depleted. Outsourcing the choice removes a major barrier to starting."
                },
                new() {
                    Category = "Pressure", Setting = "Commitment Mode", Description = "No forced completion", NewValue = "Off",
                    Tooltip = "You can close tasks without completing them. No pressure, no guilt.",
                    WhyItHelps = "Forced completion adds pressure when you may not have capacity. Flexibility is more sustainable than rigidity."
                },
                new() {
                    Category = "Focus Timer", Setting = "Timer", Description = "Optional, not required", NewValue = "Off (optional 10 min)",
                    Tooltip = "Timer is available if you want it (10 min sessions) but disabled by default. No obligations.",
                    WhyItHelps = "Some days, any external structure feels like too much. The timer is there if helpful, invisible if not."
                },
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
                new() {
                    Category = "Progress", Setting = "Progress Visualization", Description = "See your progress visually", NewValue = "On",
                    Tooltip = "Charts and visuals showing your activity over time. See your patterns and growth.",
                    WhyItHelps = "Visual evidence counters the feeling of 'I never get anything done.' You can see that you ARE making progress."
                },
                new() {
                    Category = "Progress", Setting = "Daily Progress Bar", Description = "Track daily completion", NewValue = "On",
                    Tooltip = "A progress bar fills as you complete tasks, showing how close you are to your daily goal.",
                    WhyItHelps = "Watching the bar fill provides real-time motivation. Each task moves the needle visibly."
                },
                new() {
                    Category = "Progress", Setting = "Weekly Summary", Description = "Celebrate weekly achievements", NewValue = "On",
                    Tooltip = "Each week, receive a summary of what you accomplished—tasks, focus time, streaks.",
                    WhyItHelps = "Weekly perspective shows cumulative progress that's invisible day-to-day. Evidence that effort adds up."
                },
                new() {
                    Category = "Progress", Setting = "Focus Streaks", Description = "Build consecutive session streaks", NewValue = "On",
                    Tooltip = "Track streaks of completed focus sessions. Build momentum over time.",
                    WhyItHelps = "Streaks create a gentle pull to continue. Protecting a streak becomes its own motivation source."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Big celebrations for wins", NewValue = "Extra",
                    Tooltip = "Completing tasks triggers enthusiastic celebrations—confetti, animations, positive feedback.",
                    WhyItHelps = "Big external rewards compensate when internal reward signals are muted. Your brain might not celebrate, but the app will."
                },
                new() {
                    Category = "Motivation", Setting = "Micro-rewards", Description = "Frequent small rewards", NewValue = "On",
                    Tooltip = "Small positive feedback for incremental progress, not just completion.",
                    WhyItHelps = "Frequent rewards maintain engagement during tasks. The brain needs more frequent positive signals."
                },
                new() {
                    Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive reinforcement", NewValue = "On",
                    Tooltip = "Supportive messages appear periodically, acknowledging effort and celebrating progress.",
                    WhyItHelps = "External encouragement fills in when self-encouragement is hard to access. Kind words matter."
                },
                new() {
                    Category = "Goals", Setting = "Daily Goal", Description = "Achievable 3-task target", NewValue = "3 tasks",
                    Tooltip = "A realistic daily goal of 3 tasks. Designed to be achievable.",
                    WhyItHelps = "Achievable goals create a success track record. Repeated small wins build confidence and momentum."
                },
                new() {
                    Category = "Focus Timer", Setting = "Timer", Description = "Short 15-min sessions", NewValue = "15 min",
                    Tooltip = "Short 15-minute focus sessions. Easier to start, easier to complete.",
                    WhyItHelps = "Shorter sessions are less daunting to begin. Success in short bursts builds confidence for longer ones."
                },
                new() {
                    Category = "Support", Setting = "Pick For Me", Description = "Help with decisions", NewValue = "On",
                    Tooltip = "When stuck deciding, this button picks a task for you.",
                    WhyItHelps = "Low energy makes decisions harder. Removing the choice barrier makes starting possible."
                },
                new() {
                    Category = "Support", Setting = "Task Breakdown", Description = "Suggest smaller subtasks", NewValue = "20 min threshold",
                    Tooltip = "Tasks over 20 minutes will prompt you to break them into smaller pieces.",
                    WhyItHelps = "Large tasks feel impossible when energy is low. Breaking them down reveals manageable first steps."
                },
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
                new() {
                    Category = "Audio", Setting = "All Sounds", Description = "Complete silence", NewValue = "Muted",
                    Tooltip = "All app sounds are disabled—notifications, timers, feedback sounds. Complete auditory silence.",
                    WhyItHelps = "Eliminates unexpected auditory input that can be jarring or overwhelming for sensory-sensitive individuals."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "No motion or transitions", NewValue = "Always Reduced",
                    Tooltip = "All animations and motion effects are disabled. The interface remains static and still.",
                    WhyItHelps = "Motion can be distracting or overstimulating. A still interface is more predictable and easier to process."
                },
                new() {
                    Category = "Visual", Setting = "Celebrations", Description = "No visual effects", NewValue = "Off",
                    Tooltip = "Completion celebrations are disabled. Task completion is acknowledged simply without effects.",
                    WhyItHelps = "Surprise visual effects can be overwhelming. Removing them creates a calmer, more predictable experience."
                },
                new() {
                    Category = "Visual", Setting = "Micro-rewards", Description = "No popup rewards", NewValue = "Off",
                    Tooltip = "Progress reward popups are disabled. No unexpected visual interruptions.",
                    WhyItHelps = "Popups break focus and can feel jarring. A consistent, uninterrupted interface supports sustained attention."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Clean, minimal interface", NewValue = "On",
                    Tooltip = "Task cards show only essential information. Metadata, tags, and extra details are hidden.",
                    WhyItHelps = "Reduces visual complexity and information processing demands. Less sensory input, easier to focus."
                },
                new() {
                    Category = "Task View", Setting = "Minimal Mode", Description = "Hide non-essential elements", NewValue = "On",
                    Tooltip = "Hides decorative and non-essential UI elements throughout the app.",
                    WhyItHelps = "Creates maximum visual calm. Only functional elements remain, reducing cognitive and sensory load."
                },
                new() {
                    Category = "Notifications", Setting = "Encouraging Messages", Description = "No surprise popups", NewValue = "Off",
                    Tooltip = "Motivational popup messages are disabled. No unexpected text appearing.",
                    WhyItHelps = "Surprise messages break predictability. Removing them maintains a consistent, expected experience."
                },
                new() {
                    Category = "Notifications", Setting = "Timer Notification", Description = "No popup alerts", NewValue = "Off",
                    Tooltip = "Timer completion won't show popup notifications. You can check status when ready.",
                    WhyItHelps = "Popup alerts can feel intrusive. Giving you control over when to check status respects your sensory needs."
                },
                new() {
                    Category = "Structure", Setting = "Focus Timer", Description = "Predictable work intervals", NewValue = "On (25 min)",
                    Tooltip = "Standard 25-minute Pomodoro sessions. Consistent, predictable work rhythm.",
                    WhyItHelps = "Predictable structure reduces uncertainty. Knowing exactly when breaks happen supports comfortable routines."
                },
                new() {
                    Category = "Time Helpers", Setting = "Time Displays", Description = "Clear elapsed/remaining time", NewValue = "On",
                    Tooltip = "Shows clear time information—how long you've worked, how long until break.",
                    WhyItHelps = "Explicit time displays eliminate guessing. Clear information supports planning and reduces uncertainty."
                },
                new() {
                    Category = "Accessibility", Setting = "Link Underlines", Description = "Always visible link indicators", NewValue = "On",
                    Tooltip = "Links are always underlined, not just on hover. Consistent visual indicators.",
                    WhyItHelps = "Consistent visual cues reduce guessing about what's clickable. Predictable interface behavior supports navigation."
                },
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
                new() {
                    Category = "Structure", Setting = "Morning Planning", Description = "Daily planning prompt at 8 AM", NewValue = "On",
                    Tooltip = "Every day at 8 AM, you'll be prompted to review and plan your day. Consistent daily start.",
                    WhyItHelps = "Reliable routines reduce anxiety and cognitive load. Same time, same activity, predictable start to the day."
                },
                new() {
                    Category = "Structure", Setting = "Evening Wind-down", Description = "Closure routine at 6 PM", NewValue = "On",
                    Tooltip = "At 6 PM, receive a prompt to close out work and prepare for the next day.",
                    WhyItHelps = "Clear endings are important for transitioning between activities. Routines around closure help process the day."
                },
                new() {
                    Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Consistent 25/5/15 min pattern", NewValue = "On",
                    Tooltip = "Standard Pomodoro: 25 min work, 5 min break, 15 min long break after 4 sessions. Always the same.",
                    WhyItHelps = "Predictable rhythm means no surprises. You always know what's coming next—work, short break, long break."
                },
                new() {
                    Category = "Focus Timer", Setting = "Auto-start Breaks", Description = "Predictable break timing", NewValue = "On",
                    Tooltip = "Breaks start automatically when work ends. No decision needed.",
                    WhyItHelps = "Automatic transitions maintain routine without requiring decisions. The system handles the timing."
                },
                new() {
                    Category = "Transitions", Setting = "Task Transitions", Description = "5-min buffer between tasks", NewValue = "On (5 min)",
                    Tooltip = "5 minutes of buffer time between tasks. Time to complete one thing and prepare for the next.",
                    WhyItHelps = "Transitions are challenging and deserve explicit time. Buffer allows for mental adjustment without rushing."
                },
                new() {
                    Category = "Transitions", Setting = "Context Switch Warnings", Description = "Alert before work type changes", NewValue = "On",
                    Tooltip = "Get advance notice when your next task is a different type of work (e.g., creative to administrative).",
                    WhyItHelps = "Context switches require mental preparation. Advance warning allows you to prepare for the shift."
                },
                new() {
                    Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Time awareness throughout day", NewValue = "On",
                    Tooltip = "Shows elapsed time, estimates, and periodic time check-ins throughout your work.",
                    WhyItHelps = "Explicit time information removes guessing. Clear data supports planning and reduces uncertainty about 'how long things take.'"
                },
                new() {
                    Category = "Time Helpers", Setting = "Ambient Time Display", Description = "Always-visible time indicator", NewValue = "On",
                    Tooltip = "A subtle, always-present time display keeps current time visible without being intrusive.",
                    WhyItHelps = "Constant time awareness supports routine adherence. No need to seek out a clock; time is always available."
                },
                new() {
                    Category = "Progress", Setting = "Progress Visualization", Description = "Track routine completion", NewValue = "On",
                    Tooltip = "Visual charts showing your activity patterns and routine adherence over time.",
                    WhyItHelps = "Visible patterns are reassuring and informative. See how consistently you're following your routines."
                },
                new() {
                    Category = "Audio", Setting = "All Sounds", Description = "Silent operation", NewValue = "Muted",
                    Tooltip = "All app sounds are disabled for complete silence.",
                    WhyItHelps = "Auditory predictability means silence. No unexpected sounds that could be jarring."
                },
                new() {
                    Category = "Visual", Setting = "Celebrations", Description = "No surprise animations", NewValue = "Off",
                    Tooltip = "Task completion shows simple acknowledgment, no animations or visual effects.",
                    WhyItHelps = "Predictable visual responses mean no surprises. Completion is confirmed without unexpected fanfare."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Reduced motion", NewValue = "Always Reduced",
                    Tooltip = "All animations and transitions are minimized or disabled.",
                    WhyItHelps = "Still interfaces are more predictable. Motion can be distracting or uncomfortable."
                },
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
                new() {
                    Category = "Reading", Setting = "Dyslexia-Friendly Font", Description = "OpenDyslexic or similar font", NewValue = "On",
                    Tooltip = "Uses a typeface designed for dyslexic readers, with weighted bottoms and unique letter shapes to reduce confusion.",
                    WhyItHelps = "Dyslexia-friendly fonts have features that make letters more distinct and harder to flip or confuse (like b/d, p/q)."
                },
                new() {
                    Category = "Reading", Setting = "Line Height", Description = "Extra space between lines", NewValue = "1.8x",
                    Tooltip = "Increases the space between lines of text to 1.8x the normal height.",
                    WhyItHelps = "Extra line spacing helps prevent 'line jumping' and makes it easier to track across a line of text."
                },
                new() {
                    Category = "Reading", Setting = "Letter Spacing", Description = "Wider letter spacing", NewValue = "0.12em",
                    Tooltip = "Adds extra space between individual letters.",
                    WhyItHelps = "Wider letter spacing reduces crowding, which can cause letters to 'swim' together for some readers."
                },
                new() {
                    Category = "Reading", Setting = "Text Size", Description = "Larger default text", NewValue = "110%",
                    Tooltip = "Increases the base text size by 10%, making all text slightly larger.",
                    WhyItHelps = "Larger text is easier to read and reduces eye strain when processing takes more effort."
                },
                new() {
                    Category = "Visual", Setting = "Link Underlines", Description = "Always visible link indicators", NewValue = "On",
                    Tooltip = "Links are always underlined, not just on hover. Clear visual indicators.",
                    WhyItHelps = "Consistent underlining makes links obvious without requiring reading comprehension to identify them."
                },
                new() {
                    Category = "Visual", Setting = "Focus Indicator", Description = "Larger focus outlines", NewValue = "3px",
                    Tooltip = "Keyboard focus outlines are thicker (3px) and more visible.",
                    WhyItHelps = "Larger focus indicators are easier to track, especially when navigating by keyboard."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Less text to process", NewValue = "On",
                    Tooltip = "Task cards show minimal text—just the essential information.",
                    WhyItHelps = "Less text means less reading required. Information is conveyed efficiently with fewer words to decode."
                },
                new() {
                    Category = "Progress", Setting = "Progress Visualization", Description = "Visual instead of text-based", NewValue = "On",
                    Tooltip = "Progress is shown through charts and visual indicators rather than text descriptions.",
                    WhyItHelps = "Visual representations communicate information without requiring reading. A picture is worth a thousand words."
                },
                new() {
                    Category = "Progress", Setting = "Progress Bar", Description = "Visual daily progress", NewValue = "On",
                    Tooltip = "A visual bar shows daily progress—no reading needed to understand.",
                    WhyItHelps = "Progress bars communicate instantly and visually. No text processing required to understand status."
                },
                new() {
                    Category = "Audio", Setting = "Timer Sound", Description = "Audio cues for completion", NewValue = "On",
                    Tooltip = "Audio notification when timers complete. Sound-based feedback.",
                    WhyItHelps = "Audio cues provide information through a different channel. Less reliance on reading visual notifications."
                },
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
                new() {
                    Category = "Vision", Setting = "High Contrast Mode", Description = "Maximum color contrast", NewValue = "On",
                    Tooltip = "Increases contrast between text and backgrounds to the maximum level. Black on white, white on black.",
                    WhyItHelps = "High contrast makes text more visible and easier to distinguish from backgrounds for those with reduced vision."
                },
                new() {
                    Category = "Vision", Setting = "Text Size", Description = "50% larger text", NewValue = "150%",
                    Tooltip = "All text is scaled up by 50%, making it significantly larger throughout the interface.",
                    WhyItHelps = "Larger text is easier to see and read for those with reduced visual acuity."
                },
                new() {
                    Category = "Vision", Setting = "Line Height", Description = "Extra line spacing", NewValue = "1.6x",
                    Tooltip = "Increases space between lines of text to 1.6x normal height.",
                    WhyItHelps = "Extra spacing makes lines more distinct and easier to follow, reducing eye strain."
                },
                new() {
                    Category = "Vision", Setting = "Focus Indicator", Description = "Large, visible focus outlines", NewValue = "4px",
                    Tooltip = "Keyboard focus indicators are thick (4px) and highly visible.",
                    WhyItHelps = "Large focus outlines are easier to see when navigating by keyboard, essential for users who can't easily use a mouse."
                },
                new() {
                    Category = "Vision", Setting = "Larger Click Targets", Description = "Bigger buttons and links", NewValue = "On",
                    Tooltip = "Interactive elements (buttons, links) are larger than standard, with more padding.",
                    WhyItHelps = "Larger targets are easier to see and click, reducing the precision needed for interaction."
                },
                new() {
                    Category = "Vision", Setting = "Link Underlines", Description = "Always visible links", NewValue = "On",
                    Tooltip = "Links are always underlined, making them visible without relying on color alone.",
                    WhyItHelps = "Underlines provide a secondary visual cue that doesn't depend on color distinction."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Cleaner, easier to see", NewValue = "On",
                    Tooltip = "Task cards show less information, creating a cleaner, less cluttered view.",
                    WhyItHelps = "Less visual clutter means less to process. Important information stands out more clearly."
                },
                new() {
                    Category = "Audio", Setting = "Timer Sound", Description = "Audio feedback", NewValue = "On",
                    Tooltip = "Timer completion and other events produce audio notifications.",
                    WhyItHelps = "Audio feedback provides information through sound, supplementing visual notifications."
                },
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
                new() {
                    Category = "Vision", Setting = "Color Mode", Description = "Deuteranopia-safe color palette", NewValue = "Deuteranopia",
                    Tooltip = "Adjusts the app's color palette to avoid problematic red-green color combinations for deuteranopia (green-blind) color vision.",
                    WhyItHelps = "Deuteranopia affects the perception of green light. This palette uses colors that remain distinguishable."
                },
                new() {
                    Category = "Vision", Setting = "Link Underlines", Description = "Non-color link indicators", NewValue = "On",
                    Tooltip = "Links are always underlined, providing a non-color-based way to identify them.",
                    WhyItHelps = "When color can't reliably indicate links, underlines provide a consistent visual indicator."
                },
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
                new() {
                    Category = "Vision", Setting = "Color Mode", Description = "Protanopia-safe color palette", NewValue = "Protanopia",
                    Tooltip = "Adjusts the app's color palette to avoid problematic red-green color combinations for protanopia (red-blind) color vision.",
                    WhyItHelps = "Protanopia affects the perception of red light. This palette uses colors that remain distinguishable."
                },
                new() {
                    Category = "Vision", Setting = "Link Underlines", Description = "Non-color link indicators", NewValue = "On",
                    Tooltip = "Links are always underlined, providing a non-color-based way to identify them.",
                    WhyItHelps = "When color can't reliably indicate links, underlines provide a consistent visual indicator."
                },
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
                new() {
                    Category = "Interaction", Setting = "Larger Click Targets", Description = "Bigger buttons and links", NewValue = "On",
                    Tooltip = "All interactive elements (buttons, links, checkboxes) are larger with more padding around them.",
                    WhyItHelps = "Larger targets require less precision to click, making them easier to hit for users with motor challenges or tremors."
                },
                new() {
                    Category = "Interaction", Setting = "Focus Indicator", Description = "Large, visible focus outlines", NewValue = "4px",
                    Tooltip = "Keyboard focus indicators are thick (4px) and clearly visible.",
                    WhyItHelps = "Large focus outlines make keyboard navigation easier to track, essential for users who navigate without a mouse."
                },
                new() {
                    Category = "Interaction", Setting = "Tooltip Delay", Description = "More time before tooltips appear", NewValue = "1000ms",
                    Tooltip = "Tooltips won't appear until you've hovered for 1 second, preventing accidental triggers.",
                    WhyItHelps = "Longer delays prevent unwanted tooltips when cursor movement is less precise or intentional."
                },
                new() {
                    Category = "Task View", Setting = "Simplified View", Description = "Fewer small elements to click", NewValue = "On",
                    Tooltip = "Task cards show less detail, with fewer small buttons and interactive elements.",
                    WhyItHelps = "Fewer targets means less need for precise clicking. Simpler interfaces are easier to navigate."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Reduced motion", NewValue = "Always Reduced",
                    Tooltip = "All animations and motion effects are minimized or disabled.",
                    WhyItHelps = "Static interfaces are easier to interact with. Moving targets are harder to click."
                },
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
                new() {
                    Category = "Focus Timer", Setting = "Pomodoro Timer", Description = "Standard 25-min sessions", NewValue = "On (25 min)",
                    Tooltip = "Standard Pomodoro technique with 25-minute focused work sessions.",
                    WhyItHelps = "Provides structure without being overwhelming. A proven technique that works well for many neurodivergent individuals."
                },
                new() {
                    Category = "Focus Timer", Setting = "Session Intentions", Description = "Set focus before starting", NewValue = "On",
                    Tooltip = "Before each focus session, state your intention—what you're going to work on.",
                    WhyItHelps = "Intentions externalize your commitment and help engage focus. Helpful for executive function challenges."
                },
                new() {
                    Category = "Task View", Setting = "Visible Tasks", Description = "Balanced task visibility", NewValue = "7 tasks",
                    Tooltip = "Shows up to 7 tasks at once—enough to see options, not enough to overwhelm.",
                    WhyItHelps = "A middle ground: more context than one task, less overwhelm than seeing everything."
                },
                new() {
                    Category = "Time Helpers", Setting = "Time Blindness Support", Description = "Time awareness helpers", NewValue = "On",
                    Tooltip = "Shows elapsed time, estimates, and gentle time check-ins to help with time perception.",
                    WhyItHelps = "Supports time awareness without creating pressure. Helpful for those who lose track of time."
                },
                new() {
                    Category = "Time Helpers", Setting = "Completion Estimates", Description = "Show when you'll finish", NewValue = "On",
                    Tooltip = "Based on your current pace, shows an estimate of when you'll complete your current task or goal.",
                    WhyItHelps = "Makes abstract progress concrete. Helps with planning and motivation."
                },
                new() {
                    Category = "Motivation", Setting = "Celebrations", Description = "Medium celebrations", NewValue = "On (Medium)",
                    Tooltip = "Task completion triggers moderate celebratory feedback—noticeable but not overwhelming.",
                    WhyItHelps = "Provides positive reinforcement without being too intense for sensory-sensitive users."
                },
                new() {
                    Category = "Motivation", Setting = "Micro-rewards", Description = "Small progress rewards", NewValue = "On",
                    Tooltip = "Small positive feedback for incremental progress throughout tasks.",
                    WhyItHelps = "Maintains engagement during tasks. Helpful when internal reward systems need external support."
                },
                new() {
                    Category = "Motivation", Setting = "Encouraging Messages", Description = "Positive reinforcement", NewValue = "On",
                    Tooltip = "Occasional supportive messages that acknowledge effort and progress.",
                    WhyItHelps = "External validation can help counteract negative self-talk common across many conditions."
                },
                new() {
                    Category = "Support", Setting = "Pick For Me", Description = "Decision help when stuck", NewValue = "On",
                    Tooltip = "A button that selects a task for you when you can't decide what to work on.",
                    WhyItHelps = "Removes decision paralysis, a common challenge across ADHD, anxiety, and depression."
                },
                new() {
                    Category = "Support", Setting = "Gentle Reminders", Description = "Soft nudges", NewValue = "On",
                    Tooltip = "Reminders are worded gently, as suggestions rather than demands.",
                    WhyItHelps = "Kind language reduces shame and resistance. More effective than harsh reminders."
                },
                new() {
                    Category = "Support", Setting = "Task Breakdown", Description = "Suggest smaller subtasks", NewValue = "On",
                    Tooltip = "Large tasks prompt suggestions to break them into smaller, manageable pieces.",
                    WhyItHelps = "Big tasks are daunting. Smaller steps make starting easier."
                },
                new() {
                    Category = "Transitions", Setting = "Task Transitions", Description = "Buffer between tasks", NewValue = "On",
                    Tooltip = "A short buffer period between tasks for mental reset and preparation.",
                    WhyItHelps = "Transitions are cognitively demanding. Buffer time reduces the stress of rapid switching."
                },
                new() {
                    Category = "Motion", Setting = "Animations", Description = "Respect system preference", NewValue = "System",
                    Tooltip = "Follows your operating system's preference for reduced motion.",
                    WhyItHelps = "Lets you control motion at the system level. Respects your overall accessibility settings."
                },
                new() {
                    Category = "Visual", Setting = "Link Underlines", Description = "Visible link indicators", NewValue = "On",
                    Tooltip = "Links are always underlined, making them easy to identify.",
                    WhyItHelps = "Consistent visual indicators reduce cognitive load and improve navigation for everyone."
                },
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
