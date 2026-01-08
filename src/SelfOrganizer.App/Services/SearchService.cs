using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

public class SearchService : ISearchService
{
    private readonly IRepository<TodoTask> _taskRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<CalendarEvent> _eventRepository;
    private readonly IRepository<CaptureItem> _captureRepository;
    private readonly IRepository<Goal> _goalRepository;

    private static readonly List<QuickAction> _quickActions = new()
    {
        new QuickAction { Id = "capture", Title = "Quick Capture", Shortcut = "C", Icon = "oi-plus" },
        new QuickAction { Id = "inbox", Title = "Go to Inbox", Shortcut = "I", Icon = "oi-inbox" },
        new QuickAction { Id = "tasks", Title = "View Tasks", Shortcut = "T", Icon = "oi-task" },
        new QuickAction { Id = "projects", Title = "View Projects", Shortcut = "P", Icon = "oi-folder" },
        new QuickAction { Id = "calendar", Title = "View Calendar", Shortcut = "K", Icon = "oi-calendar" },
        new QuickAction { Id = "review", Title = "Daily Review", Shortcut = "R", Icon = "oi-eye" },
    };

    public SearchService(
        IRepository<TodoTask> taskRepository,
        IRepository<Project> projectRepository,
        IRepository<CalendarEvent> eventRepository,
        IRepository<CaptureItem> captureRepository,
        IRepository<Goal> goalRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _eventRepository = eventRepository;
        _captureRepository = captureRepository;
        _goalRepository = goalRepository;
    }

    public async Task<SearchResults> SearchAsync(string query, SearchOptions? options = null)
    {
        options ??= new SearchOptions();
        var results = new SearchResults();
        var allResults = new List<SearchResult>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return results;
        }

        var lowerQuery = query.ToLowerInvariant();
        var filterTypes = options.FilterTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Search Tasks
        if (filterTypes == null || filterTypes.Contains("task"))
        {
            try
            {
                var tasks = await _taskRepository.GetAllAsync();
                foreach (var task in tasks.Where(t => t.Status != TodoTaskStatus.Deleted))
                {
                    string? matchedField = null;

                    if (task.Title.ToLowerInvariant().Contains(lowerQuery))
                    {
                        matchedField = "title";
                    }
                    else if (task.Description?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "description";
                    }
                    else if (task.Notes?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "notes";
                    }

                    if (matchedField != null)
                    {
                        allResults.Add(new SearchResult
                        {
                            Type = "task",
                            Id = task.Id,
                            Title = task.Title,
                            Subtitle = GetTaskSubtitle(task),
                            MatchedField = matchedField,
                            Icon = "oi-task",
                            NavigationUrl = $"/tasks/{task.Id}"
                        });
                    }
                }
            }
            catch { /* Continue searching other types */ }
        }

        // Search Projects
        if (filterTypes == null || filterTypes.Contains("project"))
        {
            try
            {
                var projects = await _projectRepository.GetAllAsync();
                foreach (var project in projects.Where(p => p.Status != ProjectStatus.Deleted))
                {
                    string? matchedField = null;

                    if (project.Name.ToLowerInvariant().Contains(lowerQuery))
                    {
                        matchedField = "name";
                    }
                    else if (project.Description?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "description";
                    }
                    else if (project.DesiredOutcome?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "desired outcome";
                    }

                    if (matchedField != null)
                    {
                        allResults.Add(new SearchResult
                        {
                            Type = "project",
                            Id = project.Id,
                            Title = project.Name,
                            Subtitle = GetProjectSubtitle(project),
                            MatchedField = matchedField,
                            Icon = "oi-folder",
                            NavigationUrl = $"/projects/{project.Id}"
                        });
                    }
                }
            }
            catch { /* Continue searching other types */ }
        }

        // Search Calendar Events
        if (filterTypes == null || filterTypes.Contains("event"))
        {
            try
            {
                var events = await _eventRepository.GetAllAsync();
                foreach (var evt in events)
                {
                    string? matchedField = null;

                    if (evt.Title.ToLowerInvariant().Contains(lowerQuery))
                    {
                        matchedField = "title";
                    }
                    else if (evt.Location?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "location";
                    }
                    else if (evt.Description?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "description";
                    }

                    if (matchedField != null)
                    {
                        allResults.Add(new SearchResult
                        {
                            Type = "event",
                            Id = evt.Id,
                            Title = evt.Title,
                            Subtitle = GetEventSubtitle(evt),
                            MatchedField = matchedField,
                            Icon = "oi-calendar",
                            NavigationUrl = $"/calendar?date={evt.StartTime:yyyy-MM-dd}"
                        });
                    }
                }
            }
            catch { /* Continue searching other types */ }
        }

        // Search Captures (unprocessed inbox items)
        if (filterTypes == null || filterTypes.Contains("capture"))
        {
            try
            {
                var captures = await _captureRepository.GetAllAsync();
                foreach (var capture in captures.Where(c => !c.IsProcessed))
                {
                    if (capture.RawText.ToLowerInvariant().Contains(lowerQuery) ||
                        capture.CleanedText.ToLowerInvariant().Contains(lowerQuery))
                    {
                        allResults.Add(new SearchResult
                        {
                            Type = "capture",
                            Id = capture.Id,
                            Title = TruncateText(capture.CleanedText.Length > 0 ? capture.CleanedText : capture.RawText, 60),
                            Subtitle = "Inbox item",
                            MatchedField = "text",
                            Icon = "oi-inbox",
                            NavigationUrl = "/inbox"
                        });
                    }
                }
            }
            catch { /* Continue searching other types */ }
        }

        // Search Goals
        if (filterTypes == null || filterTypes.Contains("goal"))
        {
            try
            {
                var goals = await _goalRepository.GetAllAsync();
                foreach (var goal in goals.Where(g => g.Status != GoalStatus.Archived))
                {
                    string? matchedField = null;

                    if (goal.Title.ToLowerInvariant().Contains(lowerQuery))
                    {
                        matchedField = "title";
                    }
                    else if (goal.Description?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "description";
                    }
                    else if (goal.DesiredOutcome?.ToLowerInvariant().Contains(lowerQuery) == true)
                    {
                        matchedField = "desired outcome";
                    }

                    if (matchedField != null)
                    {
                        allResults.Add(new SearchResult
                        {
                            Type = "goal",
                            Id = goal.Id,
                            Title = goal.Title,
                            Subtitle = GetGoalSubtitle(goal),
                            MatchedField = matchedField,
                            Icon = "oi-target",
                            NavigationUrl = "/goals"
                        });
                    }
                }
            }
            catch { /* Continue searching other types */ }
        }

        // Apply max results limit
        results.Results = allResults.Take(options.MaxResults).ToList();

        // Calculate counts by type
        results.CountByType = allResults
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return results;
    }

    public IEnumerable<QuickAction> GetQuickActions()
    {
        return _quickActions;
    }

    private static string GetTaskSubtitle(TodoTask task)
    {
        var parts = new List<string>();

        if (task.Status != TodoTaskStatus.NextAction)
        {
            parts.Add(task.Status.ToString());
        }

        if (task.DueDate.HasValue)
        {
            parts.Add($"Due {task.DueDate.Value:MMM d}");
        }

        if (task.Priority == 1)
        {
            parts.Add("High priority");
        }

        return parts.Count > 0 ? string.Join(" - ", parts) : "Task";
    }

    private static string GetProjectSubtitle(Project project)
    {
        var parts = new List<string>();

        parts.Add(project.Status.ToString());

        if (project.DueDate.HasValue)
        {
            parts.Add($"Due {project.DueDate.Value:MMM d}");
        }

        return string.Join(" - ", parts);
    }

    private static string GetEventSubtitle(CalendarEvent evt)
    {
        var dateStr = evt.StartTime.Date == DateTime.Today
            ? "Today"
            : evt.StartTime.Date == DateTime.Today.AddDays(1)
                ? "Tomorrow"
                : evt.StartTime.ToString("MMM d");

        var timeStr = evt.IsAllDay ? "All day" : evt.StartTime.ToString("h:mm tt");

        return $"{dateStr} at {timeStr}";
    }

    private static string GetGoalSubtitle(Goal goal)
    {
        var parts = new List<string>();

        parts.Add(goal.Timeframe.ToString());

        if (goal.ProgressPercent > 0)
        {
            parts.Add($"{goal.ProgressPercent}% complete");
        }

        return string.Join(" - ", parts);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }
}
