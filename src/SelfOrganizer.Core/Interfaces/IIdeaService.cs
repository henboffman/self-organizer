using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface IIdeaService
{
    Task<Idea?> GetByIdAsync(Guid id);
    Task<IEnumerable<Idea>> GetAllAsync();
    Task<IEnumerable<Idea>> GetActiveAsync();
    Task<IEnumerable<Idea>> GetByStatusAsync(IdeaStatus status);
    Task<IEnumerable<Idea>> GetByGoalAsync(Guid goalId);
    Task<IEnumerable<Idea>> GetByProjectAsync(Guid projectId);
    Task<Idea> CreateAsync(Idea idea);
    Task<Idea> UpdateAsync(Idea idea);
    Task DeleteAsync(Guid id);
    Task<Idea> ArchiveAsync(Guid id);
    Task<Idea> DismissAsync(Guid id);

    /// <summary>
    /// Converts an idea to a task and marks the idea as converted
    /// </summary>
    Task<TodoTask> ConvertToTaskAsync(Guid ideaId, TodoTask taskTemplate);

    Task<int> GetActiveCountAsync();
    Task<IEnumerable<Idea>> SearchAsync(string query);
}
