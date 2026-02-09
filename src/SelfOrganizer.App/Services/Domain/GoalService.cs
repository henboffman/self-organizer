using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class GoalService : IGoalService
{
    private readonly IRepository<Goal> _repository;
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IRepository<Skill> _skillRepository;
    private readonly IUserPreferencesProvider _preferencesProvider;

    public GoalService(
        IRepository<Goal> repository,
        ITaskService taskService,
        IProjectService projectService,
        IRepository<Skill> skillRepository,
        IUserPreferencesProvider preferencesProvider)
    {
        _repository = repository;
        _taskService = taskService;
        _projectService = projectService;
        _skillRepository = skillRepository;
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
        ArgumentNullException.ThrowIfNull(goal);
        return await _repository.AddAsync(goal);
    }

    public async Task<Goal> UpdateAsync(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
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
            // Clear next action if the unlinked task was the designated next action
            if (goal.NextActionTaskId == taskId)
            {
                goal.NextActionTaskId = null;
            }
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

    public async Task SetNextActionAsync(Guid goalId, Guid? taskId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        // Validate task if provided
        if (taskId.HasValue)
        {
            var task = await _taskService.GetByIdAsync(taskId.Value);
            if (task == null)
                throw new InvalidOperationException($"Task {taskId} not found");

            // Task must be linked to this goal
            if (!goal.LinkedTaskIds.Contains(taskId.Value))
                throw new InvalidOperationException("Task is not linked to this goal");

            // Task must be incomplete
            if (task.Status == TodoTaskStatus.Completed || task.Status == TodoTaskStatus.Deleted)
                throw new InvalidOperationException("Cannot set completed or deleted task as next action");
        }

        // Demote existing NextAction status tasks linked to this goal to Active
        var linkedTasks = await GetLinkedTasksAsync(goalId);
        foreach (var t in linkedTasks.Where(t => t.Status == TodoTaskStatus.NextAction && t.Id != taskId))
        {
            t.Status = TodoTaskStatus.Active;
            t.ModifiedAt = DateTime.UtcNow;
            await _taskService.UpdateAsync(t);
        }

        // If setting a task as next action, ensure it has NextAction status
        if (taskId.HasValue)
        {
            var task = await _taskService.GetByIdAsync(taskId.Value);
            if (task != null && task.Status != TodoTaskStatus.NextAction)
            {
                task.Status = TodoTaskStatus.NextAction;
                task.ModifiedAt = DateTime.UtcNow;
                await _taskService.UpdateAsync(task);
            }
        }

        // Update goal with new next action
        goal.NextActionTaskId = taskId;
        goal.ModifiedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(goal);
    }

    public async Task ClearNextActionIfMatchesAsync(Guid goalId, Guid taskId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal?.NextActionTaskId == taskId)
        {
            goal.NextActionTaskId = null;
            goal.ModifiedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(goal);
        }
    }

    public async Task LinkSkillAsync(Guid goalId, Guid skillId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        var skill = await _skillRepository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        // Update goal side
        if (!goal.LinkedSkillIds.Contains(skillId))
        {
            goal.LinkedSkillIds.Add(skillId);
            await _repository.UpdateAsync(goal);
        }

        // Update skill side (bidirectional)
        if (!skill.LinkedGoalIds.Contains(goalId))
        {
            skill.LinkedGoalIds.Add(goalId);
            await _skillRepository.UpdateAsync(skill);
        }
    }

    public async Task UnlinkSkillAsync(Guid goalId, Guid skillId)
    {
        var goal = await _repository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        // Update goal side
        if (goal.LinkedSkillIds.Remove(skillId))
        {
            await _repository.UpdateAsync(goal);
        }

        // Update skill side (bidirectional)
        var skill = await _skillRepository.GetByIdAsync(skillId);
        if (skill != null && skill.LinkedGoalIds.Remove(goalId))
        {
            await _skillRepository.UpdateAsync(skill);
        }
    }
}
