using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class TaskService : ITaskService
{
    private readonly IRepository<TodoTask> _repository;
    private readonly IUserPreferencesProvider _preferencesProvider;
    private readonly ITaskIconIntelligenceService? _iconIntelligenceService;

    public TaskService(
        IRepository<TodoTask> repository,
        IUserPreferencesProvider preferencesProvider,
        ITaskIconIntelligenceService? iconIntelligenceService = null)
    {
        _repository = repository;
        _preferencesProvider = preferencesProvider;
        _iconIntelligenceService = iconIntelligenceService;
    }

    /// <summary>
    /// Filters out sample data when ShowSampleData preference is false
    /// </summary>
    private async Task<IEnumerable<TodoTask>> FilterSampleDataAsync(IEnumerable<TodoTask> tasks)
    {
        if (await _preferencesProvider.ShowSampleDataAsync())
            return tasks;
        return tasks.Where(t => !t.IsSampleData);
    }

    public async Task<TodoTask?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<TodoTask>> GetAllAsync()
    {
        var tasks = await _repository.GetAllAsync();
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetByStatusAsync(TodoTaskStatus status)
    {
        var tasks = await _repository.QueryAsync(t => t.Status == status);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetByProjectAsync(Guid projectId)
    {
        var tasks = await _repository.QueryAsync(t => t.ProjectId == projectId);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetNextActionsAsync()
    {
        var tasks = await _repository.QueryAsync(t => t.Status == TodoTaskStatus.NextAction);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetWaitingForAsync()
    {
        var tasks = await _repository.QueryAsync(t => t.Status == TodoTaskStatus.WaitingFor);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetScheduledAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var tasks = await _repository.QueryAsync(t => t.Status == TodoTaskStatus.Scheduled && t.ScheduledDate != null);
        tasks = await FilterSampleDataAsync(tasks);

        if (fromDate.HasValue)
            tasks = tasks.Where(t => t.ScheduledDate >= fromDate.Value);
        if (toDate.HasValue)
            tasks = tasks.Where(t => t.ScheduledDate <= toDate.Value);

        return tasks.OrderBy(t => t.ScheduledDate);
    }

    public async Task<IEnumerable<TodoTask>> GetSomedayMaybeAsync()
    {
        var tasks = await _repository.QueryAsync(t => t.Status == TodoTaskStatus.SomedayMaybe);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> GetOverdueAsync()
    {
        var now = DateTime.UtcNow;
        var tasks = await _repository.QueryAsync(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            t.Status != TodoTaskStatus.Completed &&
            t.Status != TodoTaskStatus.Deleted);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<IEnumerable<TodoTask>> SearchAsync(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var lowerQuery = query.ToLowerInvariant();
        var tasks = await _repository.QueryAsync(t =>
            t.Title.ToLower().Contains(lowerQuery) ||
            (t.Description != null && t.Description.ToLower().Contains(lowerQuery)) ||
            (t.Notes != null && t.Notes.ToLower().Contains(lowerQuery)));
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<TodoTask> CreateAsync(TodoTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        // Auto-detect icon if enabled and no manual icon set
        if (task.IsIconAutoDetected && string.IsNullOrEmpty(task.Icon) && _iconIntelligenceService != null)
        {
            var result = _iconIntelligenceService.AnalyzeTask(task.Title, task.Description, task.Contexts);
            task.Icon = result.Icon;
            task.DetectedCategory = result.Category;
        }

        return await _repository.AddAsync(task);
    }

    public async Task<TodoTask> UpdateAsync(TodoTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        // Re-detect icon if auto-detection is enabled and title/description might have changed
        if (task.IsIconAutoDetected && _iconIntelligenceService != null)
        {
            var result = _iconIntelligenceService.AnalyzeTask(task.Title, task.Description, task.Contexts);
            task.Icon = result.Icon;
            task.DetectedCategory = result.Category;
        }

        return await _repository.UpdateAsync(task);
    }

    public async Task<TodoTask> CompleteAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Task {id} not found");

        task.Status = TodoTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        return await _repository.UpdateAsync(task);
    }

    public async Task<TodoTask> DeactivateAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Task {id} not found");

        // Move task back to Inbox and clear scheduling info
        task.Status = TodoTaskStatus.Inbox;
        task.ScheduledDate = null;
        task.ScheduledStartTime = null;
        return await _repository.UpdateAsync(task);
    }

    public async Task<TodoTask> MoveToSomedayAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Task {id} not found");

        task.Status = TodoTaskStatus.SomedayMaybe;
        task.ScheduledDate = null;
        task.ScheduledStartTime = null;
        return await _repository.UpdateAsync(task);
    }

    public async Task<TodoTask> ScheduleAsync(Guid id, DateTime scheduledDate, DateTime? startTime = null)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Task {id} not found");

        task.Status = TodoTaskStatus.Scheduled;
        task.ScheduledDate = scheduledDate;
        task.ScheduledStartTime = startTime;
        return await _repository.UpdateAsync(task);
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task != null)
        {
            task.Status = TodoTaskStatus.Deleted;
            await _repository.UpdateAsync(task);
        }
    }

    public async Task<int> GetCompletedTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        var showSample = await _preferencesProvider.ShowSampleDataAsync();
        return await _repository.CountAsync(t =>
            t.Status == TodoTaskStatus.Completed &&
            t.CompletedAt.HasValue &&
            t.CompletedAt.Value.Date == today &&
            (showSample || !t.IsSampleData));
    }

    // Subtask operations
    public async Task<IEnumerable<TodoTask>> GetSubtasksAsync(Guid parentTaskId)
    {
        return await _repository.QueryAsync(t => t.ParentTaskId == parentTaskId);
    }

    public async Task<TodoTask> CreateSubtaskAsync(Guid parentTaskId, TodoTask subtask)
    {
        ArgumentNullException.ThrowIfNull(subtask);

        var parentTask = await _repository.GetByIdAsync(parentTaskId);
        if (parentTask == null)
            throw new InvalidOperationException($"Parent task {parentTaskId} not found");

        // Set up the subtask relationship
        subtask.ParentTaskId = parentTaskId;
        subtask.ProjectId = parentTask.ProjectId; // Inherit project from parent

        var createdSubtask = await _repository.AddAsync(subtask);

        // Update parent's subtask list
        parentTask.SubtaskIds.Add(createdSubtask.Id);
        await _repository.UpdateAsync(parentTask);

        return createdSubtask;
    }

    public async Task<TodoTask> MakeSubtaskOfAsync(Guid taskId, Guid parentTaskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        var parentTask = await _repository.GetByIdAsync(parentTaskId);
        if (parentTask == null)
            throw new InvalidOperationException($"Parent task {parentTaskId} not found");

        // Remove from old parent if it was already a subtask
        if (task.ParentTaskId.HasValue)
        {
            var oldParent = await _repository.GetByIdAsync(task.ParentTaskId.Value);
            if (oldParent != null)
            {
                oldParent.SubtaskIds.Remove(taskId);
                await _repository.UpdateAsync(oldParent);
            }
        }

        // Set up new parent relationship
        task.ParentTaskId = parentTaskId;
        parentTask.SubtaskIds.Add(taskId);

        await _repository.UpdateAsync(parentTask);
        return await _repository.UpdateAsync(task);
    }

    public async Task<TodoTask> PromoteToStandaloneAsync(Guid subtaskId)
    {
        var subtask = await _repository.GetByIdAsync(subtaskId);
        if (subtask == null)
            throw new InvalidOperationException($"Subtask {subtaskId} not found");

        if (!subtask.ParentTaskId.HasValue)
            return subtask; // Already standalone

        // Remove from parent's subtask list
        var parentTask = await _repository.GetByIdAsync(subtask.ParentTaskId.Value);
        if (parentTask != null)
        {
            parentTask.SubtaskIds.Remove(subtaskId);
            await _repository.UpdateAsync(parentTask);
        }

        // Clear parent reference
        subtask.ParentTaskId = null;
        return await _repository.UpdateAsync(subtask);
    }

    // Blocking/dependency operations
    public async Task<IEnumerable<TodoTask>> GetBlockedTasksAsync()
    {
        var allTasks = await _repository.GetAllAsync();
        allTasks = await FilterSampleDataAsync(allTasks);
        var taskList = allTasks.ToList();

        var completedTaskIds = taskList
            .Where(t => t.Status == TodoTaskStatus.Completed)
            .Select(t => t.Id)
            .ToHashSet();

        // Return tasks that have blockers and at least one blocker is not completed
        return taskList.Where(t =>
            t.BlockedByTaskIds.Any() &&
            t.BlockedByTaskIds.Any(blockerId => !completedTaskIds.Contains(blockerId)) &&
            t.Status != TodoTaskStatus.Completed &&
            t.Status != TodoTaskStatus.Deleted);
    }

    public async Task<IEnumerable<TodoTask>> GetAvailableTasksAsync()
    {
        var allTasks = await _repository.GetAllAsync();
        allTasks = await FilterSampleDataAsync(allTasks);
        var taskList = allTasks.ToList();

        var completedTaskIds = taskList
            .Where(t => t.Status == TodoTaskStatus.Completed)
            .Select(t => t.Id)
            .ToHashSet();

        // Return tasks that are NextAction and not blocked
        return taskList.Where(t =>
            t.Status == TodoTaskStatus.NextAction &&
            (!t.BlockedByTaskIds.Any() ||
             t.BlockedByTaskIds.All(blockerId => completedTaskIds.Contains(blockerId))));
    }

    public async Task<bool> IsTaskBlockedAsync(Guid taskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task == null || !task.BlockedByTaskIds.Any())
            return false;

        // Check if any blocking task is not completed
        foreach (var blockerId in task.BlockedByTaskIds)
        {
            var blocker = await _repository.GetByIdAsync(blockerId);
            if (blocker != null && blocker.Status != TodoTaskStatus.Completed)
                return true;
        }

        return false;
    }

    public async Task AddBlockingDependencyAsync(Guid taskId, Guid blockerTaskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        if (!task.BlockedByTaskIds.Contains(blockerTaskId))
        {
            task.BlockedByTaskIds.Add(blockerTaskId);
            await _repository.UpdateAsync(task);
        }
    }

    public async Task RemoveBlockingDependencyAsync(Guid taskId, Guid blockerTaskId)
    {
        var task = await _repository.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        if (task.BlockedByTaskIds.Remove(blockerTaskId))
        {
            await _repository.UpdateAsync(task);
        }
    }

    // Recurring task operations
    public async Task<IEnumerable<TodoTask>> GetRecurringTasksAsync()
    {
        var tasks = await _repository.QueryAsync(t => t.IsRecurring);
        return await FilterSampleDataAsync(tasks);
    }

    public async Task<TodoTask> CompleteRecurringTaskAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Task {id} not found");

        if (!task.IsRecurring)
        {
            // Not recurring, just complete normally
            return await CompleteAsync(id);
        }

        // Mark current task as completed
        task.Status = TodoTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        task.LastRecurrenceDate = DateTime.UtcNow;
        await _repository.UpdateAsync(task);

        // Create next occurrence
        var nextTask = new TodoTask
        {
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            Status = TodoTaskStatus.NextAction,
            Contexts = new List<string>(task.Contexts),
            Category = task.Category,
            EnergyLevel = task.EnergyLevel,
            EstimatedMinutes = task.EstimatedMinutes,
            Priority = task.Priority,
            RequiresDeepWork = task.RequiresDeepWork,
            Notes = task.Notes,
            Tags = new List<string>(task.Tags),
            Links = new List<string>(task.Links),
            WhoFor = task.WhoFor,
            IsRecurring = true,
            RecurrencePattern = task.RecurrencePattern,
            RecurrenceIntervalDays = task.RecurrenceIntervalDays,
            ScheduledDate = CalculateNextOccurrence(task),
            DueDate = task.DueDate.HasValue ? CalculateNextOccurrence(task) : null
        };

        await _repository.AddAsync(nextTask);

        return task;
    }

    public DateTime CalculateNextOccurrence(TodoTask task)
    {
        var baseDate = task.LastRecurrenceDate ?? task.ScheduledDate ?? DateTime.UtcNow;

        return task.RecurrencePattern switch
        {
            RecurrencePattern.Daily => baseDate.AddDays(1),
            RecurrencePattern.Weekdays => GetNextWeekday(baseDate),
            RecurrencePattern.Weekly => baseDate.AddDays(7),
            RecurrencePattern.Biweekly => baseDate.AddDays(14),
            RecurrencePattern.Monthly => baseDate.AddMonths(1),
            RecurrencePattern.Quarterly => baseDate.AddMonths(3),
            RecurrencePattern.Yearly => baseDate.AddYears(1),
            RecurrencePattern.Custom => baseDate.AddDays(task.RecurrenceIntervalDays ?? 1),
            _ => baseDate.AddDays(1)
        };
    }

    private static DateTime GetNextWeekday(DateTime date)
    {
        var nextDay = date.AddDays(1);
        while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    // Batch operations
    public async Task BatchDeleteAsync(IEnumerable<Guid> taskIds)
    {
        ArgumentNullException.ThrowIfNull(taskIds);

        foreach (var id in taskIds)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task != null)
            {
                task.Status = TodoTaskStatus.Deleted;
                await _repository.UpdateAsync(task);
            }
        }
    }

    public async Task BatchCompleteAsync(IEnumerable<Guid> taskIds)
    {
        ArgumentNullException.ThrowIfNull(taskIds);

        var now = DateTime.UtcNow;
        foreach (var id in taskIds)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task != null)
            {
                // Handle recurring tasks
                if (task.IsRecurring)
                {
                    await CompleteRecurringTaskAsync(id);
                }
                else
                {
                    task.Status = TodoTaskStatus.Completed;
                    task.CompletedAt = now;
                    await _repository.UpdateAsync(task);
                }
            }
        }
    }

    public async Task BatchChangeStatusAsync(IEnumerable<Guid> taskIds, TodoTaskStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(taskIds);

        foreach (var id in taskIds)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task != null)
            {
                task.Status = newStatus;

                // Clear scheduling info when moving away from scheduled status
                if (newStatus != TodoTaskStatus.Scheduled)
                {
                    task.ScheduledDate = null;
                    task.ScheduledStartTime = null;
                }

                // Clear waiting info when moving away from waiting status
                if (newStatus != TodoTaskStatus.WaitingFor)
                {
                    task.WaitingForNote = null;
                    task.WaitingForSince = null;
                    task.WaitingForContactId = null;
                }

                // Set completion time if marking as completed
                if (newStatus == TodoTaskStatus.Completed && !task.CompletedAt.HasValue)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }

                await _repository.UpdateAsync(task);
            }
        }
    }

    public async Task BatchChangePriorityAsync(IEnumerable<Guid> taskIds, int priority)
    {
        ArgumentNullException.ThrowIfNull(taskIds);
        if (priority < 1 || priority > 3)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be between 1 and 3");

        foreach (var id in taskIds)
        {
            var task = await _repository.GetByIdAsync(id);
            if (task != null)
            {
                task.Priority = priority;
                await _repository.UpdateAsync(task);
            }
        }
    }
}
