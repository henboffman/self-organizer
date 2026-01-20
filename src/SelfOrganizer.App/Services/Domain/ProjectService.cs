using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _repository;
    private readonly IRepository<Goal> _goalRepository;
    private readonly ITaskService _taskService;
    private readonly IUserPreferencesProvider _preferencesProvider;

    public ProjectService(
        IRepository<Project> repository,
        IRepository<Goal> goalRepository,
        ITaskService taskService,
        IUserPreferencesProvider preferencesProvider)
    {
        _repository = repository;
        _goalRepository = goalRepository;
        _taskService = taskService;
        _preferencesProvider = preferencesProvider;
    }

    /// <summary>
    /// Filters out sample data when ShowSampleData preference is false
    /// </summary>
    private async Task<IEnumerable<Project>> FilterSampleDataAsync(IEnumerable<Project> projects)
    {
        if (await _preferencesProvider.ShowSampleDataAsync())
            return projects;
        return projects.Where(p => !p.IsSampleData);
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        var projects = await _repository.GetAllAsync();
        return await FilterSampleDataAsync(projects);
    }

    public async Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status)
    {
        var projects = await _repository.QueryAsync(p => p.Status == status);
        return await FilterSampleDataAsync(projects);
    }

    public async Task<IEnumerable<Project>> GetActiveAsync()
    {
        var projects = await _repository.QueryAsync(p => p.Status == ProjectStatus.Active);
        return await FilterSampleDataAsync(projects);
    }

    public async Task<IEnumerable<Project>> GetStalledProjectsAsync()
    {
        // Load all datasets in parallel - eliminates N+1 query problem
        // Note: GetActiveAsync and _taskService.GetAllAsync already filter sample data
        var activeProjectsTask = GetActiveAsync();
        var allTasksTask = _taskService.GetAllAsync();
        var allGoalsTask = _goalRepository.GetAllAsync();

        await Task.WhenAll(activeProjectsTask, allTasksTask, allGoalsTask);

        var activeProjects = (await activeProjectsTask).ToList();
        var allTasks = (await allTasksTask).ToList();
        var allGoals = (await allGoalsTask).ToList();

        // Filter sample goals if ShowSampleData is false
        if (!await _preferencesProvider.ShowSampleDataAsync())
        {
            allGoals = allGoals.Where(g => !g.IsSampleData).ToList();
        }

        // Build a set of project IDs that have at least one NextAction task (direct tasks)
        var projectsWithNextActions = allTasks
            .Where(t => t.ProjectId.HasValue && t.Status == TodoTaskStatus.NextAction)
            .Select(t => t.ProjectId!.Value)
            .ToHashSet();

        // Also check goals linked to projects - if a goal has tasks with NextAction, the project is not stalled
        foreach (var project in activeProjects)
        {
            if (projectsWithNextActions.Contains(project.Id))
                continue;

            // Find goals that link to this project
            var linkedGoals = allGoals.Where(g => g.LinkedProjectIds.Contains(project.Id));

            foreach (var goal in linkedGoals)
            {
                // Check if any of the goal's linked tasks are NextAction
                var hasNextAction = allTasks
                    .Any(t => goal.LinkedTaskIds.Contains(t.Id) && t.Status == TodoTaskStatus.NextAction);

                if (hasNextAction)
                {
                    projectsWithNextActions.Add(project.Id);
                    break;
                }
            }
        }

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
        // Check direct project tasks
        var tasks = await _taskService.GetByProjectAsync(projectId);
        if (tasks.Any(t => t.Status == TodoTaskStatus.NextAction))
            return true;

        // Check tasks from linked goals
        var allGoals = await _goalRepository.GetAllAsync();
        var linkedGoals = allGoals.Where(g => g.LinkedProjectIds.Contains(projectId));

        foreach (var goal in linkedGoals)
        {
            if (!goal.LinkedTaskIds.Any())
                continue;

            var goalTasks = await _taskService.GetAllAsync();
            var hasNextAction = goalTasks
                .Any(t => goal.LinkedTaskIds.Contains(t.Id) && t.Status == TodoTaskStatus.NextAction);

            if (hasNextAction)
                return true;
        }

        return false;
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
