namespace SelfOrganizer.App.Services.Data;

public interface IIndexedDbService
{
    Task InitializeAsync();
    Task<T?> GetAsync<T>(string storeName, string id) where T : class;
    Task<List<T>> GetAllAsync<T>(string storeName) where T : class;
    Task<T> AddAsync<T>(string storeName, T item) where T : class;
    Task<T> PutAsync<T>(string storeName, T item) where T : class;
    Task DeleteAsync(string storeName, string id);
    Task ClearAsync(string storeName);
    Task<int> CountAsync(string storeName);
    Task<List<T>> GetByIndexAsync<T>(string storeName, string indexName, object value) where T : class;
    Task<List<T>> GetByIndexRangeAsync<T>(string storeName, string indexName, object lower, object upper) where T : class;
    Task<string> ExportAllAsync();
    Task ImportAllAsync(string jsonData);
}
