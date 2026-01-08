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
}
