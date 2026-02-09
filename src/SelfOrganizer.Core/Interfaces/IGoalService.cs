using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface IGoalService
{
    Task<Goal?> GetByIdAsync(Guid id);
    Task<IEnumerable<Goal>> GetAllAsync();
    Task<IEnumerable<Goal>> GetByStatusAsync(GoalStatus status);
    Task<IEnumerable<Goal>> GetActiveGoalsAsync();
    Task<Goal> CreateAsync(Goal goal);
    Task<Goal> UpdateAsync(Goal goal);
    Task DeleteAsync(Guid id);
    Task<Goal> CompleteAsync(Guid id);
    Task<double> CalculateProgressAsync(Guid goalId);
    Task LinkProjectAsync(Guid goalId, Guid projectId);
    Task UnlinkProjectAsync(Guid goalId, Guid projectId);
    Task LinkTaskAsync(Guid goalId, Guid taskId);
    Task UnlinkTaskAsync(Guid goalId, Guid taskId);
    Task<IEnumerable<TodoTask>> GetLinkedTasksAsync(Guid goalId);
    Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid goalId);

    /// <summary>
    /// Sets the designated next action task for a goal.
    /// Demotes any existing NextAction status tasks linked to the goal to Active.
    /// </summary>
    /// <param name="goalId">The goal ID</param>
    /// <param name="taskId">The task to designate as next action, or null to clear</param>
    Task SetNextActionAsync(Guid goalId, Guid? taskId);

    /// <summary>
    /// Clears the next action if the specified task matches.
    /// Called when a task is completed or deleted.
    /// </summary>
    Task ClearNextActionIfMatchesAsync(Guid goalId, Guid taskId);

    /// <summary>
    /// Links a skill to this goal (bidirectional).
    /// </summary>
    Task LinkSkillAsync(Guid goalId, Guid skillId);

    /// <summary>
    /// Unlinks a skill from this goal (bidirectional).
    /// </summary>
    Task UnlinkSkillAsync(Guid goalId, Guid skillId);
}
