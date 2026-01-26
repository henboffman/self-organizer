using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class IdeaService : IIdeaService
{
    private readonly IRepository<Idea> _repository;
    private readonly ITaskService _taskService;
    private readonly IDataChangeNotificationService _notificationService;

    public IdeaService(
        IRepository<Idea> repository,
        ITaskService taskService,
        IDataChangeNotificationService notificationService)
    {
        _repository = repository;
        _taskService = taskService;
        _notificationService = notificationService;
    }

    public async Task<Idea?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Idea>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Idea>> GetActiveAsync()
    {
        return await _repository.QueryAsync(i => i.Status == IdeaStatus.Active);
    }

    public async Task<IEnumerable<Idea>> GetByStatusAsync(IdeaStatus status)
    {
        return await _repository.QueryAsync(i => i.Status == status);
    }

    public async Task<IEnumerable<Idea>> GetByGoalAsync(Guid goalId)
    {
        return await _repository.QueryAsync(i => i.LinkedGoalId == goalId);
    }

    public async Task<IEnumerable<Idea>> GetByProjectAsync(Guid projectId)
    {
        return await _repository.QueryAsync(i => i.LinkedProjectId == projectId);
    }

    public async Task<Idea> CreateAsync(Idea idea)
    {
        ArgumentNullException.ThrowIfNull(idea);
        var result = await _repository.AddAsync(idea);
        _notificationService.NotifyDataChanged();
        return result;
    }

    public async Task<Idea> UpdateAsync(Idea idea)
    {
        ArgumentNullException.ThrowIfNull(idea);
        var result = await _repository.UpdateAsync(idea);
        _notificationService.NotifyDataChanged();
        return result;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        _notificationService.NotifyDataChanged();
    }

    public async Task<Idea> ArchiveAsync(Guid id)
    {
        var idea = await _repository.GetByIdAsync(id);
        if (idea == null)
            throw new InvalidOperationException($"Idea {id} not found");

        idea.Status = IdeaStatus.Archived;
        var result = await _repository.UpdateAsync(idea);
        _notificationService.NotifyDataChanged();
        return result;
    }

    public async Task<Idea> DismissAsync(Guid id)
    {
        var idea = await _repository.GetByIdAsync(id);
        if (idea == null)
            throw new InvalidOperationException($"Idea {id} not found");

        idea.Status = IdeaStatus.Dismissed;
        var result = await _repository.UpdateAsync(idea);
        _notificationService.NotifyDataChanged();
        return result;
    }

    public async Task<TodoTask> ConvertToTaskAsync(Guid ideaId, TodoTask taskTemplate)
    {
        ArgumentNullException.ThrowIfNull(taskTemplate);
        var idea = await _repository.GetByIdAsync(ideaId);
        if (idea == null)
            throw new InvalidOperationException($"Idea {ideaId} not found");

        // Create the task
        taskTemplate.Title = string.IsNullOrWhiteSpace(taskTemplate.Title) ? idea.Title : taskTemplate.Title;
        taskTemplate.Description = string.IsNullOrWhiteSpace(taskTemplate.Description) ? idea.Description : taskTemplate.Description;
        taskTemplate.Tags = idea.Tags.Any() ? new List<string>(idea.Tags) : taskTemplate.Tags;

        // Copy goal linkage if present
        if (idea.LinkedGoalId.HasValue && !taskTemplate.GoalIds.Contains(idea.LinkedGoalId.Value))
        {
            taskTemplate.GoalIds.Add(idea.LinkedGoalId.Value);
        }

        // Copy project linkage if present
        if (idea.LinkedProjectId.HasValue)
        {
            taskTemplate.ProjectId = idea.LinkedProjectId.Value;
        }

        var createdTask = await _taskService.CreateAsync(taskTemplate);

        // Mark idea as converted
        idea.Status = IdeaStatus.ConvertedToTask;
        idea.ConvertedToTaskId = createdTask.Id;
        await _repository.UpdateAsync(idea);

        _notificationService.NotifyDataChanged();
        return createdTask;
    }

    public async Task<int> GetActiveCountAsync()
    {
        var active = await GetActiveAsync();
        return active.Count();
    }

    public async Task<IEnumerable<Idea>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        var lowerQuery = query.ToLowerInvariant();
        return await _repository.QueryAsync(i =>
            i.Title.ToLower().Contains(lowerQuery) ||
            (i.Description != null && i.Description.ToLower().Contains(lowerQuery)) ||
            (i.Notes != null && i.Notes.ToLower().Contains(lowerQuery)) ||
            i.Tags.Any(t => t.ToLower().Contains(lowerQuery)));
    }
}
