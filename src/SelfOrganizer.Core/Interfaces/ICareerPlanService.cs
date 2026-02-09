using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ICareerPlanService
{
    // CRUD
    Task<CareerPlan?> GetByIdAsync(Guid id);
    Task<IEnumerable<CareerPlan>> GetAllAsync();
    Task<IEnumerable<CareerPlan>> GetByStatusAsync(CareerPlanStatus status);
    Task<IEnumerable<CareerPlan>> GetActivePlansAsync();
    Task<CareerPlan> CreateAsync(CareerPlan plan);
    Task<CareerPlan> UpdateAsync(CareerPlan plan);
    Task DeleteAsync(Guid id);

    // Milestone management
    Task<CareerPlan> AddMilestoneAsync(Guid planId, CareerMilestone milestone);
    Task<CareerPlan> UpdateMilestoneAsync(Guid planId, CareerMilestone milestone);
    Task<CareerPlan> RemoveMilestoneAsync(Guid planId, Guid milestoneId);
    Task<CareerPlan> CompleteMilestoneAsync(Guid planId, Guid milestoneId);

    // Entity linking
    Task LinkGoalAsync(Guid planId, Guid goalId);
    Task UnlinkGoalAsync(Guid planId, Guid goalId);
    Task LinkSkillAsync(Guid planId, Guid skillId);
    Task UnlinkSkillAsync(Guid planId, Guid skillId);
    Task LinkProjectAsync(Guid planId, Guid projectId);
    Task UnlinkProjectAsync(Guid planId, Guid projectId);
    Task LinkHabitAsync(Guid planId, Guid habitId);
    Task UnlinkHabitAsync(Guid planId, Guid habitId);

    // Aggregation
    Task<IEnumerable<Goal>> GetLinkedGoalsAsync(Guid planId);
    Task<IEnumerable<Skill>> GetLinkedSkillsAsync(Guid planId);
    Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid planId);
    Task<IEnumerable<Habit>> GetLinkedHabitsAsync(Guid planId);
}
