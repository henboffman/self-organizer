using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class SkillService : ISkillService
{
    private readonly IRepository<Skill> _repository;
    private readonly IRepository<Goal> _goalRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly ITaskService _taskService;
    private readonly IRepository<Habit> _habitRepository;
    private readonly IUserPreferencesProvider _preferencesProvider;

    public SkillService(
        IRepository<Skill> repository,
        IRepository<Goal> goalRepository,
        IRepository<Project> projectRepository,
        ITaskService taskService,
        IRepository<Habit> habitRepository,
        IUserPreferencesProvider preferencesProvider)
    {
        _repository = repository;
        _goalRepository = goalRepository;
        _projectRepository = projectRepository;
        _taskService = taskService;
        _habitRepository = habitRepository;
        _preferencesProvider = preferencesProvider;
    }

    private async Task<IEnumerable<Skill>> FilterSampleDataAsync(IEnumerable<Skill> skills)
    {
        if (await _preferencesProvider.ShowSampleDataAsync())
            return skills;
        return skills.Where(s => !s.IsSampleData);
    }

    public async Task<Skill?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Skill>> GetAllAsync()
    {
        var skills = await _repository.GetAllAsync();
        return await FilterSampleDataAsync(skills);
    }

    public async Task<IEnumerable<Skill>> GetByTypeAsync(SkillType type)
    {
        var skills = await _repository.QueryAsync(s => s.Type == type);
        return await FilterSampleDataAsync(skills);
    }

    public async Task<IEnumerable<Skill>> GetByCategoryAsync(SkillCategory category)
    {
        var skills = await _repository.QueryAsync(s => s.Category == category);
        return await FilterSampleDataAsync(skills);
    }

    public async Task<IEnumerable<Skill>> GetActiveSkillsAsync()
    {
        var skills = await _repository.QueryAsync(s => s.IsActive);
        return await FilterSampleDataAsync(skills);
    }

    public async Task<Skill> CreateAsync(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return await _repository.AddAsync(skill);
    }

    public async Task<Skill> UpdateAsync(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return await _repository.UpdateAsync(skill);
    }

    public async Task DeleteAsync(Guid id)
    {
        var skill = await _repository.GetByIdAsync(id);
        if (skill == null) return;

        // Unlink from all related entities before deleting
        foreach (var goalId in skill.LinkedGoalIds.ToList())
        {
            await UnlinkGoalAsync(id, goalId);
        }
        foreach (var projectId in skill.LinkedProjectIds.ToList())
        {
            await UnlinkProjectAsync(id, projectId);
        }
        foreach (var taskId in skill.LinkedTaskIds.ToList())
        {
            await UnlinkTaskAsync(id, taskId);
        }
        foreach (var habitId in skill.LinkedHabitIds.ToList())
        {
            await UnlinkHabitAsync(id, habitId);
        }

        // Soft delete by setting IsActive to false
        skill.IsActive = false;
        await _repository.UpdateAsync(skill);
    }

    public async Task<Skill> UpdateProficiencyAsync(Guid skillId, int currentLevel, int? targetLevel = null)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        skill.CurrentProficiency = Math.Clamp(currentLevel, 1, 5);
        if (targetLevel.HasValue)
        {
            skill.TargetProficiency = Math.Clamp(targetLevel.Value, 1, 5);
        }

        return await _repository.UpdateAsync(skill);
    }

    // Goal linking (bidirectional)
    public async Task LinkGoalAsync(Guid skillId, Guid goalId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal == null)
            throw new InvalidOperationException($"Goal {goalId} not found");

        // Update skill side
        if (!skill.LinkedGoalIds.Contains(goalId))
        {
            skill.LinkedGoalIds.Add(goalId);
            await _repository.UpdateAsync(skill);
        }

        // Update goal side (bidirectional)
        if (!goal.LinkedSkillIds.Contains(skillId))
        {
            goal.LinkedSkillIds.Add(skillId);
            await _goalRepository.UpdateAsync(goal);
        }
    }

    public async Task UnlinkGoalAsync(Guid skillId, Guid goalId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        // Update skill side
        if (skill.LinkedGoalIds.Remove(goalId))
        {
            await _repository.UpdateAsync(skill);
        }

        // Update goal side (bidirectional)
        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal != null && goal.LinkedSkillIds.Remove(skillId))
        {
            await _goalRepository.UpdateAsync(goal);
        }
    }

    public async Task<IEnumerable<Goal>> GetLinkedGoalsAsync(Guid skillId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null || skill.LinkedGoalIds.Count == 0)
            return Enumerable.Empty<Goal>();

        var goals = new List<Goal>();
        foreach (var goalId in skill.LinkedGoalIds)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId);
            if (goal != null)
            {
                goals.Add(goal);
            }
        }
        return goals;
    }

    // Project linking (bidirectional)
    public async Task LinkProjectAsync(Guid skillId, Guid projectId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
            throw new InvalidOperationException($"Project {projectId} not found");

        // Update skill side
        if (!skill.LinkedProjectIds.Contains(projectId))
        {
            skill.LinkedProjectIds.Add(projectId);
            await _repository.UpdateAsync(skill);
        }

        // Update project side (bidirectional)
        if (!project.LinkedSkillIds.Contains(skillId))
        {
            project.LinkedSkillIds.Add(skillId);
            await _projectRepository.UpdateAsync(project);
        }
    }

    public async Task UnlinkProjectAsync(Guid skillId, Guid projectId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        // Update skill side
        if (skill.LinkedProjectIds.Remove(projectId))
        {
            await _repository.UpdateAsync(skill);
        }

        // Update project side (bidirectional)
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project != null && project.LinkedSkillIds.Remove(skillId))
        {
            await _projectRepository.UpdateAsync(project);
        }
    }

    public async Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid skillId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null || skill.LinkedProjectIds.Count == 0)
            return Enumerable.Empty<Project>();

        var projects = new List<Project>();
        foreach (var projectId in skill.LinkedProjectIds)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project != null)
            {
                projects.Add(project);
            }
        }
        return projects;
    }

    // Task linking (bidirectional)
    public async Task LinkTaskAsync(Guid skillId, Guid taskId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        var task = await _taskService.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        // Update skill side
        if (!skill.LinkedTaskIds.Contains(taskId))
        {
            skill.LinkedTaskIds.Add(taskId);
            await _repository.UpdateAsync(skill);
        }

        // Update task side (bidirectional)
        if (!task.SkillIds.Contains(skillId))
        {
            task.SkillIds.Add(skillId);
            await _taskService.UpdateAsync(task);
        }
    }

    public async Task UnlinkTaskAsync(Guid skillId, Guid taskId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        // Update skill side
        if (skill.LinkedTaskIds.Remove(taskId))
        {
            await _repository.UpdateAsync(skill);
        }

        // Update task side (bidirectional)
        var task = await _taskService.GetByIdAsync(taskId);
        if (task != null && task.SkillIds.Remove(skillId))
        {
            await _taskService.UpdateAsync(task);
        }
    }

    public async Task<IEnumerable<TodoTask>> GetLinkedTasksAsync(Guid skillId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null || skill.LinkedTaskIds.Count == 0)
            return Enumerable.Empty<TodoTask>();

        var tasks = new List<TodoTask>();
        foreach (var taskId in skill.LinkedTaskIds)
        {
            var task = await _taskService.GetByIdAsync(taskId);
            if (task != null)
            {
                tasks.Add(task);
            }
        }
        return tasks;
    }

    public async Task<int> GetCompletedTaskCountAsync(Guid skillId)
    {
        var tasks = await GetLinkedTasksAsync(skillId);
        return tasks.Count(t => t.Status == TodoTaskStatus.Completed);
    }

    // Habit linking (bidirectional)
    public async Task LinkHabitAsync(Guid skillId, Guid habitId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        var habit = await _habitRepository.GetByIdAsync(habitId);
        if (habit == null)
            throw new InvalidOperationException($"Habit {habitId} not found");

        // Update skill side
        if (!skill.LinkedHabitIds.Contains(habitId))
        {
            skill.LinkedHabitIds.Add(habitId);
            await _repository.UpdateAsync(skill);
        }

        // Update habit side (bidirectional)
        if (!habit.LinkedSkillIds.Contains(skillId))
        {
            habit.LinkedSkillIds.Add(skillId);
            await _habitRepository.UpdateAsync(habit);
        }
    }

    public async Task UnlinkHabitAsync(Guid skillId, Guid habitId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null)
            throw new InvalidOperationException($"Skill {skillId} not found");

        // Update skill side
        if (skill.LinkedHabitIds.Remove(habitId))
        {
            await _repository.UpdateAsync(skill);
        }

        // Update habit side (bidirectional)
        var habit = await _habitRepository.GetByIdAsync(habitId);
        if (habit != null && habit.LinkedSkillIds.Remove(skillId))
        {
            await _habitRepository.UpdateAsync(habit);
        }
    }

    public async Task<IEnumerable<Habit>> GetLinkedHabitsAsync(Guid skillId)
    {
        var skill = await _repository.GetByIdAsync(skillId);
        if (skill == null || skill.LinkedHabitIds.Count == 0)
            return Enumerable.Empty<Habit>();

        var habits = new List<Habit>();
        foreach (var habitId in skill.LinkedHabitIds)
        {
            var habit = await _habitRepository.GetByIdAsync(habitId);
            if (habit != null)
            {
                habits.Add(habit);
            }
        }
        return habits;
    }

    // Get skills linked to specific entities (reverse lookups)
    public async Task<IEnumerable<Skill>> GetSkillsForGoalAsync(Guid goalId)
    {
        var skills = await GetAllAsync();
        return skills.Where(s => s.LinkedGoalIds.Contains(goalId));
    }

    public async Task<IEnumerable<Skill>> GetSkillsForProjectAsync(Guid projectId)
    {
        var skills = await GetAllAsync();
        return skills.Where(s => s.LinkedProjectIds.Contains(projectId));
    }

    public async Task<IEnumerable<Skill>> GetSkillsForTaskAsync(Guid taskId)
    {
        var skills = await GetAllAsync();
        return skills.Where(s => s.LinkedTaskIds.Contains(taskId));
    }

    public async Task<IEnumerable<Skill>> GetSkillsForHabitAsync(Guid habitId)
    {
        var skills = await GetAllAsync();
        return skills.Where(s => s.LinkedHabitIds.Contains(habitId));
    }
}
