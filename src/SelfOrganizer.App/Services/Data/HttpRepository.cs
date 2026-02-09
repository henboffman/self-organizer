using System.Linq.Expressions;
using System.Net.Http.Json;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Data;

/// <summary>
/// Repository implementation that calls server API endpoints.
/// </summary>
public class HttpRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly HttpClient _httpClient;
    private readonly string _entityType;

    public HttpRepository(HttpClient httpClient, string entityType)
    {
        _httpClient = httpClient;
        _entityType = entityType;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>($"api/data/{_entityType}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<T>>($"api/data/{_entityType}");
        return result ?? new List<T>();
    }

    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        // Server-side filtering would require changes to the API
        // For now, get all and filter client-side
        var all = await GetAllAsync();
        return all.AsQueryable().Where(predicate).ToList();
    }

    public async Task<T> AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;

        var response = await _httpClient.PostAsJsonAsync($"api/data/{_entityType}", entity);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<T>();
        return result ?? throw new InvalidOperationException("Server returned null entity");
    }

    public async Task<T> UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.ModifiedAt = DateTime.UtcNow;

        var response = await _httpClient.PutAsJsonAsync($"api/data/{_entityType}/{entity.Id}", entity);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<T>();
        return result ?? throw new InvalidOperationException("Server returned null entity");
    }

    public async Task DeleteAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/data/{_entityType}/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            var response = await _httpClient.GetFromJsonAsync<CountResponse>($"api/data/{_entityType}/count");
            return response?.Count ?? 0;
        }

        // For filtered count, get all and filter client-side
        var all = await GetAllAsync();
        return all.AsQueryable().Where(predicate).Count();
    }

    private class CountResponse
    {
        public int Count { get; set; }
    }
}

/// <summary>
/// Maps entity types to their API endpoint names.
/// </summary>
public static class EntityTypeNames
{
    public const string Tasks = "tasks";
    public const string Projects = "projects";
    public const string Goals = "goals";
    public const string Ideas = "ideas";
    public const string Captures = "captures";
    public const string Events = "events";
    public const string Habits = "habits";
    public const string HabitLogs = "habitlogs";
    public const string References = "references";
    public const string Contexts = "contexts";
    public const string Categories = "categories";
    public const string Preferences = "preferences";
    public const string DailySnapshots = "dailysnapshots";
    public const string WeeklySnapshots = "weeklysnapshots";
    public const string TimeBlocks = "timeblocks";
    public const string Contacts = "contacts";
    public const string EntityLinkRules = "entitylinkrules";
    public const string FocusSessionLogs = "focussessionlogs";
    public const string TaskReminderSnoozes = "taskremindersnoozes";
    public const string Skills = "skills";
    public const string CareerPlans = "careerplans";
    public const string GrowthSnapshots = "growthsnapshots";
}
