namespace SelfOrganizer.Core.Interfaces;

using SelfOrganizer.Core.Models;

/// <summary>
/// Service for managing contexts (where/how tasks can be done)
/// </summary>
public interface IContextService
{
    /// <summary>
    /// Get all contexts sorted by MRU (most recently used first)
    /// </summary>
    Task<IEnumerable<Context>> GetAllSortedByMruAsync();

    /// <summary>
    /// Get a context by ID
    /// </summary>
    Task<Context?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get a context by name
    /// </summary>
    Task<Context?> GetByNameAsync(string name);

    /// <summary>
    /// Create a new context
    /// </summary>
    Task<Context> CreateAsync(string name, string? icon = null, string? color = null);

    /// <summary>
    /// Update an existing context
    /// </summary>
    Task<Context> UpdateAsync(Context context);

    /// <summary>
    /// Delete a context (only user-created contexts can be deleted)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Record usage of a context (updates MRU tracking)
    /// </summary>
    Task RecordUsageAsync(string contextName);

    /// <summary>
    /// Record usage of multiple contexts
    /// </summary>
    Task RecordUsageAsync(IEnumerable<string> contextNames);

    /// <summary>
    /// Ensure built-in contexts exist (called on app startup)
    /// </summary>
    Task EnsureBuiltInContextsAsync();
}
