using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Commands;

/// <summary>
/// Command to create a new task.
/// </summary>
public class CreateTaskCommand : ICommand
{
    private readonly IRepository<TodoTask> _repository;
    private readonly TodoTask _task;

    public string Description => $"Create task: {_task.Title}";
    public DateTime ExecutedAt { get; private set; }

    public CreateTaskCommand(IRepository<TodoTask> repository, TodoTask task)
    {
        _repository = repository;
        _task = task;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;
        await _repository.AddAsync(_task);
    }

    public async Task UndoAsync()
    {
        await _repository.DeleteAsync(_task.Id);
    }
}

/// <summary>
/// Command to update an existing task.
/// Stores the previous state for full restoration on undo.
/// </summary>
public class UpdateTaskCommand : ICommand
{
    private readonly IRepository<TodoTask> _repository;
    private readonly TodoTask _newState;
    private TodoTask? _previousState;

    public string Description => $"Update task: {_newState.Title}";
    public DateTime ExecutedAt { get; private set; }

    public UpdateTaskCommand(IRepository<TodoTask> repository, TodoTask newState)
    {
        _repository = repository;
        _newState = newState;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the previous state for undo
        var existingTask = await _repository.GetByIdAsync(_newState.Id);
        if (existingTask != null)
        {
            _previousState = CloneTask(existingTask);
        }

        await _repository.UpdateAsync(_newState);
    }

    public async Task UndoAsync()
    {
        if (_previousState == null)
            throw new InvalidOperationException("Cannot undo: no previous state captured.");

        await _repository.UpdateAsync(_previousState);
    }

    private static TodoTask CloneTask(TodoTask task)
    {
        return new TodoTask
        {
            Id = task.Id,
            CreatedAt = task.CreatedAt,
            ModifiedAt = task.ModifiedAt,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            Status = task.Status,
            Contexts = new List<string>(task.Contexts),
            Category = task.Category,
            EnergyLevel = task.EnergyLevel,
            EstimatedMinutes = task.EstimatedMinutes,
            ActualMinutes = task.ActualMinutes,
            DueDate = task.DueDate,
            ScheduledDate = task.ScheduledDate,
            ScheduledStartTime = task.ScheduledStartTime,
            CompletedAt = task.CompletedAt,
            WaitingForContactId = task.WaitingForContactId,
            WaitingForNote = task.WaitingForNote,
            WaitingForSince = task.WaitingForSince,
            Priority = task.Priority,
            LinkedTaskIds = new List<Guid>(task.LinkedTaskIds),
            LinkedMeetingIds = new List<Guid>(task.LinkedMeetingIds),
            RequiresDeepWork = task.RequiresDeepWork,
            Notes = task.Notes,
            Tags = new List<string>(task.Tags),
            ParentTaskId = task.ParentTaskId,
            SubtaskIds = new List<Guid>(task.SubtaskIds),
            BlockedByTaskIds = new List<Guid>(task.BlockedByTaskIds),
            IsRecurring = task.IsRecurring,
            RecurrencePattern = task.RecurrencePattern,
            RecurrenceIntervalDays = task.RecurrenceIntervalDays,
            LastRecurrenceDate = task.LastRecurrenceDate,
            Links = new List<string>(task.Links),
            WhoFor = task.WhoFor
        };
    }
}

/// <summary>
/// Command to delete a task.
/// Stores the deleted task for restoration on undo.
/// </summary>
public class DeleteTaskCommand : ICommand
{
    private readonly IRepository<TodoTask> _repository;
    private readonly Guid _taskId;
    private TodoTask? _deletedTask;

    public string Description => _deletedTask != null
        ? $"Delete task: {_deletedTask.Title}"
        : "Delete task";
    public DateTime ExecutedAt { get; private set; }

    public DeleteTaskCommand(IRepository<TodoTask> repository, Guid taskId)
    {
        _repository = repository;
        _taskId = taskId;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the task before deletion for undo
        _deletedTask = await _repository.GetByIdAsync(_taskId);
        await _repository.DeleteAsync(_taskId);
    }

    public async Task UndoAsync()
    {
        if (_deletedTask == null)
            throw new InvalidOperationException("Cannot undo: no deleted task captured.");

        await _repository.AddAsync(_deletedTask);
    }
}

/// <summary>
/// Command to complete a task.
/// Stores the previous status and completion time for undo.
/// </summary>
public class CompleteTaskCommand : ICommand
{
    private readonly IRepository<TodoTask> _repository;
    private readonly Guid _taskId;
    private TodoTaskStatus _previousStatus;
    private DateTime? _previousCompletedAt;
    private int? _previousActualMinutes;
    private TodoTask? _task;

    public string Description => _task != null
        ? $"Complete task: {_task.Title}"
        : "Complete task";
    public DateTime ExecutedAt { get; private set; }

    public CompleteTaskCommand(IRepository<TodoTask> repository, Guid taskId)
    {
        _repository = repository;
        _taskId = taskId;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        _task = await _repository.GetByIdAsync(_taskId);
        if (_task == null)
            throw new InvalidOperationException($"Task with ID {_taskId} not found.");

        // Store previous state
        _previousStatus = _task.Status;
        _previousCompletedAt = _task.CompletedAt;
        _previousActualMinutes = _task.ActualMinutes;

        // Mark as completed
        _task.Status = TodoTaskStatus.Completed;
        _task.CompletedAt = DateTime.UtcNow;
        _task.ModifiedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(_task);
    }

    public async Task UndoAsync()
    {
        if (_task == null)
            throw new InvalidOperationException("Cannot undo: no task captured.");

        // Restore previous state
        _task.Status = _previousStatus;
        _task.CompletedAt = _previousCompletedAt;
        _task.ActualMinutes = _previousActualMinutes;
        _task.ModifiedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(_task);
    }
}
