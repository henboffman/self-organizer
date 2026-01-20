using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Provides cached access to user preferences for efficient querying across services
/// </summary>
public class UserPreferencesProvider : IUserPreferencesProvider
{
    private readonly IRepository<UserPreferences> _repository;
    private UserPreferences? _cached;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public UserPreferencesProvider(IRepository<UserPreferences> repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferences> GetPreferencesAsync()
    {
        if (_cached != null)
            return _cached;

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cached != null)
                return _cached;

            var prefs = (await _repository.GetAllAsync()).FirstOrDefault();
            _cached = prefs ?? new UserPreferences();
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ShowSampleDataAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs.ShowSampleData;
    }

    public void InvalidateCache()
    {
        _cached = null;
    }
}
