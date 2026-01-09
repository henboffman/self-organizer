using System.Linq.Expressions;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Data;

public class IndexedDbRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IIndexedDbService _dbService;
    private readonly string _storeName;

    public IndexedDbRepository(IIndexedDbService dbService, string storeName)
    {
        _dbService = dbService;
        _storeName = storeName;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbService.GetAsync<T>(_storeName, id.ToString());
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbService.GetAllAsync<T>(_storeName);
    }

    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.AsQueryable().Where(predicate).ToList();
    }

    public async Task<T> AddAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        return await _dbService.AddAsync(_storeName, entity);
    }

    public async Task<T> UpdateAsync(T entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        return await _dbService.PutAsync(_storeName, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _dbService.DeleteAsync(_storeName, id.ToString());
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbService.CountAsync(_storeName);
        }
        var all = await GetAllAsync();
        return all.AsQueryable().Where(predicate).Count();
    }
}

public static class StoreNames
{
    public const string Captures = "captures";
    public const string Tasks = "tasks";
    public const string Projects = "projects";
    public const string Events = "events";
    public const string TimeBlocks = "timeBlocks";
    public const string Contacts = "contacts";
    public const string References = "references";
    public const string Contexts = "contexts";
    public const string Categories = "categories";
    public const string Preferences = "preferences";
    public const string DailySnapshots = "dailySnapshots";
    public const string Goals = "goals";
    public const string Ideas = "ideas";
}
