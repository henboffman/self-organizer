using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

/// <summary>
/// Service for detecting and notifying about stale tasks that haven't been touched in a while.
/// Follows Petri Net model: Idle -> Checking -> (NotifyUser | NoAction) -> Idle
/// </summary>
public interface IStaleTaskReminderService
{
    /// <summary>
    /// Check for stale tasks based on user preferences
    /// </summary>
    Task<List<StaleTaskInfo>> GetStaleTasksAsync();

    /// <summary>
    /// Mark a task as reviewed (resets the stale timer)
    /// </summary>
    Task MarkTaskReviewedAsync(Guid taskId);

    /// <summary>
    /// Snooze reminders for a task for a specific number of days
    /// </summary>
    Task SnoozeTaskReminderAsync(Guid taskId, int days);

    /// <summary>
    /// Get user's stale task preferences
    /// </summary>
    Task<StaleTaskPreferences> GetPreferencesAsync();

    /// <summary>
    /// Update user's stale task preferences
    /// </summary>
    Task UpdatePreferencesAsync(StaleTaskPreferences preferences);

    /// <summary>
    /// Event fired when stale tasks are detected
    /// </summary>
    event Action<List<StaleTaskInfo>>? OnStaleTasksDetected;
}

/// <summary>
/// Information about a stale task
/// </summary>
public class StaleTaskInfo
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public TodoTaskStatus Status { get; set; }
    public int DaysStale { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? SnoozedUntil { get; set; }
    public StaleReason Reason { get; set; }
    public List<string> SuggestedActions { get; set; } = new();
}

public enum StaleReason
{
    NotModifiedRecently,
    WaitingForTooLong,
    OverdueWithNoProgress,
    InboxTooLong,
    SomedayMaybeNeedsReview
}

/// <summary>
/// User preferences for stale task reminders
/// </summary>
public class StaleTaskPreferences
{
    /// <summary>
    /// Whether stale task reminders are enabled
    /// </summary>
    public bool EnableStaleTaskReminders { get; set; } = true;

    /// <summary>
    /// Days before a NextAction task is considered stale
    /// </summary>
    public int NextActionStaleDays { get; set; } = 7;

    /// <summary>
    /// Days before a WaitingFor task is considered stale
    /// </summary>
    public int WaitingForStaleDays { get; set; } = 5;

    /// <summary>
    /// Days before an Inbox item is considered stale
    /// </summary>
    public int InboxStaleDays { get; set; } = 2;

    /// <summary>
    /// Days before a Someday/Maybe item needs review
    /// </summary>
    public int SomedayMaybeReviewDays { get; set; } = 30;

    /// <summary>
    /// Hour of day to show reminders (24h format)
    /// </summary>
    public int ReminderHour { get; set; } = 9;

    /// <summary>
    /// Maximum number of stale tasks to show at once
    /// </summary>
    public int MaxRemindersToShow { get; set; } = 5;

    /// <summary>
    /// Whether to show reminders for overdue tasks with no progress
    /// </summary>
    public bool RemindOverdueTasks { get; set; } = true;

    /// <summary>
    /// Whether to use gentle, non-intrusive reminders
    /// </summary>
    public bool UseGentleReminders { get; set; } = true;
}

public class StaleTaskReminderService : IStaleTaskReminderService
{
    private readonly ITaskService _taskService;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly IRepository<TaskReminderSnooze> _snoozeRepository;

    public event Action<List<StaleTaskInfo>>? OnStaleTasksDetected;

    public StaleTaskReminderService(
        ITaskService taskService,
        IRepository<Project> projectRepository,
        IRepository<UserPreferences> preferencesRepository,
        IRepository<TaskReminderSnooze> snoozeRepository)
    {
        _taskService = taskService;
        _projectRepository = projectRepository;
        _preferencesRepository = preferencesRepository;
        _snoozeRepository = snoozeRepository;
    }

    public async Task<List<StaleTaskInfo>> GetStaleTasksAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (!prefs.EnableStaleTaskReminders)
        {
            return new List<StaleTaskInfo>();
        }

        var staleTasks = new List<StaleTaskInfo>();
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Get all active tasks
        var allTasks = await _taskService.GetAllAsync();
        var activeTasks = allTasks.Where(t =>
            t.Status != TodoTaskStatus.Completed &&
            t.Status != TodoTaskStatus.Deleted).ToList();

        // Get projects for display
        var projects = await _projectRepository.GetAllAsync();
        var projectLookup = projects.ToDictionary(p => p.Id, p => p.Name);

        // Get snoozed tasks
        var snoozes = await _snoozeRepository.GetAllAsync();
        var snoozeLookup = snoozes
            .Where(s => s.SnoozedUntil > now)
            .ToDictionary(s => s.TaskId, s => s.SnoozedUntil);

        foreach (var task in activeTasks)
        {
            // Skip if snoozed
            if (snoozeLookup.TryGetValue(task.Id, out var snoozedUntil) && snoozedUntil > now)
            {
                continue;
            }

            var daysSinceModified = (now - task.ModifiedAt).Days;
            StaleTaskInfo? staleInfo = null;

            switch (task.Status)
            {
                case TodoTaskStatus.Inbox:
                    if (daysSinceModified >= prefs.InboxStaleDays)
                    {
                        staleInfo = CreateStaleInfo(task, daysSinceModified, StaleReason.InboxTooLong, projectLookup);
                        staleInfo.SuggestedActions = new List<string>
                        {
                            "Process this item - decide what to do with it",
                            "Convert to a Next Action if it's actionable",
                            "Move to Someday/Maybe if not urgent",
                            "Delete if no longer relevant"
                        };
                    }
                    break;

                case TodoTaskStatus.NextAction:
                    if (daysSinceModified >= prefs.NextActionStaleDays)
                    {
                        staleInfo = CreateStaleInfo(task, daysSinceModified, StaleReason.NotModifiedRecently, projectLookup);
                        staleInfo.SuggestedActions = new List<string>
                        {
                            "Work on this task now",
                            "Break it down into smaller steps",
                            "Move to Someday/Maybe if not a priority",
                            "Delegate or delete if no longer needed"
                        };
                    }
                    break;

                case TodoTaskStatus.WaitingFor:
                    var waitingDays = task.WaitingForSince.HasValue
                        ? (now - task.WaitingForSince.Value).Days
                        : daysSinceModified;
                    if (waitingDays >= prefs.WaitingForStaleDays)
                    {
                        staleInfo = CreateStaleInfo(task, waitingDays, StaleReason.WaitingForTooLong, projectLookup);
                        staleInfo.SuggestedActions = new List<string>
                        {
                            "Follow up with the person you're waiting on",
                            "Set a reminder to check back",
                            "Find an alternative approach",
                            "Cancel if no longer needed"
                        };
                    }
                    break;

                case TodoTaskStatus.SomedayMaybe:
                    if (daysSinceModified >= prefs.SomedayMaybeReviewDays)
                    {
                        staleInfo = CreateStaleInfo(task, daysSinceModified, StaleReason.SomedayMaybeNeedsReview, projectLookup);
                        staleInfo.SuggestedActions = new List<string>
                        {
                            "Review if this is still relevant",
                            "Promote to Next Action if ready",
                            "Archive or delete if no longer interested",
                            "Update the description with fresh thoughts"
                        };
                    }
                    break;
            }

            // Check for overdue with no progress
            if (staleInfo == null && prefs.RemindOverdueTasks && task.DueDate.HasValue)
            {
                var daysOverdue = (today - task.DueDate.Value.Date).Days;
                if (daysOverdue > 0 && daysSinceModified >= 2)
                {
                    staleInfo = CreateStaleInfo(task, daysOverdue, StaleReason.OverdueWithNoProgress, projectLookup);
                    staleInfo.SuggestedActions = new List<string>
                    {
                        "Complete this overdue task immediately",
                        "Reschedule to a realistic date",
                        "Communicate the delay to stakeholders",
                        "Break into smaller pieces if blocked"
                    };
                }
            }

            if (staleInfo != null)
            {
                staleTasks.Add(staleInfo);
            }
        }

        // Sort by urgency and limit
        var result = staleTasks
            .OrderByDescending(t => t.Reason == StaleReason.OverdueWithNoProgress)
            .ThenByDescending(t => t.Reason == StaleReason.InboxTooLong)
            .ThenByDescending(t => t.DaysStale)
            .Take(prefs.MaxRemindersToShow)
            .ToList();

        if (result.Any())
        {
            OnStaleTasksDetected?.Invoke(result);
        }

        return result;
    }

    private static StaleTaskInfo CreateStaleInfo(TodoTask task, int daysStale, StaleReason reason, Dictionary<Guid, string> projectLookup)
    {
        return new StaleTaskInfo
        {
            TaskId = task.Id,
            Title = task.Title,
            ProjectName = task.ProjectId.HasValue && projectLookup.TryGetValue(task.ProjectId.Value, out var name) ? name : null,
            Status = task.Status,
            DaysStale = daysStale,
            LastModified = task.ModifiedAt,
            DueDate = task.DueDate,
            Reason = reason
        };
    }

    public async Task MarkTaskReviewedAsync(Guid taskId)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task != null)
        {
            task.ModifiedAt = DateTime.UtcNow;
            await _taskService.UpdateAsync(task);
        }

        // Remove any existing snooze
        var snoozes = await _snoozeRepository.QueryAsync(s => s.TaskId == taskId);
        foreach (var snooze in snoozes)
        {
            await _snoozeRepository.DeleteAsync(snooze.Id);
        }
    }

    public async Task SnoozeTaskReminderAsync(Guid taskId, int days)
    {
        // Remove existing snooze
        var existingSnoozes = await _snoozeRepository.QueryAsync(s => s.TaskId == taskId);
        foreach (var existing in existingSnoozes)
        {
            await _snoozeRepository.DeleteAsync(existing.Id);
        }

        // Create new snooze
        var snooze = new TaskReminderSnooze
        {
            TaskId = taskId,
            SnoozedUntil = DateTime.UtcNow.AddDays(days),
            CreatedAt = DateTime.UtcNow
        };
        await _snoozeRepository.AddAsync(snooze);
    }

    public async Task<StaleTaskPreferences> GetPreferencesAsync()
    {
        var userPrefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
        return new StaleTaskPreferences
        {
            EnableStaleTaskReminders = userPrefs?.EnableBlockerPrompts ?? true,
            NextActionStaleDays = userPrefs?.StaleTakPromptDays ?? 7,
            WaitingForStaleDays = Math.Max(3, (userPrefs?.StaleTakPromptDays ?? 7) - 2),
            InboxStaleDays = 2,
            SomedayMaybeReviewDays = 30,
            ReminderHour = userPrefs?.DailyReviewReminderHour ?? 9,
            MaxRemindersToShow = 5,
            RemindOverdueTasks = true,
            UseGentleReminders = userPrefs?.EnableGentleReminders ?? true
        };
    }

    public async Task UpdatePreferencesAsync(StaleTaskPreferences preferences)
    {
        var userPrefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
        if (userPrefs != null)
        {
            userPrefs.EnableBlockerPrompts = preferences.EnableStaleTaskReminders;
            userPrefs.StaleTakPromptDays = preferences.NextActionStaleDays;
            userPrefs.DailyReviewReminderHour = preferences.ReminderHour;
            userPrefs.EnableGentleReminders = preferences.UseGentleReminders;
            await _preferencesRepository.UpdateAsync(userPrefs);
        }
    }
}
