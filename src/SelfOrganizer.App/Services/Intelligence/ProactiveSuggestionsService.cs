using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.App.Services.Domain;
using SelfOrganizer.App.Services.GoogleCalendar;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service that generates proactive suggestions based on user's current state.
/// Analyzes tasks, habits, goals, calendar, and patterns to surface actionable insights.
/// </summary>
public class ProactiveSuggestionsService : IProactiveSuggestionsService
{
    private readonly ITaskService _taskService;
    private readonly IGoalService _goalService;
    private readonly IRepository<Habit> _habitRepository;
    private readonly IRepository<HabitLog> _habitLogRepository;
    private readonly IRepository<CaptureItem> _captureRepository;
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly ILlmService _llmService;
    private readonly ITaskOptimizerService _taskOptimizer;
    private readonly ICalendarService _calendarService;
    private readonly IMeetingInsightService _meetingInsightService;
    private readonly IProjectService _projectService;
    private readonly IBalanceDimensionService _balanceDimensionService;
    private readonly IGoogleCalendarSyncService _googleCalendarSyncService;

    private List<ProactiveSuggestion> _cachedSuggestions = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public event Action? OnSuggestionsChanged;

    public ProactiveSuggestionsService(
        ITaskService taskService,
        IGoalService goalService,
        IRepository<Habit> habitRepository,
        IRepository<HabitLog> habitLogRepository,
        IRepository<CaptureItem> captureRepository,
        IRepository<UserPreferences> preferencesRepository,
        ILlmService llmService,
        ITaskOptimizerService taskOptimizer,
        ICalendarService calendarService,
        IMeetingInsightService meetingInsightService,
        IProjectService projectService,
        IBalanceDimensionService balanceDimensionService,
        IGoogleCalendarSyncService googleCalendarSyncService)
    {
        _taskService = taskService;
        _goalService = goalService;
        _habitRepository = habitRepository;
        _habitLogRepository = habitLogRepository;
        _captureRepository = captureRepository;
        _preferencesRepository = preferencesRepository;
        _llmService = llmService;
        _taskOptimizer = taskOptimizer;
        _calendarService = calendarService;
        _meetingInsightService = meetingInsightService;
        _projectService = projectService;
        _balanceDimensionService = balanceDimensionService;
        _googleCalendarSyncService = googleCalendarSyncService;
    }

    /// <summary>
    /// Gets all current suggestions, using cache if available.
    /// </summary>
    public async Task<IReadOnlyList<ProactiveSuggestion>> GetSuggestionsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && DateTime.Now - _lastRefresh < CacheDuration && _cachedSuggestions.Any())
        {
            return _cachedSuggestions;
        }

        await RefreshSuggestionsAsync();
        return _cachedSuggestions;
    }

    /// <summary>
    /// Refreshes suggestions by analyzing current state.
    /// </summary>
    public async Task RefreshSuggestionsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            // Run all analyses in parallel
            var analyses = await Task.WhenAll(
                AnalyzeOverdueTasksAsync(),
                AnalyzeInboxAsync(),
                AnalyzeStaleGoalsAsync(),
                AnalyzeHabitStreaksAsync(),
                AnalyzeTaskPatternsAsync(),
                AnalyzeLifeBalanceAsync(),
                GetEnergyBasedSuggestionsAsync(),
                // Calendar-aware suggestions
                AnalyzeCalendarPatternsAsync(),
                AnalyzeProjectFocusAsync(),
                DetectSchedulingConflictsAsync(),
                // Balance and sync suggestions
                AnalyzeBalanceDimensionsAsync(),
                AnalyzeGoogleCalendarSyncAsync()
            );

            foreach (var analysis in analyses)
            {
                suggestions.AddRange(analysis);
            }

            // Sort by priority and recency
            _cachedSuggestions = suggestions
                .OrderByDescending(s => s.Priority)
                .ThenByDescending(s => s.CreatedAt)
                .Take(10)
                .ToList();

            _lastRefresh = DateTime.Now;
            OnSuggestionsChanged?.Invoke();
        }
        catch
        {
            // Keep cached suggestions on error
        }
    }

    /// <summary>
    /// Dismisses a suggestion by ID.
    /// </summary>
    public void DismissSuggestion(Guid suggestionId)
    {
        _cachedSuggestions.RemoveAll(s => s.Id == suggestionId);
        OnSuggestionsChanged?.Invoke();
    }

    /// <summary>
    /// Gets a quick insight for the current moment.
    /// </summary>
    public async Task<string?> GetQuickInsightAsync()
    {
        var hour = DateTime.Now.Hour;
        var suggestions = await GetSuggestionsAsync();

        if (!suggestions.Any())
            return null;

        var topSuggestion = suggestions.First();

        // Time-based context
        var timeContext = hour switch
        {
            < 9 => "morning",
            < 12 => "mid-morning",
            < 14 => "early afternoon",
            < 17 => "afternoon",
            _ => "evening"
        };

        return $"{GetTimeGreeting(hour)} {topSuggestion.Message}";
    }

    #region Analysis Methods

    private async Task<List<ProactiveSuggestion>> AnalyzeOverdueTasksAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var tasks = await _taskService.GetAllAsync();
        var today = DateTime.Today;

        var overdueTasks = tasks
            .Where(t => t.Status != TodoTaskStatus.Completed &&
                       t.DueDate.HasValue &&
                       t.DueDate.Value.Date < today)
            .OrderBy(t => t.DueDate)
            .ToList();

        if (overdueTasks.Count > 0)
        {
            var oldestOverdue = overdueTasks.First();
            var daysOverdue = (today - oldestOverdue.DueDate!.Value.Date).Days;

            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Warning,
                Category = "Tasks",
                Title = "Overdue Tasks",
                Message = overdueTasks.Count == 1
                    ? $"\"{oldestOverdue.Title}\" is {daysOverdue} days overdue"
                    : $"You have {overdueTasks.Count} overdue tasks. Oldest is {daysOverdue} days overdue.",
                ActionLabel = "Review",
                ActionUrl = "/tasks?filter=overdue",
                Priority = Math.Min(10, 5 + overdueTasks.Count),
                Icon = "oi-warning",
                RelatedEntityId = oldestOverdue.Id,
                CreatedAt = DateTime.Now
            });
        }

        // Tasks due today
        var dueToday = tasks
            .Where(t => t.Status != TodoTaskStatus.Completed &&
                       t.DueDate.HasValue &&
                       t.DueDate.Value.Date == today)
            .ToList();

        if (dueToday.Count > 0)
        {
            var firstDueTask = dueToday[0];
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Reminder,
                Category = "Tasks",
                Title = "Due Today",
                Message = dueToday.Count == 1
                    ? $"\"{firstDueTask.Title}\" is due today"
                    : $"{dueToday.Count} tasks are due today",
                ActionLabel = "View",
                ActionUrl = "/tasks?filter=today",
                Priority = 7,
                Icon = "oi-clock",
                CreatedAt = DateTime.Now
            });
        }

        return suggestions;
    }

    private async Task<List<ProactiveSuggestion>> AnalyzeInboxAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var captures = await _captureRepository.GetAllAsync();
        var unprocessed = captures.Where(c => !c.IsProcessed).ToList();

        if (unprocessed.Count > 5)
        {
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Action,
                Category = "Inbox",
                Title = "Inbox Overflow",
                Message = $"{unprocessed.Count} items waiting in your inbox. Time for a quick processing session?",
                ActionLabel = "Process",
                ActionUrl = "/inbox",
                Priority = Math.Min(8, 4 + unprocessed.Count / 5),
                Icon = "oi-inbox",
                CreatedAt = DateTime.Now
            });
        }

        // Check for old captures
        var oldCaptures = unprocessed
            .Where(c => (DateTime.Now - c.CreatedAt).TotalDays > 7)
            .ToList();

        if (oldCaptures.Any())
        {
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Warning,
                Category = "Inbox",
                Title = "Stale Captures",
                Message = $"{oldCaptures.Count} items have been in your inbox for over a week",
                ActionLabel = "Review",
                ActionUrl = "/inbox",
                Priority = 5,
                Icon = "oi-clock",
                CreatedAt = DateTime.Now
            });
        }

        return suggestions;
    }

    private async Task<List<ProactiveSuggestion>> AnalyzeStaleGoalsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var goals = await _goalService.GetAllAsync();
        var activeGoals = goals.Where(g => g.Status == GoalStatus.Active).ToList();

        foreach (var goal in activeGoals)
        {
            var daysSinceUpdate = (DateTime.Now - goal.ModifiedAt).TotalDays;

            if (daysSinceUpdate > 14)
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Id = Guid.NewGuid(),
                    Type = SuggestionType.Insight,
                    Category = "Goals",
                    Title = "Goal Check-in",
                    Message = $"Goal \"{goal.Title}\" hasn't been updated in {(int)daysSinceUpdate} days. Still relevant?",
                    ActionLabel = "Review",
                    ActionUrl = $"/goals/{goal.Id}",
                    Priority = 4,
                    Icon = "oi-target",
                    RelatedEntityId = goal.Id,
                    CreatedAt = DateTime.Now
                });
                break; // Only show one stale goal suggestion
            }
        }

        // Goals approaching deadline
        var todayDt = DateTime.Today;
        var weekFromNowDt = DateTime.Today.AddDays(7);
        var approachingDeadline = activeGoals
            .Where(g => g.TargetDate.HasValue &&
                       g.TargetDate.Value > todayDt &&
                       g.TargetDate.Value <= weekFromNowDt)
            .ToList();

        if (approachingDeadline.Any())
        {
            var goal = approachingDeadline.First();
            var daysLeft = (goal.TargetDate!.Value - DateTime.Today).Days;

            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Reminder,
                Category = "Goals",
                Title = "Goal Deadline Approaching",
                Message = $"\"{goal.Title}\" target date is in {daysLeft} days",
                ActionLabel = "View",
                ActionUrl = $"/goals/{goal.Id}",
                Priority = 6,
                Icon = "oi-target",
                RelatedEntityId = goal.Id,
                CreatedAt = DateTime.Now
            });
        }

        return suggestions;
    }

    private async Task<List<ProactiveSuggestion>> AnalyzeHabitStreaksAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var habits = await _habitRepository.GetAllAsync();
        var activeHabits = habits.Where(h => h.IsActive).ToList();

        if (!activeHabits.Any())
            return suggestions;

        var logs = await _habitLogRepository.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);

        foreach (var habit in activeHabits)
        {
            var habitLogs = logs.Where(l => l.HabitId == habit.Id).ToList();
            var completedToday = habitLogs.Any(l => l.Date == today && l.Completed);
            var completedYesterday = habitLogs.Any(l => l.Date == yesterday && l.Completed);

            // Streak at risk
            if (!completedToday && completedYesterday)
            {
                var streak = CalculateStreak(habitLogs, yesterday);
                if (streak >= 3)
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Reminder,
                        Category = "Habits",
                        Title = "Streak at Risk",
                        Message = $"Your {streak}-day streak for \"{habit.Name}\" needs attention today!",
                        ActionLabel = "Complete",
                        ActionUrl = "/habits",
                        Priority = 6,
                        Icon = "oi-fire",
                        RelatedEntityId = habit.Id,
                        CreatedAt = DateTime.Now
                    });
                    break; // Only show one habit warning
                }
            }
        }

        // Celebrate long streaks
        foreach (var habit in activeHabits.Take(3))
        {
            var habitLogs = logs.Where(l => l.HabitId == habit.Id).ToList();
            var streak = CalculateStreak(habitLogs, today);

            if (streak == 7 || streak == 30 || streak == 100)
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Id = Guid.NewGuid(),
                    Type = SuggestionType.Celebration,
                    Category = "Habits",
                    Title = "Streak Milestone!",
                    Message = $"Congratulations! {streak}-day streak on \"{habit.Name}\"!",
                    ActionLabel = null,
                    ActionUrl = null,
                    Priority = 3,
                    Icon = "oi-star",
                    RelatedEntityId = habit.Id,
                    CreatedAt = DateTime.Now
                });
            }
        }

        return suggestions;
    }

    private int CalculateStreak(List<HabitLog> logs, DateOnly fromDate)
    {
        var streak = 0;
        var currentDate = fromDate;

        while (logs.Any(l => l.Date == currentDate && l.Completed))
        {
            streak++;
            currentDate = currentDate.AddDays(-1);
        }

        return streak;
    }

    private async Task<List<ProactiveSuggestion>> AnalyzeTaskPatternsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var tasks = await _taskService.GetAllAsync();
        var incompleteTasks = tasks.Where(t => t.Status != TodoTaskStatus.Completed).ToList();

        // Tasks without due dates
        var noDueDateTasks = incompleteTasks.Where(t => !t.DueDate.HasValue).ToList();
        if (noDueDateTasks.Count > 10)
        {
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Insight,
                Category = "Organization",
                Title = "Unscheduled Tasks",
                Message = $"{noDueDateTasks.Count} tasks have no due date. Consider scheduling them for better planning.",
                ActionLabel = "Review",
                ActionUrl = "/tasks?filter=no-date",
                Priority = 3,
                Icon = "oi-calendar",
                CreatedAt = DateTime.Now
            });
        }

        // Blocked tasks
        var blockedTasks = incompleteTasks.Where(t => t.IsBlocked).ToList();
        if (blockedTasks.Count > 3)
        {
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Warning,
                Category = "Tasks",
                Title = "Blocked Tasks",
                Message = $"{blockedTasks.Count} tasks are blocked. Review blockers to make progress.",
                ActionLabel = "Review",
                ActionUrl = "/tasks?filter=blocked",
                Priority = 5,
                Icon = "oi-ban",
                CreatedAt = DateTime.Now
            });
        }

        // High priority but needs high energy
        var highPriorityHighEnergy = incompleteTasks
            .Where(t => t.Priority == 1 && t.EnergyLevel >= 4) // 1 = High priority
            .ToList();

        if (highPriorityHighEnergy.Count > 5)
        {
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Insight,
                Category = "Planning",
                Title = "High-Energy Backlog",
                Message = $"{highPriorityHighEnergy.Count} high-priority tasks need high energy. Schedule focused time blocks.",
                ActionLabel = "Plan",
                ActionUrl = "/calendar",
                Priority = 4,
                Icon = "oi-bolt",
                CreatedAt = DateTime.Now
            });
        }

        return suggestions;
    }

    private async Task<List<ProactiveSuggestion>> AnalyzeLifeBalanceAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var dimensions = await _balanceDimensionService.GetEnabledDimensionsAsync();
            var ratings = await _balanceDimensionService.GetBalanceRatingsAsync();

            if (!ratings.Any() || !dimensions.Any())
                return suggestions;

            var averageRating = ratings.Values.Average();

            // Find lowest rated dimension
            var lowestDimension = dimensions
                .OrderBy(d => ratings.GetValueOrDefault(d.Id, 5))
                .FirstOrDefault();

            if (lowestDimension != null)
            {
                var lowestRating = ratings.GetValueOrDefault(lowestDimension.Id, 5);
                if (lowestRating <= 3 && lowestRating < averageRating - 1.5)
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Insight,
                        Category = "Life Balance",
                        Title = "Area Needs Attention",
                        Message = $"Your \"{lowestDimension.Name}\" area is rated {lowestRating}/10. Consider setting a goal to improve it.",
                        ActionLabel = "View Balance",
                        ActionUrl = "/balance",
                        Priority = 3,
                        Icon = lowestDimension.Icon,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Check for overall imbalance (high variance)
            var variance = ratings.Values.Select(r => Math.Pow(r - averageRating, 2)).Average();
            if (variance > 6 && averageRating >= 4) // High variance but not all low
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Id = Guid.NewGuid(),
                    Type = SuggestionType.Insight,
                    Category = "Life Balance",
                    Title = "Life Imbalance Detected",
                    Message = "Your life balance shows significant variation between areas. Consider focusing on neglected dimensions.",
                    ActionLabel = "Review Balance",
                    ActionUrl = "/balance",
                    Priority = 4,
                    Icon = "oi-dial",
                    CreatedAt = DateTime.Now
                });
            }
        }
        catch
        {
            // Ignore errors
        }

        return suggestions;
    }

    private async Task<List<ProactiveSuggestion>> GetEnergyBasedSuggestionsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();
        var hour = DateTime.Now.Hour;

        // Determine energy level based on time of day (simplified)
        var estimatedEnergy = hour switch
        {
            >= 9 and <= 11 => 5, // Peak morning
            >= 14 and <= 15 => 2, // Post-lunch dip
            >= 16 and <= 17 => 4, // Afternoon recovery
            _ => 3
        };

        var tasks = await _taskService.GetAllAsync();
        var availableTasks = tasks
            .Where(t => t.Status != TodoTaskStatus.Completed && !t.IsBlocked)
            .ToList();

        if (!availableTasks.Any())
            return suggestions;

        // Get user preferences
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault() ?? new UserPreferences();

        var context = new SchedulingContext
        {
            Preferences = prefs,
            TargetDate = DateOnly.FromDateTime(DateTime.Today),
            TargetHour = hour,
            CurrentEnergyLevel = estimatedEnergy
        };

        var optimized = _taskOptimizer.OptimizeTasks(availableTasks, context);

        if (optimized.Any())
        {
            var topTask = optimized.First().Task;
            suggestions.Add(new ProactiveSuggestion
            {
                Id = Guid.NewGuid(),
                Type = SuggestionType.Action,
                Category = "Next Action",
                Title = "Suggested Task",
                Message = $"Based on your energy level, try: \"{topTask.Title}\"",
                ActionLabel = "Start",
                ActionUrl = $"/tasks/{topTask.Id}",
                Priority = 2,
                Icon = "oi-task",
                RelatedEntityId = topTask.Id,
                CreatedAt = DateTime.Now
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Analyzes calendar patterns for today and surfaces critical meeting insights.
    /// </summary>
    private async Task<List<ProactiveSuggestion>> AnalyzeCalendarPatternsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var analysis = await _meetingInsightService.AnalyzeDayAsync(today);

            // Surface critical insights as suggestions
            foreach (var insight in analysis.Insights.Where(i => i.Severity == MeetingInsightSeverity.Critical).Take(2))
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Id = Guid.NewGuid(),
                    Type = SuggestionType.Warning,
                    Category = "Calendar",
                    Title = GetInsightTitle(insight.Type),
                    Message = insight.Message,
                    ActionLabel = "View Calendar",
                    ActionUrl = "/calendar",
                    Priority = 8,
                    Icon = "oi-calendar",
                    CreatedAt = DateTime.Now
                });
            }

            // No focus time warning
            if (analysis.AvailableFocusMinutes < 60 && analysis.TotalMeetings > 3)
            {
                suggestions.Add(new ProactiveSuggestion
                {
                    Id = Guid.NewGuid(),
                    Type = SuggestionType.Warning,
                    Category = "Focus",
                    Title = "Limited Focus Time",
                    Message = $"Only {analysis.AvailableFocusMinutes} min of focus time available today with {analysis.TotalMeetings} meetings",
                    ActionLabel = "Review Calendar",
                    ActionUrl = "/calendar",
                    Priority = 7,
                    Icon = "oi-timer",
                    CreatedAt = DateTime.Now
                });
            }
        }
        catch
        {
            // Ignore calendar analysis errors
        }

        return suggestions;
    }

    /// <summary>
    /// Analyzes if project-related meetings are scattered vs grouped for focus.
    /// </summary>
    private async Task<List<ProactiveSuggestion>> AnalyzeProjectFocusAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            var events = await _calendarService.GetEventsForRangeAsync(
                weekStart.ToDateTime(TimeOnly.MinValue),
                weekEnd.ToDateTime(TimeOnly.MaxValue));

            var projects = (await _projectService.GetAllAsync())
                .Where(p => p.Status != ProjectStatus.Completed)
                .ToDictionary(p => p.Id, p => p);

            // Group events by linked project
            var eventsByProject = events
                .Where(e => e.LinkedProjectId.HasValue && projects.ContainsKey(e.LinkedProjectId.Value))
                .GroupBy(e => e.LinkedProjectId!.Value)
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var projectGroup in eventsByProject.Take(2))
            {
                var project = projects[projectGroup.Key];
                var daysWithMeetings = projectGroup.Select(e => e.StartTime.Date).Distinct().Count();
                var totalMeetings = projectGroup.Count();

                // If meetings are spread across many days, suggest consolidation
                if (daysWithMeetings >= 3 && totalMeetings <= daysWithMeetings * 2)
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Insight,
                        Category = "Project Focus",
                        Title = "Scattered Project Meetings",
                        Message = $"\"{project.Name}\" has {totalMeetings} meetings spread across {daysWithMeetings} days this week. Consider consolidating for better focus.",
                        ActionLabel = "View Project",
                        ActionUrl = $"/projects/{project.Id}",
                        Priority = 4,
                        Icon = "oi-briefcase",
                        RelatedEntityId = project.Id,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Check for project focus days (single project dominates a day)
            var eventsByDay = events
                .Where(e => e.LinkedProjectId.HasValue)
                .GroupBy(e => e.StartTime.Date)
                .ToList();

            foreach (var dayGroup in eventsByDay)
            {
                var projectCounts = dayGroup.GroupBy(e => e.LinkedProjectId).ToList();
                if (projectCounts.Count == 1 && dayGroup.Count() >= 3)
                {
                    var projectId = projectCounts.First().Key!.Value;
                    if (projects.TryGetValue(projectId, out var project) && dayGroup.Key.Date >= DateTime.Today)
                    {
                        var dayName = dayGroup.Key.DayOfWeek.ToString();
                        suggestions.Add(new ProactiveSuggestion
                        {
                            Id = Guid.NewGuid(),
                            Type = SuggestionType.Celebration,
                            Category = "Focus Day",
                            Title = $"{dayName} Focus Day",
                            Message = $"{dayName} is focused on \"{project.Name}\" with {dayGroup.Count()} related meetings. Great for deep project work!",
                            ActionLabel = null,
                            ActionUrl = null,
                            Priority = 2,
                            Icon = "oi-star",
                            CreatedAt = DateTime.Now
                        });
                        break; // Only show one focus day celebration
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return suggestions;
    }

    /// <summary>
    /// Detects conflicts between high-priority tasks and meeting-heavy days.
    /// </summary>
    private async Task<List<ProactiveSuggestion>> DetectSchedulingConflictsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var tasks = await _taskService.GetAllAsync();

            // Get tasks due in the next 3 days that need significant time
            var urgentTasks = tasks
                .Where(t => t.Status != TodoTaskStatus.Completed &&
                           t.DueDate.HasValue &&
                           t.DueDate.Value.Date >= DateTime.Today &&
                           t.DueDate.Value.Date <= DateTime.Today.AddDays(3) &&
                           (t.Priority == 1 || t.EstimatedMinutes >= 60))
                .ToList();

            foreach (var task in urgentTasks.Take(2))
            {
                var taskDate = DateOnly.FromDateTime(task.DueDate!.Value.Date);
                var dayAnalysis = await _meetingInsightService.AnalyzeDayAsync(taskDate);

                // Check if it's a meeting-heavy day
                var meetingPercent = dayAnalysis.TotalMeetingMinutes > 0
                    ? (dayAnalysis.TotalMeetingMinutes * 100) / 480 // Assume 8-hour day
                    : 0;

                if (meetingPercent >= 50 && task.EstimatedMinutes > dayAnalysis.AvailableFocusMinutes)
                {
                    var dayName = taskDate == today ? "Today" : task.DueDate.Value.DayOfWeek.ToString();
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Warning,
                        Category = "Scheduling Conflict",
                        Title = "Task vs Meetings Conflict",
                        Message = $"\"{task.Title}\" ({task.EstimatedMinutes} min) is due {dayName} but only {dayAnalysis.AvailableFocusMinutes} min focus time available",
                        ActionLabel = "Review",
                        ActionUrl = $"/tasks/{task.Id}",
                        Priority = 7,
                        Icon = "oi-warning",
                        RelatedEntityId = task.Id,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Check for high-energy tasks on exhausting meeting days
            var highEnergyTasks = tasks
                .Where(t => t.Status != TodoTaskStatus.Completed &&
                           t.ScheduledDate.HasValue &&
                           t.EnergyLevel >= 4 &&
                           t.ScheduledDate.Value.Date >= DateTime.Today &&
                           t.ScheduledDate.Value.Date <= DateTime.Today.AddDays(2))
                .ToList();

            foreach (var task in highEnergyTasks.Take(1))
            {
                var taskDate = DateOnly.FromDateTime(task.ScheduledDate!.Value.Date);
                var dayAnalysis = await _meetingInsightService.AnalyzeDayAsync(taskDate);

                if (dayAnalysis.TotalMeetings >= 5 || dayAnalysis.MaxConsecutiveMinutes >= 120)
                {
                    var dayName = taskDate == today ? "today" : task.ScheduledDate.Value.DayOfWeek.ToString();
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Insight,
                        Category = "Energy Planning",
                        Title = "Energy Mismatch",
                        Message = $"High-energy task \"{task.Title}\" is scheduled on {dayName} with {dayAnalysis.TotalMeetings} meetings. Consider rescheduling.",
                        ActionLabel = "Reschedule",
                        ActionUrl = "/calendar",
                        Priority = 5,
                        Icon = "oi-bolt",
                        RelatedEntityId = task.Id,
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return suggestions;
    }

    private static string GetInsightTitle(MeetingInsightType type)
    {
        return type switch
        {
            MeetingInsightType.BackToBackMeetings => "Back-to-Back Meetings",
            MeetingInsightType.ConsecutiveMeetingsExceeded => "Meeting Overload",
            MeetingInsightType.MeetingHeavyDay => "Meeting-Heavy Day",
            MeetingInsightType.NoFocusTime => "No Focus Time",
            MeetingInsightType.NoPrepTime => "Missing Prep Time",
            MeetingInsightType.LunchConflict => "Lunch Conflict",
            _ => "Calendar Alert"
        };
    }

    private static string GetTimeGreeting(int hour)
    {
        return hour switch
        {
            < 12 => "Good morning!",
            < 17 => "Good afternoon!",
            _ => "Good evening!"
        };
    }

    /// <summary>
    /// Analyzes balance dimensions to find neglected areas and suggest goals.
    /// </summary>
    private async Task<List<ProactiveSuggestion>> AnalyzeBalanceDimensionsAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var dimensions = await _balanceDimensionService.GetEnabledDimensionsAsync();
            var ratings = await _balanceDimensionService.GetBalanceRatingsAsync();
            var goals = await _goalService.GetAllAsync();
            var activeGoals = goals.Where(g => g.Status == GoalStatus.Active).ToList();

            if (!ratings.Any() || !dimensions.Any())
                return suggestions;

            // Find dimensions with low ratings and no linked goals
            foreach (var dimension in dimensions)
            {
                var rating = ratings.GetValueOrDefault(dimension.Id, 5);
                var linkedGoals = activeGoals.Where(g =>
                    g.BalanceDimensionIds.Contains(dimension.Id) ||
                    g.PrimaryBalanceDimensionId == dimension.Id).ToList();

                if (rating <= 4 && !linkedGoals.Any())
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Insight,
                        Category = "Life Balance",
                        Title = $"{dimension.Name} Needs Goals",
                        Message = $"Your \"{dimension.Name}\" area is rated {rating}/10 with no active goals. Consider setting a goal to improve it.",
                        ActionLabel = "Create Goal",
                        ActionUrl = "/goals/new",
                        Priority = 4,
                        Icon = dimension.Icon,
                        CreatedAt = DateTime.Now
                    });
                    break; // Only show one suggestion
                }
            }

            // Check for dimensions with good ratings but many goals (overcommitted)
            var avgRating = ratings.Values.Average();
            foreach (var dimension in dimensions)
            {
                var rating = ratings.GetValueOrDefault(dimension.Id, 5);
                var linkedGoals = activeGoals.Where(g =>
                    g.BalanceDimensionIds.Contains(dimension.Id) ||
                    g.PrimaryBalanceDimensionId == dimension.Id).ToList();

                if (rating >= 7 && linkedGoals.Count >= 5)
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Insight,
                        Category = "Life Balance",
                        Title = "Goal Overload",
                        Message = $"\"{dimension.Name}\" has {linkedGoals.Count} active goals. Consider completing or archiving some before adding more.",
                        ActionLabel = "View Goals",
                        ActionUrl = "/goals",
                        Priority = 3,
                        Icon = "oi-target",
                        CreatedAt = DateTime.Now
                    });
                    break;
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return suggestions;
    }

    /// <summary>
    /// Checks if Google Calendar needs syncing and suggests it.
    /// </summary>
    private async Task<List<ProactiveSuggestion>> AnalyzeGoogleCalendarSyncAsync()
    {
        var suggestions = new List<ProactiveSuggestion>();

        try
        {
            var lastSync = await _googleCalendarSyncService.GetLastSyncTimeAsync();

            if (lastSync.HasValue)
            {
                var daysSinceSync = (DateTime.UtcNow - lastSync.Value).TotalDays;

                if (daysSinceSync > 1)
                {
                    suggestions.Add(new ProactiveSuggestion
                    {
                        Id = Guid.NewGuid(),
                        Type = SuggestionType.Reminder,
                        Category = "Calendar",
                        Title = "Calendar Out of Sync",
                        Message = $"Google Calendar hasn't synced in {(int)daysSinceSync} days. Sync now to keep your calendar current.",
                        ActionLabel = "Sync Now",
                        ActionUrl = "/settings/calendar-providers",
                        Priority = 5,
                        Icon = "oi-loop-circular",
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }
        catch
        {
            // Ignore errors - Google Calendar may not be configured
        }

        return suggestions;
    }

    #endregion
}

/// <summary>
/// Interface for proactive suggestions service
/// </summary>
public interface IProactiveSuggestionsService
{
    event Action? OnSuggestionsChanged;
    Task<IReadOnlyList<ProactiveSuggestion>> GetSuggestionsAsync(bool forceRefresh = false);
    Task RefreshSuggestionsAsync();
    void DismissSuggestion(Guid suggestionId);
    Task<string?> GetQuickInsightAsync();
}

/// <summary>
/// A proactive suggestion generated by the system
/// </summary>
public class ProactiveSuggestion
{
    public Guid Id { get; init; }
    public SuggestionType Type { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionLabel { get; init; }
    public string? ActionUrl { get; init; }
    public int Priority { get; init; } // 1-10, higher = more important
    public string Icon { get; init; } = "oi-info";
    public Guid? RelatedEntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Types of proactive suggestions
/// </summary>
public enum SuggestionType
{
    Action,      // Something the user should do
    Reminder,    // Time-sensitive reminder
    Warning,     // Something needs attention
    Insight,     // Pattern or observation
    Celebration  // Achievement or milestone
}
