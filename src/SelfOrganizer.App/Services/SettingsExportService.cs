using System.Text.Json;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Result of a settings import operation
/// </summary>
public class SettingsImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int PreferencesImported { get; set; }
    public int ContextsImported { get; set; }
    public bool AccessibilityImported { get; set; }
    public bool ThemeImported { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static SettingsImportResult Succeeded(int prefs, int contexts, bool accessibility, bool theme)
        => new()
        {
            Success = true,
            PreferencesImported = prefs,
            ContextsImported = contexts,
            AccessibilityImported = accessibility,
            ThemeImported = theme
        };

    public static SettingsImportResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Bundle of all exportable settings
/// </summary>
public class SettingsBundle
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public string? ExportedFrom { get; set; }
    public UserPreferences? Preferences { get; set; }
    public AccessibilitySettings? Accessibility { get; set; }
    public List<Context>? CustomContexts { get; set; }
    public string? Theme { get; set; }
}

/// <summary>
/// Service for exporting and importing application settings
/// </summary>
public interface ISettingsExportService
{
    /// <summary>
    /// Export all settings to a JSON string
    /// </summary>
    Task<string> ExportSettingsJsonAsync();

    /// <summary>
    /// Import settings from a JSON string
    /// </summary>
    Task<SettingsImportResult> ImportSettingsJsonAsync(string json);

    /// <summary>
    /// Get the current settings bundle (without exporting)
    /// </summary>
    Task<SettingsBundle> GetCurrentSettingsAsync();

    /// <summary>
    /// Export settings and trigger download
    /// </summary>
    Task<ExportResult> ExportAndDownloadSettingsAsync();

    /// <summary>
    /// Validate a settings JSON string without importing
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage, SettingsBundle? Bundle)> ValidateSettingsJsonAsync(string json);
}

public class SettingsExportService : ISettingsExportService
{
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly IContextService _contextService;
    private readonly IThemeService _themeService;
    private readonly IExportService _exportService;
    private readonly IJSRuntime _jsRuntime;

    private const int CurrentVersion = 1;
    private const string AppName = "SelfOrganizer";

    public SettingsExportService(
        IRepository<UserPreferences> preferencesRepository,
        IContextService contextService,
        IThemeService themeService,
        IExportService exportService,
        IJSRuntime jsRuntime)
    {
        _preferencesRepository = preferencesRepository;
        _contextService = contextService;
        _themeService = themeService;
        _exportService = exportService;
        _jsRuntime = jsRuntime;
    }

    public async Task<SettingsBundle> GetCurrentSettingsAsync()
    {
        var preferences = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
        var contexts = await _contextService.GetAllSortedByMruAsync();
        var customContexts = contexts.Where(c => !c.IsBuiltIn).ToList();
        var theme = await _themeService.GetThemeAsync();

        return new SettingsBundle
        {
            Version = CurrentVersion,
            ExportedAt = DateTime.UtcNow,
            ExportedFrom = AppName,
            Preferences = preferences,
            Accessibility = preferences?.Accessibility,
            CustomContexts = customContexts.Count > 0 ? customContexts : null,
            Theme = theme
        };
    }

    public async Task<string> ExportSettingsJsonAsync()
    {
        var bundle = await GetCurrentSettingsAsync();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(bundle, options);
    }

    public async Task<ExportResult> ExportAndDownloadSettingsAsync()
    {
        try
        {
            var json = await ExportSettingsJsonAsync();
            var filename = $"self-organizer-settings-{DateTime.Now:yyyy-MM-dd}.json";
            return await _exportService.DownloadFileAsync(filename, json, "application/json");
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"Failed to export settings: {ex.Message}");
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage, SettingsBundle? Bundle)> ValidateSettingsJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (false, "Settings file is empty", null);
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var bundle = JsonSerializer.Deserialize<SettingsBundle>(json, options);

            if (bundle == null)
            {
                return (false, "Failed to parse settings file", null);
            }

            // Version check
            if (bundle.Version > CurrentVersion)
            {
                return (false, $"Settings file version {bundle.Version} is newer than supported version {CurrentVersion}. Please update the application.", null);
            }

            // Basic validation
            if (bundle.Preferences == null && bundle.CustomContexts == null && bundle.Accessibility == null)
            {
                return (false, "Settings file contains no recognizable settings data", null);
            }

            return (true, null, bundle);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid JSON format: {ex.Message}", null);
        }
    }

    public async Task<SettingsImportResult> ImportSettingsJsonAsync(string json)
    {
        var validation = await ValidateSettingsJsonAsync(json);
        if (!validation.IsValid)
        {
            return SettingsImportResult.Failed(validation.ErrorMessage!);
        }

        var bundle = validation.Bundle!;
        var result = new SettingsImportResult { Success = true };

        try
        {
            // Import preferences
            if (bundle.Preferences != null)
            {
                var existingPrefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();

                if (existingPrefs != null)
                {
                    // Update existing preferences with imported values
                    MergePreferences(existingPrefs, bundle.Preferences);

                    // Merge accessibility if present
                    if (bundle.Accessibility != null)
                    {
                        existingPrefs.Accessibility = bundle.Accessibility;
                        result.AccessibilityImported = true;
                    }

                    await _preferencesRepository.UpdateAsync(existingPrefs);
                }
                else
                {
                    // Create new preferences
                    bundle.Preferences.Id = Guid.NewGuid();
                    bundle.Preferences.CreatedAt = DateTime.UtcNow;
                    bundle.Preferences.ModifiedAt = DateTime.UtcNow;

                    if (bundle.Accessibility != null)
                    {
                        bundle.Preferences.Accessibility = bundle.Accessibility;
                        result.AccessibilityImported = true;
                    }

                    await _preferencesRepository.AddAsync(bundle.Preferences);
                }

                result.PreferencesImported = 1;
            }
            else if (bundle.Accessibility != null)
            {
                // Import accessibility settings only
                var existingPrefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
                if (existingPrefs != null)
                {
                    existingPrefs.Accessibility = bundle.Accessibility;
                    await _preferencesRepository.UpdateAsync(existingPrefs);
                    result.AccessibilityImported = true;
                }
            }

            // Import custom contexts
            if (bundle.CustomContexts?.Count > 0)
            {
                var existingContexts = await _contextService.GetAllSortedByMruAsync();

                foreach (var context in bundle.CustomContexts)
                {
                    // Check if context with same name already exists
                    var existing = existingContexts.FirstOrDefault(
                        c => c.Name.Equals(context.Name, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        // Update existing context
                        existing.Icon = context.Icon;
                        existing.Color = context.Color;
                        existing.IsActive = context.IsActive;
                        await _contextService.UpdateAsync(existing);
                        result.Warnings.Add($"Context '{context.Name}' already exists and was updated");
                    }
                    else
                    {
                        // Create new context using the service
                        await _contextService.CreateAsync(context.Name, context.Icon, context.Color);
                    }

                    result.ContextsImported++;
                }
            }

            // Import theme
            if (!string.IsNullOrEmpty(bundle.Theme))
            {
                await _themeService.SetThemeAsync(bundle.Theme);
                result.ThemeImported = true;
            }

            // Apply accessibility settings via JS interop if present
            if (result.AccessibilityImported && bundle.Accessibility != null)
            {
                await ApplyAccessibilitySettingsAsync(bundle.Accessibility);
            }

            return result;
        }
        catch (Exception ex)
        {
            return SettingsImportResult.Failed($"Failed to import settings: {ex.Message}");
        }
    }

    private static void MergePreferences(UserPreferences existing, UserPreferences imported)
    {
        // Work Schedule Settings
        existing.WorkDayStart = imported.WorkDayStart;
        existing.WorkDayEnd = imported.WorkDayEnd;
        existing.WorkDays = imported.WorkDays;
        existing.DefaultTaskDurationMinutes = imported.DefaultTaskDurationMinutes;
        existing.MinimumUsableBlockMinutes = imported.MinimumUsableBlockMinutes;
        existing.DeepWorkMinimumMinutes = imported.DeepWorkMinimumMinutes;
        existing.DefaultBreakMinutes = imported.DefaultBreakMinutes;
        existing.MaxConsecutiveMeetingMinutes = imported.MaxConsecutiveMeetingMinutes;
        existing.BufferBetweenMeetingsMinutes = imported.BufferBetweenMeetingsMinutes;
        existing.MorningEnergyPeak = imported.MorningEnergyPeak;
        existing.AfternoonEnergyPeak = imported.AfternoonEnergyPeak;
        existing.AutoScheduleEnabled = imported.AutoScheduleEnabled;
        existing.DailyReviewReminderHour = imported.DailyReviewReminderHour;
        existing.WeeklyReviewDay = imported.WeeklyReviewDay;

        // ADHD Settings
        existing.EnableCelebrationEffects = imported.EnableCelebrationEffects;
        existing.EnableFocusTimer = imported.EnableFocusTimer;
        existing.FocusTimerMinutes = imported.FocusTimerMinutes;
        existing.EnableTimeBlindnessHelpers = imported.EnableTimeBlindnessHelpers;
        existing.EnableTaskChunking = imported.EnableTaskChunking;
        existing.MaxTaskChunkMinutes = imported.MaxTaskChunkMinutes;
        existing.EnableHyperfocusAlerts = imported.EnableHyperfocusAlerts;
        existing.HyperfocusAlertMinutes = imported.HyperfocusAlertMinutes;
        existing.EnableContextSwitchWarnings = imported.EnableContextSwitchWarnings;
        existing.EnableMinimalMode = imported.EnableMinimalMode;
        existing.EnablePickForMe = imported.EnablePickForMe;

        // Extended ADHD Settings
        existing.EnableBodyDoubling = imported.EnableBodyDoubling;
        existing.EnableProgressVisualization = imported.EnableProgressVisualization;
        existing.EnableMicroRewards = imported.EnableMicroRewards;
        existing.TaskStartGracePeriod = imported.TaskStartGracePeriod;
        existing.EnableGentleReminders = imported.EnableGentleReminders;
        existing.ShowEstimatedCompletion = imported.ShowEstimatedCompletion;
        existing.EnableFocusSounds = imported.EnableFocusSounds;
        existing.PreferredFocusSound = imported.PreferredFocusSound;
        existing.BreakReminderMinutes = imported.BreakReminderMinutes;
        existing.AutoStartBreakTimer = imported.AutoStartBreakTimer;
        existing.EnableTaskTransitions = imported.EnableTaskTransitions;
        existing.TransitionBufferMinutes = imported.TransitionBufferMinutes;

        // Advanced Focus Timer Settings
        existing.ShowSessionIntentionPrompt = imported.ShowSessionIntentionPrompt;
        existing.ShowPostSessionReflection = imported.ShowPostSessionReflection;
        existing.TrackDistractions = imported.TrackDistractions;
        existing.EnableFocusStreaks = imported.EnableFocusStreaks;
        existing.PlayTimerCompletionSound = imported.PlayTimerCompletionSound;
        existing.ShowTimerNotification = imported.ShowTimerNotification;
        existing.PauseTimerOnWindowBlur = imported.PauseTimerOnWindowBlur;
        existing.LongBreakMinutes = imported.LongBreakMinutes;
        existing.SessionsBeforeLongBreak = imported.SessionsBeforeLongBreak;

        // Task Presentation & Cognitive Load
        existing.MaxVisibleTasks = imported.MaxVisibleTasks;
        existing.OneTaskAtATimeMode = imported.OneTaskAtATimeMode;
        existing.HideTaskCounts = imported.HideTaskCounts;
        existing.HideDueDatesOnCards = imported.HideDueDatesOnCards;
        existing.SimplifiedTaskView = imported.SimplifiedTaskView;
        existing.ShowEncouragingMessages = imported.ShowEncouragingMessages;

        // Time Perception Helpers
        existing.ShowElapsedTime = imported.ShowElapsedTime;
        existing.ShowTimeRemainingInBlock = imported.ShowTimeRemainingInBlock;
        existing.TimeAwarenessIntervalMinutes = imported.TimeAwarenessIntervalMinutes;
        existing.EnableAmbientTimeDisplay = imported.EnableAmbientTimeDisplay;
        existing.ShowTimeSinceLastBreak = imported.ShowTimeSinceLastBreak;

        // Sensory & Environment
        existing.AutoDarkMode = imported.AutoDarkMode;
        existing.DarkModeStartHour = imported.DarkModeStartHour;
        existing.DarkModEndHour = imported.DarkModEndHour;
        existing.SoundVolume = imported.SoundVolume;
        existing.MuteAllSounds = imported.MuteAllSounds;

        // Motivation & Rewards
        existing.EnableDailyGoalSetting = imported.EnableDailyGoalSetting;
        existing.ShowDailyProgressBar = imported.ShowDailyProgressBar;
        existing.DailyTaskGoal = imported.DailyTaskGoal;
        existing.EnableWeeklySummary = imported.EnableWeeklySummary;
        existing.CelebrationIntensity = imported.CelebrationIntensity;

        // Executive Function Support
        existing.SuggestTaskBreakdown = imported.SuggestTaskBreakdown;
        existing.TaskBreakdownThresholdMinutes = imported.TaskBreakdownThresholdMinutes;
        existing.EnableBlockerPrompts = imported.EnableBlockerPrompts;
        existing.StaleTakPromptDays = imported.StaleTakPromptDays;
        existing.EnableQuickCaptureMode = imported.EnableQuickCaptureMode;
        existing.AutoExpandNextActions = imported.AutoExpandNextActions;

        // Accountability & Structure
        existing.EnableMorningPlanningPrompt = imported.EnableMorningPlanningPrompt;
        existing.MorningPlanningHour = imported.MorningPlanningHour;
        existing.EnableEveningWindDown = imported.EnableEveningWindDown;
        existing.EveningWindDownHour = imported.EveningWindDownHour;
        existing.EnableCommitmentMode = imported.EnableCommitmentMode;
        existing.RequireAbandonReason = imported.RequireAbandonReason;

        // Schedule Optimization Weights
        existing.ContextGroupingWeight = imported.ContextGroupingWeight;
        existing.SimilarWorkGroupingWeight = imported.SimilarWorkGroupingWeight;
        existing.EnergyMatchingWeight = imported.EnergyMatchingWeight;
        existing.DueDateUrgencyWeight = imported.DueDateUrgencyWeight;
        existing.StakeholderGroupingWeight = imported.StakeholderGroupingWeight;
        existing.TagSimilarityWeight = imported.TagSimilarityWeight;
        existing.DeepWorkPreferenceWeight = imported.DeepWorkPreferenceWeight;
        existing.BlockedTaskPenalty = imported.BlockedTaskPenalty;

        // App Mode
        existing.AppMode = imported.AppMode;
        existing.EnabledBalanceDimensions = imported.EnabledBalanceDimensions;

        existing.ModifiedAt = DateTime.UtcNow;
    }

    private async Task ApplyAccessibilitySettingsAsync(AccessibilitySettings settings)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("accessibilityInterop.applyAccessibilitySettings", settings);
        }
        catch
        {
            // Silently fail if JS interop not available
        }
    }
}
