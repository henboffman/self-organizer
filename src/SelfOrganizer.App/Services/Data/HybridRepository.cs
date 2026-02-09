using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Data;

/// <summary>
/// Repository that uses server API when online and falls back to IndexedDB when offline.
/// Queues offline operations for later sync.
/// </summary>
public class HybridRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly HttpRepository<T> _httpRepository;
    private readonly IndexedDbRepository<T> _indexedDbRepository;
    private readonly INetworkStatusService _networkStatus;
    private readonly IPendingOperationQueue _pendingQueue;
    private readonly string _entityType;

    public HybridRepository(
        HttpClient httpClient,
        IIndexedDbService indexedDbService,
        INetworkStatusService networkStatus,
        IPendingOperationQueue pendingQueue,
        string entityType,
        string storeName)
    {
        _httpRepository = new HttpRepository<T>(httpClient, entityType);
        _indexedDbRepository = new IndexedDbRepository<T>(indexedDbService, storeName);
        _networkStatus = networkStatus;
        _pendingQueue = pendingQueue;
        _entityType = entityType;
    }

    private bool IsOnline => _networkStatus.IsOnline;

    public async Task<T?> GetByIdAsync(Guid id)
    {
        if (IsOnline)
        {
            try
            {
                var result = await _httpRepository.GetByIdAsync(id);
                if (result != null)
                {
                    // Cache in IndexedDB
                    await _indexedDbRepository.UpdateAsync(result);
                }
                return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Fall back to local on network errors or invalid JSON (e.g., HTML 404 page)
                return await _indexedDbRepository.GetByIdAsync(id);
            }
        }

        return await _indexedDbRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        if (IsOnline)
        {
            try
            {
                var serverResults = (await _httpRepository.GetAllAsync()).ToList();
                var localResults = (await _indexedDbRepository.GetAllAsync()).ToList();

                // Find items that exist locally but not on the server
                var serverIds = new HashSet<Guid>(serverResults.Select(e => e.Id));
                var localOnlyItems = localResults.Where(e => !serverIds.Contains(e.Id)).ToList();

                // Queue local-only items for sync to server (dedup against pending queue)
                if (localOnlyItems.Count > 0)
                {
                    var pending = await _pendingQueue.GetPendingAsync();
                    var pendingEntityIds = new HashSet<Guid>(pending.Select(p => p.EntityId));

                    foreach (var entity in localOnlyItems)
                    {
                        if (!pendingEntityIds.Contains(entity.Id))
                        {
                            await _pendingQueue.EnqueueAsync(new PendingOperation
                            {
                                EntityType = _entityType,
                                EntityId = entity.Id,
                                OperationType = OperationType.Create,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                }

                // Cache server items locally
                foreach (var entity in serverResults)
                {
                    try
                    {
                        await _indexedDbRepository.UpdateAsync(entity);
                    }
                    catch
                    {
                        await _indexedDbRepository.AddAsync(entity);
                    }
                }

                // Return merged: server items + local-only items
                return serverResults.Concat(localOnlyItems);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Fall back to local on network errors or invalid JSON (e.g., HTML 404 page)
                return await _indexedDbRepository.GetAllAsync();
            }
        }

        return await _indexedDbRepository.GetAllAsync();
    }

    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.AsQueryable().Where(predicate).ToList();
    }

    public async Task<T> AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;

        // Always save locally first
        var localResult = await _indexedDbRepository.AddAsync(entity);

        if (IsOnline)
        {
            try
            {
                var serverResult = await _httpRepository.AddAsync(entity);
                // Update local with server response
                await _indexedDbRepository.UpdateAsync(serverResult);
                return serverResult;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Queue for sync on network errors or invalid JSON
                await _pendingQueue.EnqueueAsync(new PendingOperation
                {
                    EntityType = _entityType,
                    EntityId = entity.Id,
                    OperationType = OperationType.Create,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Queue for sync
            await _pendingQueue.EnqueueAsync(new PendingOperation
            {
                EntityType = _entityType,
                EntityId = entity.Id,
                OperationType = OperationType.Create,
                Timestamp = DateTime.UtcNow
            });
        }

        return localResult;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.ModifiedAt = DateTime.UtcNow;

        // Always save locally first
        var localResult = await _indexedDbRepository.UpdateAsync(entity);

        if (IsOnline)
        {
            try
            {
                var serverResult = await _httpRepository.UpdateAsync(entity);
                // Update local with server response
                await _indexedDbRepository.UpdateAsync(serverResult);
                return serverResult;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Queue for sync on network errors or invalid JSON
                await _pendingQueue.EnqueueAsync(new PendingOperation
                {
                    EntityType = _entityType,
                    EntityId = entity.Id,
                    OperationType = OperationType.Update,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Queue for sync
            await _pendingQueue.EnqueueAsync(new PendingOperation
            {
                EntityType = _entityType,
                EntityId = entity.Id,
                OperationType = OperationType.Update,
                Timestamp = DateTime.UtcNow
            });
        }

        return localResult;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Always delete locally first
        await _indexedDbRepository.DeleteAsync(id);

        if (IsOnline)
        {
            try
            {
                await _httpRepository.DeleteAsync(id);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // Queue for sync on network errors or invalid JSON
                await _pendingQueue.EnqueueAsync(new PendingOperation
                {
                    EntityType = _entityType,
                    EntityId = id,
                    OperationType = OperationType.Delete,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Queue for sync
            await _pendingQueue.EnqueueAsync(new PendingOperation
            {
                EntityType = _entityType,
                EntityId = id,
                OperationType = OperationType.Delete,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null && IsOnline)
        {
            try
            {
                return await _httpRepository.CountAsync(predicate);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                return await _indexedDbRepository.CountAsync(predicate);
            }
        }

        return await _indexedDbRepository.CountAsync(predicate);
    }
}

/// <summary>
/// Service for tracking network connectivity status.
/// </summary>
public interface INetworkStatusService
{
    bool IsOnline { get; }
    event Action<bool>? OnStatusChanged;
}

/// <summary>
/// Network status service that checks browser connectivity.
/// </summary>
public class NetworkStatusService : INetworkStatusService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isOnline = true;
    private DotNetObjectReference<NetworkStatusService>? _dotNetRef;

    public bool IsOnline => _isOnline;
    public event Action<bool>? OnStatusChanged;

    public NetworkStatusService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            _isOnline = await _jsRuntime.InvokeAsync<bool>("navigator.onLine");

            // Register for online/offline events
            await _jsRuntime.InvokeVoidAsync("eval", @"
                window.networkStatusCallback = (dotNetRef) => {
                    window.addEventListener('online', () => dotNetRef.invokeMethodAsync('SetOnlineStatus', true));
                    window.addEventListener('offline', () => dotNetRef.invokeMethodAsync('SetOnlineStatus', false));
                };
            ");

            await _jsRuntime.InvokeVoidAsync("networkStatusCallback", _dotNetRef);
        }
        catch
        {
            // JS interop might fail during prerendering
            _isOnline = true;
        }
    }

    [JSInvokable]
    public void SetOnlineStatus(bool isOnline)
    {
        if (_isOnline != isOnline)
        {
            _isOnline = isOnline;
            OnStatusChanged?.Invoke(isOnline);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        await ValueTask.CompletedTask;
    }
}

/// <summary>
/// Queue for pending operations that need to sync when back online.
/// </summary>
public interface IPendingOperationQueue
{
    Task EnqueueAsync(PendingOperation operation);
    Task<IEnumerable<PendingOperation>> GetPendingAsync();
    Task RemoveAsync(Guid operationId);
    Task ClearAsync();
}

/// <summary>
/// Pending operation queue stored in IndexedDB.
/// </summary>
public class PendingOperationQueue : IPendingOperationQueue
{
    private readonly IIndexedDbService _indexedDbService;
    private const string StoreName = "pendingOperations";

    public PendingOperationQueue(IIndexedDbService indexedDbService)
    {
        _indexedDbService = indexedDbService;
    }

    public async Task EnqueueAsync(PendingOperation operation)
    {
        operation.Id = Guid.NewGuid();
        await _indexedDbService.AddAsync(StoreName, operation);
    }

    public async Task<IEnumerable<PendingOperation>> GetPendingAsync()
    {
        return await _indexedDbService.GetAllAsync<PendingOperation>(StoreName);
    }

    public async Task RemoveAsync(Guid operationId)
    {
        await _indexedDbService.DeleteAsync(StoreName, operationId.ToString());
    }

    public async Task ClearAsync()
    {
        var operations = await GetPendingAsync();
        foreach (var op in operations)
        {
            await RemoveAsync(op.Id);
        }
    }
}

/// <summary>
/// Represents a pending operation that needs to sync.
/// </summary>
public class PendingOperation
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public OperationType OperationType { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Type of operation that was performed.
/// </summary>
public enum OperationType
{
    Create,
    Update,
    Delete
}
