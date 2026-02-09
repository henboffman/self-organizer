using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Data;

/// <summary>
/// Service for synchronizing local IndexedDB data with remote SQL Server database.
/// Implements the Petri net model from PETRI-MODELS.md for database sync operations.
/// </summary>
public class DbSyncService : IDbSyncService
{
    private readonly IIndexedDbService _indexedDbService;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IEntraAuthService? _authService;

    private SyncStatus _status = SyncStatus.Offline;
    private readonly List<PendingChange> _pendingChanges = new();
    private readonly List<SyncConflict> _conflicts = new();
    private DateTime? _lastSyncTime;

    private const string SyncMetadataStore = "syncMetadata";
    private const string PendingChangesStore = "pendingChanges";
    private const string ApiBaseUrl = "api/sync";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SyncStatus Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                OnStatusChanged?.Invoke(_status);
            }
        }
    }

    public event Action<SyncStatus>? OnStatusChanged;
    public event Action<SyncProgress>? OnProgressChanged;

    public DbSyncService(
        IIndexedDbService indexedDbService,
        HttpClient httpClient,
        IJSRuntime jsRuntime,
        IEntraAuthService? authService = null)
    {
        _indexedDbService = indexedDbService;
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authService = authService;
    }

    /// <summary>
    /// Checks if there is data in IndexedDB that needs to be synced.
    /// </summary>
    public async Task<bool> HasLocalDataAsync()
    {
        try
        {
            var storeNames = new[]
            {
                StoreNames.Tasks, StoreNames.Projects, StoreNames.Goals,
                StoreNames.Ideas, StoreNames.Captures, StoreNames.Events,
                StoreNames.Habits, StoreNames.References, StoreNames.Contexts
            };

            foreach (var store in storeNames)
            {
                var count = await _indexedDbService.CountAsync(store);
                if (count > 0)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if sync is available (user is authenticated and server is reachable).
    /// </summary>
    public async Task<bool> IsSyncAvailableAsync()
    {
        // Check if auth service exists and is enabled
        if (_authService == null || !_authService.IsEnabled)
            return false;

        // Check if user is authenticated
        if (_authService.State != AuthState.Authenticated)
            return false;

        // Check server connectivity
        try
        {
            Status = SyncStatus.Checking;
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Status = SyncStatus.Offline;
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/health");
            var available = response.IsSuccessStatusCode;

            Status = available ? SyncStatus.Connected : SyncStatus.Offline;
            return available;
        }
        catch
        {
            Status = SyncStatus.Offline;
            return false;
        }
    }

    /// <summary>
    /// Performs initial sync - uploads all local data to server.
    /// Call this when user first authenticates.
    /// Follows the Petri net transitions: T1 (StartInitialSync) → P2 (Syncing)
    /// </summary>
    public async Task<SyncResult> PerformInitialSyncAsync()
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            if (!await IsSyncAvailableAsync())
            {
                result.Success = false;
                result.Error = "Sync not available. Please ensure you are logged in and have internet connectivity.";
                return result;
            }

            Status = SyncStatus.Syncing;

            // Get all entity types and their data
            var entityTypes = GetEntityStoreMapping();
            var totalItems = 0;
            var processedItems = 0;

            // Count total items first
            foreach (var (entityType, storeName) in entityTypes)
            {
                totalItems += await _indexedDbService.CountAsync(storeName);
            }

            ReportProgress(totalItems, 0, "Starting initial sync...");

            // Sync each entity type
            foreach (var (entityType, storeName) in entityTypes)
            {
                ReportProgress(totalItems, processedItems, $"Syncing {entityType}...");

                var syncedCount = await SyncStoreAsync(entityType, storeName);
                processedItems += syncedCount;
                result.ItemsSynced += syncedCount;

                ReportProgress(totalItems, processedItems, $"Synced {syncedCount} {entityType}");
            }

            // Save last sync time
            _lastSyncTime = DateTime.UtcNow;
            await SaveSyncMetadataAsync();

            result.Success = true;
            result.Messages.Add($"Successfully synced {result.ItemsSynced} items to server.");

            Status = _conflicts.Any() ? SyncStatus.HasConflicts : SyncStatus.Synced;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Initial sync failed: {ex.Message}";
            Status = SyncStatus.Error;
        }

        return result;
    }

    /// <summary>
    /// Performs incremental sync - syncs changes since last sync.
    /// </summary>
    public async Task<SyncResult> SyncChangesAsync()
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            if (!await IsSyncAvailableAsync())
            {
                result.Success = false;
                result.Error = "Sync not available.";
                return result;
            }

            Status = SyncStatus.Syncing;

            // First push any pending changes
            var pushResult = await ProcessOfflineQueueAsync();
            result.ItemsSynced += pushResult.ItemsSynced;

            // Then pull changes from server
            var pullResult = await PullChangesAsync(_lastSyncTime);
            result.ItemsSynced += pullResult.ItemsSynced;
            result.Conflicts = pullResult.Conflicts;

            _lastSyncTime = DateTime.UtcNow;
            await SaveSyncMetadataAsync();

            result.Success = true;
            Status = _conflicts.Any() ? SyncStatus.HasConflicts : SyncStatus.Synced;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Sync failed: {ex.Message}";
            Status = SyncStatus.Error;
        }

        return result;
    }

    /// <summary>
    /// Pushes a specific entity change to the server.
    /// </summary>
    public async Task<SyncResult> PushEntityChangeAsync(string entityType, Guid entityId, SyncOperation operation)
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            if (!await IsSyncAvailableAsync())
            {
                // Queue for later sync
                await QueueOfflineOperationAsync(entityType, entityId, operation, null);
                result.Success = true;
                result.Messages.Add("Change queued for sync when online.");
                Status = SyncStatus.PendingChanges;
                return result;
            }

            Status = SyncStatus.Syncing;

            var token = await _authService!.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var storeName = GetStoreNameForEntityType(entityType);
            if (string.IsNullOrEmpty(storeName))
            {
                result.Success = false;
                result.Error = $"Unknown entity type: {entityType}";
                return result;
            }

            switch (operation)
            {
                case SyncOperation.Create:
                case SyncOperation.Update:
                    var entity = await GetEntityFromStoreAsync(storeName, entityId);
                    if (entity != null)
                    {
                        var response = await _httpClient.PostAsJsonAsync(
                            $"{ApiBaseUrl}/{entityType}",
                            entity,
                            JsonOptions);

                        if (response.IsSuccessStatusCode)
                        {
                            result.Success = true;
                            result.ItemsSynced = 1;
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            result.Success = false;
                            result.Error = error;
                        }
                    }
                    break;

                case SyncOperation.Delete:
                    var deleteResponse = await _httpClient.DeleteAsync($"{ApiBaseUrl}/{entityType}/{entityId}");
                    result.Success = deleteResponse.IsSuccessStatusCode;
                    if (!result.Success)
                    {
                        result.Error = await deleteResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        result.ItemsSynced = 1;
                    }
                    break;
            }

            Status = SyncStatus.Synced;
        }
        catch (Exception ex)
        {
            // Queue for retry
            await QueueOfflineOperationAsync(entityType, entityId, operation, null);
            result.Success = false;
            result.Error = ex.Message;
            Status = SyncStatus.PendingChanges;
        }

        return result;
    }

    /// <summary>
    /// Pulls latest changes from server.
    /// </summary>
    public async Task<SyncResult> PullChangesAsync(DateTime? since = null)
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            var token = await _authService!.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = since.HasValue
                ? $"{ApiBaseUrl}/changes?since={since.Value:o}"
                : $"{ApiBaseUrl}/changes";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                result.Success = false;
                result.Error = "Failed to fetch changes from server.";
                return result;
            }

            var changes = await response.Content.ReadFromJsonAsync<ServerChangesResponse>(JsonOptions);
            if (changes == null)
            {
                result.Success = true;
                return result;
            }

            // Apply changes to local IndexedDB
            foreach (var change in changes.Changes)
            {
                await ApplyServerChangeAsync(change, result);
            }

            result.Success = true;
            result.ItemsSynced = changes.Changes.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Pull failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    public async Task<SyncResult> ResolveConflictAsync(SyncConflict conflict, ConflictResolution resolution)
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            switch (resolution)
            {
                case ConflictResolution.KeepLocal:
                    // Push local version to server
                    await PushEntityChangeAsync(conflict.EntityType, conflict.EntityId, SyncOperation.Update);
                    break;

                case ConflictResolution.KeepServer:
                    // Pull server version (already in ServerVersion field)
                    var storeName = GetStoreNameForEntityType(conflict.EntityType);
                    if (!string.IsNullOrEmpty(storeName))
                    {
                        var serverEntity = JsonSerializer.Deserialize<object>(conflict.ServerVersion, JsonOptions);
                        if (serverEntity != null)
                        {
                            await _indexedDbService.PutAsync(storeName, serverEntity);
                        }
                    }
                    break;

                case ConflictResolution.Merge:
                    // Merge logic would be entity-specific
                    // For now, default to keeping the most recently modified version
                    if (conflict.LocalModifiedAt > conflict.ServerModifiedAt)
                    {
                        await PushEntityChangeAsync(conflict.EntityType, conflict.EntityId, SyncOperation.Update);
                    }
                    else
                    {
                        var mergeStoreName = GetStoreNameForEntityType(conflict.EntityType);
                        if (!string.IsNullOrEmpty(mergeStoreName))
                        {
                            var serverData = JsonSerializer.Deserialize<object>(conflict.ServerVersion, JsonOptions);
                            if (serverData != null)
                            {
                                await _indexedDbService.PutAsync(mergeStoreName, serverData);
                            }
                        }
                    }
                    break;
            }

            // Remove the conflict from the list
            _conflicts.RemoveAll(c => c.Id == conflict.Id);

            result.Success = true;
            Status = _conflicts.Any() ? SyncStatus.HasConflicts : SyncStatus.Synced;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Failed to resolve conflict: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Gets pending changes that haven't been synced.
    /// </summary>
    public async Task<IEnumerable<PendingChange>> GetPendingChangesAsync()
    {
        try
        {
            var changes = await _indexedDbService.GetAllAsync<PendingChange>(PendingChangesStore);
            return changes;
        }
        catch
        {
            return _pendingChanges;
        }
    }

    /// <summary>
    /// Gets any conflicts that need user resolution.
    /// </summary>
    public Task<IEnumerable<SyncConflict>> GetConflictsAsync()
    {
        return Task.FromResult<IEnumerable<SyncConflict>>(_conflicts);
    }

    /// <summary>
    /// Queues an operation for sync when connectivity is restored.
    /// </summary>
    public async Task QueueOfflineOperationAsync(string entityType, Guid entityId, SyncOperation operation, object? data)
    {
        var change = new PendingChange
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            QueuedAt = DateTime.UtcNow,
            Data = data != null ? JsonSerializer.Serialize(data, JsonOptions) : null
        };

        try
        {
            await _indexedDbService.AddAsync(PendingChangesStore, change);
        }
        catch
        {
            // If IndexedDB fails, keep in memory
            _pendingChanges.Add(change);
        }

        Status = SyncStatus.PendingChanges;
    }

    /// <summary>
    /// Processes queued offline operations.
    /// </summary>
    public async Task<SyncResult> ProcessOfflineQueueAsync()
    {
        var result = new SyncResult { SyncTime = DateTime.UtcNow };

        try
        {
            var pendingChanges = (await GetPendingChangesAsync()).ToList();
            if (!pendingChanges.Any())
            {
                result.Success = true;
                return result;
            }

            var totalItems = pendingChanges.Count;
            var processedItems = 0;

            ReportProgress(totalItems, 0, "Processing offline queue...");

            foreach (var change in pendingChanges.OrderBy(c => c.QueuedAt))
            {
                var pushResult = await PushEntityChangeAsync(change.EntityType, change.EntityId, change.Operation);

                if (pushResult.Success)
                {
                    // Remove from pending queue
                    await _indexedDbService.DeleteAsync(PendingChangesStore, change.Id.ToString());
                    _pendingChanges.RemoveAll(c => c.Id == change.Id);
                    result.ItemsSynced++;
                }
                else
                {
                    // Increment retry count
                    change.RetryCount++;
                    if (change.RetryCount >= 3)
                    {
                        result.Messages.Add($"Failed to sync {change.EntityType} after 3 retries: {pushResult.Error}");
                    }
                }

                processedItems++;
                ReportProgress(totalItems, processedItems, $"Processed {processedItems}/{totalItems}");
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Failed to process offline queue: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Gets the last successful sync time.
    /// </summary>
    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        if (_lastSyncTime.HasValue)
            return _lastSyncTime;

        try
        {
            var metadata = await _indexedDbService.GetAsync<SyncMetadata>(SyncMetadataStore, "lastSync");
            _lastSyncTime = metadata?.LastSyncTime;
            return _lastSyncTime;
        }
        catch
        {
            return null;
        }
    }

    #region Private Helper Methods

    private Dictionary<string, string> GetEntityStoreMapping()
    {
        return new Dictionary<string, string>
        {
            { "tasks", StoreNames.Tasks },
            { "projects", StoreNames.Projects },
            { "goals", StoreNames.Goals },
            { "ideas", StoreNames.Ideas },
            { "captures", StoreNames.Captures },
            { "events", StoreNames.Events },
            { "habits", StoreNames.Habits },
            { "habitLogs", StoreNames.HabitLogs },
            { "references", StoreNames.References },
            { "contexts", StoreNames.Contexts },
            { "categories", StoreNames.Categories },
            { "contacts", StoreNames.Contacts },
            { "timeBlocks", StoreNames.TimeBlocks },
            { "focusSessions", StoreNames.FocusSessionLogs },
            { "dailySnapshots", StoreNames.DailySnapshots },
            { "weeklySnapshots", StoreNames.WeeklySnapshots }
        };
    }

    private string? GetStoreNameForEntityType(string entityType)
    {
        var mapping = GetEntityStoreMapping();
        return mapping.TryGetValue(entityType.ToLowerInvariant(), out var storeName) ? storeName : null;
    }

    private async Task<int> SyncStoreAsync(string entityType, string storeName)
    {
        var items = await _indexedDbService.GetAllAsync<object>(storeName);
        if (!items.Any())
            return 0;

        var token = await _authService!.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var batchRequest = new SyncBatchRequest
        {
            EntityType = entityType,
            Items = items
        };

        var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/batch", batchRequest, JsonOptions);

        if (response.IsSuccessStatusCode)
        {
            var batchResult = await response.Content.ReadFromJsonAsync<SyncBatchResponse>(JsonOptions);
            if (batchResult?.Conflicts != null)
            {
                _conflicts.AddRange(batchResult.Conflicts);
            }
            return items.Count;
        }

        return 0;
    }

    private async Task<object?> GetEntityFromStoreAsync(string storeName, Guid entityId)
    {
        return await _indexedDbService.GetAsync<object>(storeName, entityId.ToString());
    }

    private async Task ApplyServerChangeAsync(ServerChange change, SyncResult result)
    {
        var storeName = GetStoreNameForEntityType(change.EntityType);
        if (string.IsNullOrEmpty(storeName))
            return;

        try
        {
            // Check for conflicts
            var localEntity = await _indexedDbService.GetAsync<BaseEntity>(storeName, change.EntityId.ToString());
            if (localEntity != null && change.Operation == SyncOperation.Update)
            {
                // Check if local was modified after the server change
                if (localEntity.ModifiedAt > change.ModifiedAt)
                {
                    // Conflict detected
                    _conflicts.Add(new SyncConflict
                    {
                        Id = Guid.NewGuid(),
                        EntityType = change.EntityType,
                        EntityId = change.EntityId,
                        LocalVersion = JsonSerializer.Serialize(localEntity, JsonOptions),
                        ServerVersion = change.Data,
                        LocalModifiedAt = localEntity.ModifiedAt,
                        ServerModifiedAt = change.ModifiedAt,
                        DetectedAt = DateTime.UtcNow
                    });
                    result.Conflicts++;
                    return;
                }
            }

            // Apply the change
            switch (change.Operation)
            {
                case SyncOperation.Create:
                case SyncOperation.Update:
                    var entity = JsonSerializer.Deserialize<object>(change.Data, JsonOptions);
                    if (entity != null)
                    {
                        await _indexedDbService.PutAsync(storeName, entity);
                    }
                    break;

                case SyncOperation.Delete:
                    await _indexedDbService.DeleteAsync(storeName, change.EntityId.ToString());
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Messages.Add($"Failed to apply change for {change.EntityType}/{change.EntityId}: {ex.Message}");
        }
    }

    private async Task SaveSyncMetadataAsync()
    {
        var metadata = new SyncMetadata
        {
            Id = "lastSync",
            LastSyncTime = _lastSyncTime ?? DateTime.UtcNow
        };

        await _indexedDbService.PutAsync(SyncMetadataStore, metadata);
    }

    private void ReportProgress(int total, int processed, string message)
    {
        OnProgressChanged?.Invoke(new SyncProgress
        {
            TotalItems = total,
            ProcessedItems = processed,
            Message = message
        });
    }

    #endregion

    #region Helper Classes

    private class SyncMetadata
    {
        public string Id { get; set; } = string.Empty;
        public DateTime LastSyncTime { get; set; }
    }

    private class SyncBatchRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public List<object> Items { get; set; } = new();
    }

    private class SyncBatchResponse
    {
        public bool Success { get; set; }
        public int ItemsSynced { get; set; }
        public List<SyncConflict>? Conflicts { get; set; }
    }

    private class ServerChangesResponse
    {
        public List<ServerChange> Changes { get; set; } = new();
    }

    private class ServerChange
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public SyncOperation Operation { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
    }

    #endregion
}
