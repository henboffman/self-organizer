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
}
