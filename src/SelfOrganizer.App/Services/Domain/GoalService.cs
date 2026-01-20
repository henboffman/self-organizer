using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class GoalService : IGoalService
{
    private readonly IRepository<Goal> _repository;
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IUserPreferencesProvider _preferencesProvider;

    public GoalService(
        IRepository<Goal> repository,
        ITaskService taskService,
        IProjectService projectService,
        IUserPreferencesProvider preferencesProvider)
    {
        _repository = repository;
        _taskService = taskService;
        _projectService = projectService;
        _preferencesProvider = preferencesProvider;
    }

    /// <summary>
    /// Filters out sample data when ShowSampleData preference is false
    /// </summary>
    private async Task<IEnumerable<Goal>> FilterSampleDataAsync(IEnumerable<Goal> goals)
    {
        if (await _preferencesProvider.ShowSampleDataAsync())
            return goals;
        return goals.Where(g => !g.IsSampleData);
    }

    public async Task<Goal?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Goal>> GetAllAsync()
    {
        var goals = await _repository.GetAllAsync();
        return await FilterSampleDataAsync(goals);
    }

    public async Task<IEnumerable<Goal>> GetByStatusAsync(GoalStatus status)
    {
        var goals = await _repository.QueryAsync(g => g.Status == status);
        return await FilterSampleDataAsync(goals);
    }

    public async Task<IEnumerable<Goal>> GetActiveGoalsAsync()
    {
        var goals = await _repository.QueryAsync(g => g.Status == GoalStatus.Active);
        return await FilterSampleDataAsync(goals);
    }

    public async Task<Goal> CreateAsync(Goal goal)
    {
        return await _repository.AddAsync(goal);
    }

    public async Task<Goal> UpdateAsync(Goal goal)
    {
        return await _repository.UpdateAsync(goal);
    }

    public async Task DeleteAsync(Guid id)
    {
        var goal = await _repository.GetByIdAsync(id);
        if (goal != null)
        {
            goal.Status = GoalStatus.Archived;
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task<Goal> CompleteAsync(Guid id)
    {
        var goal = await _repository.GetByIdAsync(id);
        if (goal == null)
            throw new InvalidOperationException($"Goal {id} not found");

        goal.Status = GoalStatus.Completed;
        goal.CompletedAt = DateTime.UtcNow;
        goal.ProgressPercent = 100;
        return await _repository.UpdateAsync(goal);
    }

    public async Task<double> CalculateProgressAsync(Guid goalId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            return 0;

        var linkedTasks = await GetLinkedTasksAsync(goalId);
        var taskList = linkedTasks.ToList();

        if (taskList.Count == 0)
            return goal.ProgressPercent;

        var completedTasks = taskList.Count(t => t.Status == TodoTaskStatus.Completed);
        var progress = (double)completedTasks / taskList.Count * 100;

        // Update the goal's progress percent
        goal.ProgressPercent = (int)Math.Round(progress);
        await _repository.UpdateAsync(goal);

        return progress;
    }

    public async Task LinkProjectAsync(Guid goalId, Guid projectId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        if (!goal.LinkedProjectIds.Contains(projectId))
        {
            goal.LinkedProjectIds.Add(projectId);
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task UnlinkProjectAsync(Guid goalId, Guid projectId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        if (goal.LinkedProjectIds.Remove(projectId))
        {
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task LinkTaskAsync(Guid goalId, Guid taskId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        if (!goal.LinkedTaskIds.Contains(taskId))
        {
            goal.LinkedTaskIds.Add(taskId);
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task UnlinkTaskAsync(Guid goalId, Guid taskId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        if (goal.LinkedTaskIds.Remove(taskId))
        {
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task<IEnumerable<TodoTask>> GetLinkedTasksAsync(Guid goalId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null || goal.LinkedTaskIds.Count == 0)
            return Enumerable.Empty<TodoTask>();

        var tasks = new List<TodoTask>();
        foreach (var taskId in goal.LinkedTaskIds)
        {
            var task = await _taskService.GetByIdAsync(taskId);
            if (task != null)
            {
                tasks.Add(task);
            }
        }

        return tasks;
    }

    public async Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid goalId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null || goal.LinkedProjectIds.Count == 0)
            return Enumerable.Empty<Project>();

        var projects = new List<Project>();
        foreach (var projectId in goal.LinkedProjectIds)
        {
            var project = await _projectService.GetByIdAsync(projectId);
            if (project != null)
            {
                projects.Add(project);
            }
        }

        return projects;
    }
}
