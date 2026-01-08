using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, SearchOptions? options = null);
    IEnumerable<QuickAction> GetQuickActions();
}
