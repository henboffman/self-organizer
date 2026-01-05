using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ITaskService
{
    // Basic CRUD
    Task<TodoTask?> GetByIdAsync(Guid id);
    Task<IEnumerable<TodoTask>> GetAllAsync();
    Task<IEnumerable<TodoTask>> GetByStatusAsync(TodoTaskStatus status);
    Task<IEnumerable<TodoTask>> GetByProjectAsync(Guid projectId);
    Task<TodoTask> CreateAsync(TodoTask task);
    Task<TodoTask> UpdateAsync(TodoTask task);
    Task DeleteAsync(Guid id);

    // Status-based queries
    Task<IEnumerable<TodoTask>> GetNextActionsAsync();
    Task<IEnumerable<TodoTask>> GetWaitingForAsync();
    Task<IEnumerable<TodoTask>> GetScheduledAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<TodoTask>> GetSomedayMaybeAsync();
    Task<IEnumerable<TodoTask>> GetOverdueAsync();
    Task<IEnumerable<TodoTask>> SearchAsync(string query);
    Task<int> GetCompletedTodayCountAsync();

    // Status transitions
    Task<TodoTask> CompleteAsync(Guid id);
    Task<TodoTask> DeactivateAsync(Guid id);
    Task<TodoTask> MoveToSomedayAsync(Guid id);
    Task<TodoTask> ScheduleAsync(Guid id, DateTime scheduledDate, DateTime? startTime = null);

    // Subtask operations
    /// <summary>
    /// Gets all subtasks of a parent task
    /// </summary>
    Task<IEnumerable<TodoTask>> GetSubtasksAsync(Guid parentTaskId);

    /// <summary>
    /// Creates a subtask under a parent task
    /// </summary>
    Task<TodoTask> CreateSubtaskAsync(Guid parentTaskId, TodoTask subtask);

    /// <summary>
    /// Moves a task to become a subtask of another task
    /// </summary>
    Task<TodoTask> MakeSubtaskOfAsync(Guid taskId, Guid parentTaskId);

    /// <summary>
    /// Promotes a subtask to a standalone task
    /// </summary>
    Task<TodoTask> PromoteToStandaloneAsync(Guid subtaskId);

    // Blocking/dependency operations
    /// <summary>
    /// Gets tasks that are blocked (have incomplete blocking tasks)
    /// </summary>
    Task<IEnumerable<TodoTask>> GetBlockedTasksAsync();

    /// <summary>
    /// Gets tasks that are available to work on (not blocked, not waiting, active status)
    /// </summary>
    Task<IEnumerable<TodoTask>> GetAvailableTasksAsync();

    /// <summary>
    /// Checks if a task is currently blocked by incomplete dependencies
    /// </summary>
    Task<bool> IsTaskBlockedAsync(Guid taskId);

    /// <summary>
    /// Adds a blocking dependency (taskId is blocked by blockerTaskId)
    /// </summary>
    Task AddBlockingDependencyAsync(Guid taskId, Guid blockerTaskId);

    /// <summary>
    /// Removes a blocking dependency
    /// </summary>
    Task RemoveBlockingDependencyAsync(Guid taskId, Guid blockerTaskId);

    // Recurring task operations
    /// <summary>
    /// Gets all recurring tasks
    /// </summary>
    Task<IEnumerable<TodoTask>> GetRecurringTasksAsync();

    /// <summary>
    /// Completes a recurring task and creates the next occurrence
    /// </summary>
    Task<TodoTask> CompleteRecurringTaskAsync(Guid id);

    /// <summary>
    /// Calculates the next occurrence date based on recurrence pattern
    /// </summary>
    DateTime CalculateNextOccurrence(TodoTask task);
}
