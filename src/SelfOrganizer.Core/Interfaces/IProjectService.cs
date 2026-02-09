using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface IProjectService
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status);
    Task<IEnumerable<Project>> GetActiveAsync();
    Task<IEnumerable<Project>> GetStalledProjectsAsync(); // Projects with no next action
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task<Project> CompleteAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<bool> HasNextActionAsync(Guid projectId);
    Task<(int completed, int total)> GetProgressAsync(Guid projectId);
    /// <summary>
    /// Gets progress for all specified projects in a single batch operation.
    /// More efficient than calling GetProgressAsync for each project individually.
    /// </summary>
    Task<Dictionary<Guid, (int completed, int total)>> GetProgressBatchAsync(IEnumerable<Guid> projectIds);

    /// <summary>
    /// Sets the designated next action task for a project.
    /// Demotes any existing NextAction status tasks in the project to Active.
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <param name="taskId">The task to designate as next action, or null to clear</param>
    Task SetNextActionAsync(Guid projectId, Guid? taskId);

    /// <summary>
    /// Clears the next action if the specified task matches.
    /// Called when a task is completed or deleted.
    /// </summary>
    Task ClearNextActionIfMatchesAsync(Guid projectId, Guid taskId);

    /// <summary>
    /// Links a skill to this project (bidirectional).
    /// </summary>
    Task LinkSkillAsync(Guid projectId, Guid skillId);

    /// <summary>
    /// Unlinks a skill from this project (bidirectional).
    /// </summary>
    Task UnlinkSkillAsync(Guid projectId, Guid skillId);
}
