using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _repository;
    private readonly ITaskService _taskService;

    public ProjectService(IRepository<Project> repository, ITaskService taskService)
    {
        _repository = repository;
        _taskService = taskService;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status)
    {
        return await _repository.QueryAsync(p => p.Status == status);
    }

    public async Task<IEnumerable<Project>> GetActiveAsync()
    {
        return await _repository.QueryAsync(p => p.Status == ProjectStatus.Active);
    }

    public async Task<IEnumerable<Project>> GetStalledProjectsAsync()
    {
        // Load both datasets in parallel - eliminates N+1 query problem
        var activeProjectsTask = GetActiveAsync();
        var allTasksTask = _taskService.GetAllAsync();

        await Task.WhenAll(activeProjectsTask, allTasksTask);

        var activeProjects = await activeProjectsTask;
        var allTasks = (await allTasksTask).ToList();

        // Build a set of project IDs that have at least one NextAction task
        var projectsWithNextActions = allTasks
            .Where(t => t.ProjectId.HasValue && t.Status == TodoTaskStatus.NextAction)
            .Select(t => t.ProjectId!.Value)
            .ToHashSet();

        // Return active projects that don't have any next actions
        return activeProjects.Where(p => !projectsWithNextActions.Contains(p.Id));
    }

    public async Task<Project> CreateAsync(Project project)
    {
        return await _repository.AddAsync(project);
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        return await _repository.UpdateAsync(project);
    }

    public async Task<Project> CompleteAsync(Guid id)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project == null)
            throw new InvalidOperationException($"Project {id} not found");

        project.Status = ProjectStatus.Completed;
        project.CompletedAt = DateTime.UtcNow;
        return await _repository.UpdateAsync(project);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project != null)
        {
            project.Status = ProjectStatus.Deleted;
            await _repository.UpdateAsync(project);
        }
    }

    public async Task<bool> HasNextActionAsync(Guid projectId)
    {
        var tasks = await _taskService.GetByProjectAsync(projectId);
        return tasks.Any(t => t.Status == TodoTaskStatus.NextAction);
    }

    public async Task<(int completed, int total)> GetProgressAsync(Guid projectId)
    {
        var tasks = await _taskService.GetByProjectAsync(projectId);
        var taskList = tasks.Where(t => t.Status != TodoTaskStatus.Deleted).ToList();
        var completed = taskList.Count(t => t.Status == TodoTaskStatus.Completed);
        return (completed, taskList.Count);
    }

    public async Task<Dictionary<Guid, (int completed, int total)>> GetProgressBatchAsync(IEnumerable<Guid> projectIds)
    {
        // Load all tasks once instead of N separate queries
        var allTasks = await _taskService.GetAllAsync();
        var projectIdSet = projectIds.ToHashSet();

        // Group tasks by project and calculate progress
        var result = new Dictionary<Guid, (int completed, int total)>();

        var tasksByProject = allTasks
            .Where(t => t.ProjectId.HasValue &&
                        projectIdSet.Contains(t.ProjectId.Value) &&
                        t.Status != TodoTaskStatus.Deleted)
            .GroupBy(t => t.ProjectId!.Value);

        foreach (var group in tasksByProject)
        {
            var taskList = group.ToList();
            var completed = taskList.Count(t => t.Status == TodoTaskStatus.Completed);
            result[group.Key] = (completed, taskList.Count);
        }

        // Ensure all requested projects have an entry (even if they have no tasks)
        foreach (var projectId in projectIdSet)
        {
            if (!result.ContainsKey(projectId))
            {
                result[projectId] = (0, 0);
            }
        }

        return result;
    }
}
