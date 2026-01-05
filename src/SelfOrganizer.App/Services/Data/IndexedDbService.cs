using System.Text.Json;
using Microsoft.JSInterop;

namespace SelfOrganizer.App.Services.Data;

public class IndexedDbService : IIndexedDbService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.initialize");
        _isInitialized = true;
    }

    public async Task<T?> GetAsync<T>(string storeName, string id) where T : class
    {
        await EnsureInitializedAsync();
        var result = await _jsRuntime.InvokeAsync<JsonElement?>("indexedDbInterop.get", storeName, id);
        if (result == null || result.Value.ValueKind == JsonValueKind.Null)
            return null;
        return JsonSerializer.Deserialize<T>(result.Value.GetRawText(), JsonOptions);
    }

    public async Task<List<T>> GetAllAsync<T>(string storeName) where T : class
    {
        await EnsureInitializedAsync();
        var result = await _jsRuntime.InvokeAsync<JsonElement>("indexedDbInterop.getAll", storeName);
        return JsonSerializer.Deserialize<List<T>>(result.GetRawText(), JsonOptions) ?? new List<T>();
    }

    public async Task<T> AddAsync<T>(string storeName, T item) where T : class
    {
        await EnsureInitializedAsync();
        var jsonItem = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(item, JsonOptions), JsonOptions);
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.add", storeName, jsonItem);
        return item;
    }

    public async Task<T> PutAsync<T>(string storeName, T item) where T : class
    {
        await EnsureInitializedAsync();
        var jsonItem = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(item, JsonOptions), JsonOptions);
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.put", storeName, jsonItem);
        return item;
    }

    public async Task DeleteAsync(string storeName, string id)
    {
        await EnsureInitializedAsync();
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.delete", storeName, id);
    }

    public async Task ClearAsync(string storeName)
    {
        await EnsureInitializedAsync();
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.clear", storeName);
    }

    public async Task<int> CountAsync(string storeName)
    {
        await EnsureInitializedAsync();
        return await _jsRuntime.InvokeAsync<int>("indexedDbInterop.count", storeName);
    }

    public async Task<List<T>> GetByIndexAsync<T>(string storeName, string indexName, object value) where T : class
    {
        await EnsureInitializedAsync();
        var result = await _jsRuntime.InvokeAsync<JsonElement>("indexedDbInterop.getByIndex", storeName, indexName, value);
        return JsonSerializer.Deserialize<List<T>>(result.GetRawText(), JsonOptions) ?? new List<T>();
    }

    public async Task<List<T>> GetByIndexRangeAsync<T>(string storeName, string indexName, object lower, object upper) where T : class
    {
        await EnsureInitializedAsync();
        var result = await _jsRuntime.InvokeAsync<JsonElement>("indexedDbInterop.getByIndexRange", storeName, indexName, lower, upper);
        return JsonSerializer.Deserialize<List<T>>(result.GetRawText(), JsonOptions) ?? new List<T>();
    }

    public async Task<string> ExportAllAsync()
    {
        await EnsureInitializedAsync();
        return await _jsRuntime.InvokeAsync<string>("indexedDbInterop.exportAll");
    }

    public async Task ImportAllAsync(string jsonData)
    {
        await EnsureInitializedAsync();
        await _jsRuntime.InvokeVoidAsync("indexedDbInterop.importAll", jsonData);
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
            await InitializeAsync();
    }
}
