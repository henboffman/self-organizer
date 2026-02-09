using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class CareerPlanService : ICareerPlanService
{
    private readonly IRepository<CareerPlan> _repository;
    private readonly IRepository<Goal> _goalRepository;
    private readonly IRepository<Skill> _skillRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Habit> _habitRepository;
    private readonly IUserPreferencesProvider _preferencesProvider;
    private readonly IGrowthContextService _growthContextService;

    public CareerPlanService(
        IRepository<CareerPlan> repository,
        IRepository<Goal> goalRepository,
        IRepository<Skill> skillRepository,
        IRepository<Project> projectRepository,
        IRepository<Habit> habitRepository,
        IUserPreferencesProvider preferencesProvider,
        IGrowthContextService growthContextService)
    {
        _repository = repository;
        _goalRepository = goalRepository;
        _skillRepository = skillRepository;
        _projectRepository = projectRepository;
        _habitRepository = habitRepository;
        _preferencesProvider = preferencesProvider;
        _growthContextService = growthContextService;
    }

    private async Task<IEnumerable<CareerPlan>> FilterSampleDataAsync(IEnumerable<CareerPlan> plans)
    {
        if (await _preferencesProvider.ShowSampleDataAsync())
            return plans;
        return plans.Where(p => !p.IsSampleData);
    }

    public async Task<CareerPlan?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CareerPlan>> GetAllAsync()
    {
        var plans = await _repository.GetAllAsync();
        return await FilterSampleDataAsync(plans);
    }

    public async Task<IEnumerable<CareerPlan>> GetByStatusAsync(CareerPlanStatus status)
    {
        var plans = await _repository.QueryAsync(p => p.Status == status);
        return await FilterSampleDataAsync(plans);
    }

    public async Task<IEnumerable<CareerPlan>> GetActivePlansAsync()
    {
        var plans = await _repository.QueryAsync(p => p.Status == CareerPlanStatus.Active);
        return await FilterSampleDataAsync(plans);
    }

    public async Task<CareerPlan> CreateAsync(CareerPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return await _repository.AddAsync(plan);
    }

    public async Task<CareerPlan> UpdateAsync(CareerPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return await _repository.UpdateAsync(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        var plan = await _repository.GetByIdAsync(id);
        if (plan != null)
        {
            plan.Status = CareerPlanStatus.Archived;
            await _repository.UpdateAsync(plan);
        }
    }

    public async Task<CareerPlan> AddMilestoneAsync(Guid planId, CareerMilestone milestone)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        milestone.SortOrder = plan.Milestones.Count;
        plan.Milestones.Add(milestone);
        return await _repository.UpdateAsync(plan);
    }

    public async Task<CareerPlan> UpdateMilestoneAsync(Guid planId, CareerMilestone milestone)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        var index = plan.Milestones.FindIndex(m => m.Id == milestone.Id);
        if (index < 0)
            throw new InvalidOperationException($"Milestone {milestone.Id} not found in plan {planId}");

        plan.Milestones[index] = milestone;
        return await _repository.UpdateAsync(plan);
    }

    public async Task<CareerPlan> RemoveMilestoneAsync(Guid planId, Guid milestoneId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        plan.Milestones.RemoveAll(m => m.Id == milestoneId);
        return await _repository.UpdateAsync(plan);
    }

    public async Task<CareerPlan> CompleteMilestoneAsync(Guid planId, Guid milestoneId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        var milestone = plan.Milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new InvalidOperationException($"Milestone {milestoneId} not found in plan {planId}");

        milestone.Status = MilestoneStatus.Completed;
        milestone.CompletedDate = DateTime.UtcNow;
        var result = await _repository.UpdateAsync(plan);

        await _growthContextService.CaptureSnapshotAsync(SnapshotTrigger.MilestoneCompletion);

        return result;
    }

    public async Task LinkGoalAsync(Guid planId, Guid goalId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (!plan.LinkedGoalIds.Contains(goalId))
        {
            plan.LinkedGoalIds.Add(goalId);
            await _repository.UpdateAsync(plan);
        }
    }

    public async Task UnlinkGoalAsync(Guid planId, Guid goalId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (plan.LinkedGoalIds.Remove(goalId))
            await _repository.UpdateAsync(plan);
    }

    public async Task LinkSkillAsync(Guid planId, Guid skillId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (!plan.LinkedSkillIds.Contains(skillId))
        {
            plan.LinkedSkillIds.Add(skillId);
            await _repository.UpdateAsync(plan);
        }
    }

    public async Task UnlinkSkillAsync(Guid planId, Guid skillId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (plan.LinkedSkillIds.Remove(skillId))
            await _repository.UpdateAsync(plan);
    }

    public async Task LinkProjectAsync(Guid planId, Guid projectId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (!plan.LinkedProjectIds.Contains(projectId))
        {
            plan.LinkedProjectIds.Add(projectId);
            await _repository.UpdateAsync(plan);
        }
    }

    public async Task UnlinkProjectAsync(Guid planId, Guid projectId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (plan.LinkedProjectIds.Remove(projectId))
            await _repository.UpdateAsync(plan);
    }

    public async Task LinkHabitAsync(Guid planId, Guid habitId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (!plan.LinkedHabitIds.Contains(habitId))
        {
            plan.LinkedHabitIds.Add(habitId);
            await _repository.UpdateAsync(plan);
        }
    }

    public async Task UnlinkHabitAsync(Guid planId, Guid habitId)
    {
        var plan = await _repository.GetByIdAsync(planId)
            ?? throw new InvalidOperationException($"Career plan {planId} not found");

        if (plan.LinkedHabitIds.Remove(habitId))
            await _repository.UpdateAsync(plan);
    }

    public async Task<IEnumerable<Goal>> GetLinkedGoalsAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId);
        if (plan == null || plan.LinkedGoalIds.Count == 0)
            return Enumerable.Empty<Goal>();

        var goals = new List<Goal>();
        foreach (var goalId in plan.LinkedGoalIds)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId);
            if (goal != null) goals.Add(goal);
        }
        return goals;
    }

    public async Task<IEnumerable<Skill>> GetLinkedSkillsAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId);
        if (plan == null || plan.LinkedSkillIds.Count == 0)
            return Enumerable.Empty<Skill>();

        var skills = new List<Skill>();
        foreach (var skillId in plan.LinkedSkillIds)
        {
            var skill = await _skillRepository.GetByIdAsync(skillId);
            if (skill != null) skills.Add(skill);
        }
        return skills;
    }

    public async Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId);
        if (plan == null || plan.LinkedProjectIds.Count == 0)
            return Enumerable.Empty<Project>();

        var projects = new List<Project>();
        foreach (var projectId in plan.LinkedProjectIds)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project != null) projects.Add(project);
        }
        return projects;
    }

    public async Task<IEnumerable<Habit>> GetLinkedHabitsAsync(Guid planId)
    {
        var plan = await _repository.GetByIdAsync(planId);
        if (plan == null || plan.LinkedHabitIds.Count == 0)
            return Enumerable.Empty<Habit>();

        var habits = new List<Habit>();
        foreach (var habitId in plan.LinkedHabitIds)
        {
            var habit = await _habitRepository.GetByIdAsync(habitId);
            if (habit != null) habits.Add(habit);
        }
        return habits;
    }
}
