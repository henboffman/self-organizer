namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for synchronizing local IndexedDB data with remote SQL Server database.
/// Handles seamless data migration and ongoing sync for multi-device support.
/// </summary>
public interface IDbSyncService
{
    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    SyncStatus Status { get; }

    /// <summary>
    /// Event raised when sync status changes.
    /// </summary>
    event Action<SyncStatus>? OnStatusChanged;

    /// <summary>
    /// Event raised when sync progress updates.
    /// </summary>
    event Action<SyncProgress>? OnProgressChanged;

    /// <summary>
    /// Checks if there is data in IndexedDB that needs to be synced.
    /// </summary>
    Task<bool> HasLocalDataAsync();

    /// <summary>
    /// Checks if sync is available (user is authenticated and server is reachable).
    /// </summary>
    Task<bool> IsSyncAvailableAsync();

    /// <summary>
    /// Performs initial sync - uploads all local data to server.
    /// Call this when user first authenticates.
    /// </summary>
    Task<SyncResult> PerformInitialSyncAsync();

    /// <summary>
    /// Performs incremental sync - syncs changes since last sync.
    /// </summary>
    Task<SyncResult> SyncChangesAsync();

    /// <summary>
    /// Pushes a specific entity change to the server.
    /// </summary>
    Task<SyncResult> PushEntityChangeAsync(string entityType, Guid entityId, SyncOperation operation);

    /// <summary>
    /// Pulls latest changes from server.
    /// </summary>
    Task<SyncResult> PullChangesAsync(DateTime? since = null);

    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    Task<SyncResult> ResolveConflictAsync(SyncConflict conflict, ConflictResolution resolution);

    /// <summary>
    /// Gets pending changes that haven't been synced.
    /// </summary>
    Task<IEnumerable<PendingChange>> GetPendingChangesAsync();

    /// <summary>
    /// Gets any conflicts that need user resolution.
    /// </summary>
    Task<IEnumerable<SyncConflict>> GetConflictsAsync();

    /// <summary>
    /// Queues an operation for sync when connectivity is restored.
    /// </summary>
    Task QueueOfflineOperationAsync(string entityType, Guid entityId, SyncOperation operation, object? data);

    /// <summary>
    /// Processes queued offline operations.
    /// </summary>
    Task<SyncResult> ProcessOfflineQueueAsync();

    /// <summary>
    /// Gets the last successful sync time.
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync();
}

/// <summary>
/// Current sync status.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Not connected, operating in offline mode.
    /// </summary>
    Offline,

    /// <summary>
    /// Checking for connectivity and auth status.
    /// </summary>
    Checking,

    /// <summary>
    /// Connected and ready to sync.
    /// </summary>
    Connected,

    /// <summary>
    /// Currently syncing data.
    /// </summary>
    Syncing,

    /// <summary>
    /// All data is synced.
    /// </summary>
    Synced,

    /// <summary>
    /// Sync failed with error.
    /// </summary>
    Error,

    /// <summary>
    /// There are conflicts that need resolution.
    /// </summary>
    HasConflicts,

    /// <summary>
    /// Changes are pending upload.
    /// </summary>
    PendingChanges
}

/// <summary>
/// Type of sync operation.
/// </summary>
public enum SyncOperation
{
    Create,
    Update,
    Delete
}

/// <summary>
/// How to resolve a sync conflict.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Keep the local version.
    /// </summary>
    KeepLocal,

    /// <summary>
    /// Keep the server version.
    /// </summary>
    KeepServer,

    /// <summary>
    /// Merge both versions (where possible).
    /// </summary>
    Merge
}

/// <summary>
/// Progress of a sync operation.
/// </summary>
public class SyncProgress
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public string CurrentEntity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double PercentComplete => TotalItems > 0 ? (ProcessedItems * 100.0) / TotalItems : 0;
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int ItemsSynced { get; set; }
    public int Conflicts { get; set; }
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// A change pending sync.
/// </summary>
public class PendingChange
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncOperation Operation { get; set; }
    public DateTime QueuedAt { get; set; }
    public string? Data { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// A sync conflict requiring resolution.
/// </summary>
public class SyncConflict
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string LocalVersion { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
    public DateTime DetectedAt { get; set; }
}
