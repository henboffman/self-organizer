using Microsoft.AspNetCore.Components;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Components.Shared;

public partial class TaskLinkingModal
{
    [Parameter] public string Title { get; set; } = "Link Task";
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<TodoTask> OnTaskSelected { get; set; }
    [Parameter] public List<TodoTask> Tasks { get; set; } = new();
    [Parameter] public HashSet<Guid> ExcludedTaskIds { get; set; } = new();
    [Parameter] public List<Project>? Projects { get; set; }
    [Parameter] public bool ShowProjectInfo { get; set; } = true;

    private ElementReference _searchInput;
    private string _searchQuery = string.Empty;
    private bool _showAdvancedFilters = false;
    private QuickFilter _activeQuickFilter = QuickFilter.All;

    // Advanced filter values
    private string _filterStatus = string.Empty;
    private string _filterPriority = string.Empty;
    private string _filterDueDate = string.Empty;
    private string _filterEnergyLevel = string.Empty;
    private string _filterTimeEstimate = string.Empty;
    private string _filterDeepWork = string.Empty;
    private HashSet<string> _selectedTags = new();
    private HashSet<string> _selectedContexts = new();

    // Sorting
    private string _sortBy = "priority";

    private enum QuickFilter
    {
        All,
        NextActions,
        HighPriority,
        DueSoon,
        Unassigned
    }

    protected override void OnParametersSet()
    {
        if (IsVisible)
        {
            // Reset filters when modal opens
            _searchQuery = string.Empty;
            _activeQuickFilter = QuickFilter.All;
        }
    }

    private IEnumerable<string> AvailableTags => Tasks
        .Where(t => !ExcludedTaskIds.Contains(t.Id))
        .SelectMany(t => t.Tags)
        .Distinct()
        .OrderBy(t => t);

    private IEnumerable<string> AvailableContexts => Tasks
        .Where(t => !ExcludedTaskIds.Contains(t.Id))
        .SelectMany(t => t.Contexts)
        .Distinct()
        .OrderBy(c => c);

    private void ClearSearch()
    {
        _searchQuery = string.Empty;
    }

    private void SetQuickFilter(QuickFilter filter)
    {
        _activeQuickFilter = filter;
    }

    private void ToggleAdvancedFilters()
    {
        _showAdvancedFilters = !_showAdvancedFilters;
    }

    private void ToggleTag(string tag)
    {
        if (_selectedTags.Contains(tag))
            _selectedTags.Remove(tag);
        else
            _selectedTags.Add(tag);
    }

    private void ToggleContext(string context)
    {
        if (_selectedContexts.Contains(context))
            _selectedContexts.Remove(context);
        else
            _selectedContexts.Add(context);
    }

    private bool HasActiveAdvancedFilters()
    {
        return !string.IsNullOrEmpty(_filterStatus) ||
               !string.IsNullOrEmpty(_filterPriority) ||
               !string.IsNullOrEmpty(_filterDueDate) ||
               !string.IsNullOrEmpty(_filterEnergyLevel) ||
               !string.IsNullOrEmpty(_filterTimeEstimate) ||
               !string.IsNullOrEmpty(_filterDeepWork) ||
               _selectedTags.Any() ||
               _selectedContexts.Any();
    }

    private void ClearAdvancedFilters()
    {
        _filterStatus = string.Empty;
        _filterPriority = string.Empty;
        _filterDueDate = string.Empty;
        _filterEnergyLevel = string.Empty;
        _filterTimeEstimate = string.Empty;
        _filterDeepWork = string.Empty;
        _selectedTags.Clear();
        _selectedContexts.Clear();
    }

    private void ClearAllFilters()
    {
        _searchQuery = string.Empty;
        _activeQuickFilter = QuickFilter.All;
        ClearAdvancedFilters();
    }

    private int GetFilteredTaskCount(QuickFilter filter)
    {
        var baseTasks = Tasks.Where(t =>
            !ExcludedTaskIds.Contains(t.Id) &&
            t.Status != TodoTaskStatus.Deleted &&
            t.Status != TodoTaskStatus.Completed);

        return filter switch
        {
            QuickFilter.All => baseTasks.Count(),
            QuickFilter.NextActions => baseTasks.Count(t => t.Status == TodoTaskStatus.NextAction),
            QuickFilter.HighPriority => baseTasks.Count(t => t.Priority == 1),
            QuickFilter.DueSoon => baseTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value <= DateTime.Today.AddDays(7)),
            QuickFilter.Unassigned => baseTasks.Count(t => !t.ProjectId.HasValue && !t.GoalIds.Any()),
            _ => baseTasks.Count()
        };
    }

    private IEnumerable<TodoTask> GetFilteredTasks()
    {
        var tasks = Tasks.Where(t =>
            !ExcludedTaskIds.Contains(t.Id) &&
            t.Status != TodoTaskStatus.Deleted &&
            t.Status != TodoTaskStatus.Completed);

        // Apply search query
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            tasks = tasks.Where(t =>
                t.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.Tags.Any(tag => tag.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply quick filter
        tasks = _activeQuickFilter switch
        {
            QuickFilter.NextActions => tasks.Where(t => t.Status == TodoTaskStatus.NextAction),
            QuickFilter.HighPriority => tasks.Where(t => t.Priority == 1),
            QuickFilter.DueSoon => tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value <= DateTime.Today.AddDays(7)),
            QuickFilter.Unassigned => tasks.Where(t => !t.ProjectId.HasValue && !t.GoalIds.Any()),
            _ => tasks
        };

        // Apply advanced filters
        if (!string.IsNullOrEmpty(_filterStatus) && Enum.TryParse<TodoTaskStatus>(_filterStatus, out var status))
        {
            tasks = tasks.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(_filterPriority) && int.TryParse(_filterPriority, out var priority))
        {
            tasks = tasks.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrEmpty(_filterDueDate))
        {
            var today = DateTime.Today;
            tasks = _filterDueDate switch
            {
                "overdue" => tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today),
                "today" => tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today),
                "week" => tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value <= today.AddDays(7)),
                "month" => tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value <= today.AddMonths(1)),
                "none" => tasks.Where(t => !t.DueDate.HasValue),
                _ => tasks
            };
        }

        if (!string.IsNullOrEmpty(_filterEnergyLevel) && int.TryParse(_filterEnergyLevel, out var energy))
        {
            tasks = tasks.Where(t => t.EnergyLevel == energy);
        }

        if (!string.IsNullOrEmpty(_filterTimeEstimate))
        {
            tasks = _filterTimeEstimate switch
            {
                "quick" => tasks.Where(t => t.EstimatedMinutes > 0 && t.EstimatedMinutes < 15),
                "short" => tasks.Where(t => t.EstimatedMinutes >= 15 && t.EstimatedMinutes <= 30),
                "medium" => tasks.Where(t => t.EstimatedMinutes > 30 && t.EstimatedMinutes <= 60),
                "long" => tasks.Where(t => t.EstimatedMinutes > 60),
                _ => tasks
            };
        }

        if (!string.IsNullOrEmpty(_filterDeepWork))
        {
            tasks = _filterDeepWork switch
            {
                "deep" => tasks.Where(t => t.RequiresDeepWork),
                "shallow" => tasks.Where(t => !t.RequiresDeepWork),
                _ => tasks
            };
        }

        if (_selectedTags.Any())
        {
            tasks = tasks.Where(t => t.Tags.Any(tag => _selectedTags.Contains(tag)));
        }

        if (_selectedContexts.Any())
        {
            tasks = tasks.Where(t => t.Contexts.Any(ctx => _selectedContexts.Contains(ctx)));
        }

        // Apply sorting
        tasks = _sortBy switch
        {
            "title" => tasks.OrderBy(t => t.Title),
            "priority" => tasks.OrderBy(t => t.Priority).ThenBy(t => t.DueDate ?? DateTime.MaxValue),
            "dueDate" => tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ThenBy(t => t.Priority),
            "created" => tasks.OrderByDescending(t => t.CreatedAt),
            "updated" => tasks.OrderByDescending(t => t.ModifiedAt),
            _ => tasks.OrderBy(t => t.Priority)
        };

        return tasks;
    }

    private async Task SelectTask(TodoTask task)
    {
        await OnTaskSelected.InvokeAsync(task);
    }

    private string GetStatusBadgeClass(TodoTaskStatus status) => status switch
    {
        TodoTaskStatus.NextAction => "status-badge-nextaction",
        TodoTaskStatus.Active => "status-badge-active",
        TodoTaskStatus.Inbox => "status-badge-inbox",
        TodoTaskStatus.WaitingFor => "status-badge-waitingfor",
        TodoTaskStatus.Scheduled => "status-badge-scheduled",
        TodoTaskStatus.SomedayMaybe => "status-badge-somedaymaybe",
        TodoTaskStatus.Reference => "status-badge-somedaymaybe",
        _ => "status-badge-inbox"
    };

    private string GetDueDateClass(DateTime dueDate)
    {
        var today = DateTime.Today;
        if (dueDate < today)
            return "due-date-overdue small";
        if (dueDate == today)
            return "due-date-today small";
        if (dueDate <= today.AddDays(3))
            return "due-date-soon small";
        return "due-date-normal small";
    }

    private string FormatDueDate(DateTime dueDate)
    {
        var today = DateTime.Today;
        var diff = (dueDate.Date - today).Days;

        return diff switch
        {
            < 0 => $"Overdue ({Math.Abs(diff)}d)",
            0 => "Today",
            1 => "Tomorrow",
            <= 7 => dueDate.ToString("ddd"),
            _ => dueDate.ToString("MMM d")
        };
    }

    private string FormatMinutes(int minutes)
    {
        if (minutes < 60)
            return $"{minutes}m";
        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
    }

    private string GetEnergyIndicator(int level)
    {
        return level switch
        {
            1 => "\u26a1", // Low energy symbol
            2 => "\u26a1\u26a1",
            3 => "\u26a1\u26a1\u26a1",
            4 => "\u26a1\u26a1\u26a1\u26a1",
            5 => "\u26a1\u26a1\u26a1\u26a1\u26a1",
            _ => ""
        };
    }

    private string? GetProjectName(Guid projectId)
    {
        return Projects?.FirstOrDefault(p => p.Id == projectId)?.Name;
    }
}
