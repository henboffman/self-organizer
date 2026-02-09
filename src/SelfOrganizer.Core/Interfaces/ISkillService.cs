using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ISkillService
{
    // CRUD operations
    Task<Skill?> GetByIdAsync(Guid id);
    Task<IEnumerable<Skill>> GetAllAsync();
    Task<IEnumerable<Skill>> GetByTypeAsync(SkillType type);
    Task<IEnumerable<Skill>> GetByCategoryAsync(SkillCategory category);
    Task<IEnumerable<Skill>> GetActiveSkillsAsync();
    Task<Skill> CreateAsync(Skill skill);
    Task<Skill> UpdateAsync(Skill skill);
    Task DeleteAsync(Guid id);

    // Proficiency management
    Task<Skill> UpdateProficiencyAsync(Guid skillId, int currentLevel, int? targetLevel = null);

    // Goal linking (bidirectional)
    Task LinkGoalAsync(Guid skillId, Guid goalId);
    Task UnlinkGoalAsync(Guid skillId, Guid goalId);
    Task<IEnumerable<Goal>> GetLinkedGoalsAsync(Guid skillId);

    // Project linking (bidirectional)
    Task LinkProjectAsync(Guid skillId, Guid projectId);
    Task UnlinkProjectAsync(Guid skillId, Guid projectId);
    Task<IEnumerable<Project>> GetLinkedProjectsAsync(Guid skillId);

    // Task linking (tracks tasks that exercise this skill)
    Task LinkTaskAsync(Guid skillId, Guid taskId);
    Task UnlinkTaskAsync(Guid skillId, Guid taskId);
    Task<IEnumerable<TodoTask>> GetLinkedTasksAsync(Guid skillId);
    Task<int> GetCompletedTaskCountAsync(Guid skillId);

    // Habit linking (bidirectional)
    Task LinkHabitAsync(Guid skillId, Guid habitId);
    Task UnlinkHabitAsync(Guid skillId, Guid habitId);
    Task<IEnumerable<Habit>> GetLinkedHabitsAsync(Guid skillId);

    // Get skills linked to a specific entity
    Task<IEnumerable<Skill>> GetSkillsForGoalAsync(Guid goalId);
    Task<IEnumerable<Skill>> GetSkillsForProjectAsync(Guid projectId);
    Task<IEnumerable<Skill>> GetSkillsForTaskAsync(Guid taskId);
    Task<IEnumerable<Skill>> GetSkillsForHabitAsync(Guid habitId);
}
