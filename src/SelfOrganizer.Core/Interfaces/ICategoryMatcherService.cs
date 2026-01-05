using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public record CategoryMatch(MeetingCategory Category, double Confidence);

public interface ICategoryMatcherService
{
    MeetingCategory? MatchCategory(string title, string? description = null);
    IEnumerable<CategoryMatch> GetPossibleMatches(string text);
    Task<IEnumerable<CategoryDefinition>> GetCategoryDefinitionsAsync();
    Task<CategoryDefinition> AddCategoryDefinitionAsync(CategoryDefinition definition);
    Task<CategoryDefinition> UpdateCategoryDefinitionAsync(CategoryDefinition definition);
    Task DeleteCategoryDefinitionAsync(Guid id);
}
