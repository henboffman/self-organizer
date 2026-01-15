namespace SelfOrganizer.Core.Models;

/// <summary>
/// Available data sources for synchronization
/// </summary>
public enum DataSourceType
{
    /// <summary>Microsoft Graph API (Calendar, Tasks via Outlook)</summary>
    MicrosoftGraph,
    /// <summary>Import/Export from lifecycle app or other systems</summary>
    LifecycleTasks,
    /// <summary>Google Calendar API (future)</summary>
    GoogleCalendar,
    /// <summary>Generic file import (CSV, JSON)</summary>
    FileImport
}

/// <summary>
/// Status of a sync job execution
/// </summary>
public enum SyncJobStatus
{
    Pending,
    Running,
    Completed,
    CompletedWithWarnings,
    Failed,
    Cancelled
}

/// <summary>
/// Type of sync operation
/// </summary>
public enum SyncOperationType
{
    /// <summary>Pull data from external source</summary>
    Import,
    /// <summary>Push data to external source</summary>
    Export,
    /// <summary>Two-way synchronization</summary>
    Bidirectional
}

/// <summary>
/// Configuration for a data source
/// </summary>
public class DataSourceConfiguration : BaseEntity
{
    /// <summary>Type of data source</summary>
    public DataSourceType SourceType { get; set; }

    /// <summary>Display name for this configuration</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this source is enabled for automatic sync</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Connection status</summary>
    public bool IsConnected { get; set; }

    /// <summary>Account identifier (email, username, etc.)</summary>
    public string? AccountIdentifier { get; set; }

    /// <summary>Last successful sync time</summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>Number of records in the last sync</summary>
    public int LastSyncRecordCount { get; set; }

    /// <summary>Auto-sync interval in minutes (0 = manual only)</summary>
    public int SyncIntervalMinutes { get; set; } = 0;

    /// <summary>Source-specific settings as JSON</summary>
    public string? SettingsJson { get; set; }
}

/// <summary>
/// Microsoft Graph specific settings
/// </summary>
public class MicrosoftGraphSettings
{
    /// <summary>Calendar IDs to sync</summary>
    public List<string> CalendarIds { get; set; } = new();

    /// <summary>Days in the past to sync</summary>
    public int PastDays { get; set; } = 7;

    /// <summary>Days in the future to sync</summary>
    public int FutureDays { get; set; } = 30;

    /// <summary>Categories to include (empty = all)</summary>
    public List<string> IncludeCategories { get; set; } = new();

    /// <summary>Categories to exclude</summary>
    public List<string> ExcludeCategories { get; set; } = new();

    /// <summary>Auto-create prep blocks for meetings</summary>
    public bool AutoCreatePrepBlocks { get; set; } = true;

    /// <summary>Auto-create decompress blocks after meetings</summary>
    public bool AutoCreateDecompressBlocks { get; set; } = true;

    /// <summary>Access token (stored securely)</summary>
    public string? AccessToken { get; set; }

    /// <summary>Refresh token (stored securely)</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Token expiration time</summary>
    public DateTime? TokenExpiresAt { get; set; }
}

/// <summary>
/// Record of a sync job execution
/// </summary>
public class SyncJob : BaseEntity
{
    /// <summary>Data source type for this job</summary>
    public DataSourceType SourceType { get; set; }

    /// <summary>Configuration ID used for this job</summary>
    public Guid? ConfigurationId { get; set; }

    /// <summary>Type of sync operation</summary>
    public SyncOperationType OperationType { get; set; }

    /// <summary>Current status</summary>
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;

    /// <summary>When the job started executing</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When the job completed</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Current phase description</summary>
    public string? CurrentPhase { get; set; }

    /// <summary>Progress percentage (0-100)</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Total items to process</summary>
    public int TotalItems { get; set; }

    /// <summary>Items processed so far</summary>
    public int ProcessedItems { get; set; }

    /// <summary>Items created</summary>
    public int CreatedCount { get; set; }

    /// <summary>Items updated</summary>
    public int UpdatedCount { get; set; }

    /// <summary>Items skipped (duplicates, filtered)</summary>
    public int SkippedCount { get; set; }

    /// <summary>Items that failed</summary>
    public int ErrorCount { get; set; }

    /// <summary>Error messages</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Warning messages</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>Detailed log entries</summary>
    public List<SyncLogEntry> LogEntries { get; set; } = new();
}

/// <summary>
/// A log entry for sync job details
/// </summary>
public class SyncLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Info"; // Info, Warning, Error
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

/// <summary>
/// Common interchange format for task import/export
/// </summary>
public class TaskInterchangeFormat
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "self-organizer";
    public List<TaskInterchangeItem> Tasks { get; set; } = new();
    public List<ProjectInterchangeItem> Projects { get; set; } = new();
}

/// <summary>
/// Task item in interchange format
/// </summary>
public class TaskInterchangeItem
{
    /// <summary>External ID from source system</summary>
    public string? ExternalId { get; set; }

    /// <summary>Internal ID (for updates)</summary>
    public Guid? InternalId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Status mapped to common values: Active, Completed, Deleted, OnHold</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Priority 1-3 (1=high, 3=low)</summary>
    public int Priority { get; set; } = 2;

    public DateTime? DueDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Contexts { get; set; } = new();

    public int? EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }

    /// <summary>Energy level: 1-5</summary>
    public int? EnergyLevel { get; set; }

    /// <summary>Assignee email or identifier</summary>
    public string? Assignee { get; set; }

    /// <summary>Project reference</summary>
    public ProjectReference? Project { get; set; }

    /// <summary>Source-specific metadata</summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Project reference in interchange format
/// </summary>
public class ProjectReference
{
    public string? ExternalId { get; set; }
    public Guid? InternalId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Project item in interchange format
/// </summary>
public class ProjectInterchangeItem
{
    public string? ExternalId { get; set; }
    public Guid? InternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public string? Color { get; set; }
    public DateTime? DueDate { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Import options for handling duplicates and conflicts
/// </summary>
public class ImportOptions
{
    /// <summary>How to handle duplicates: Skip, Update, CreateNew</summary>
    public string DuplicateStrategy { get; set; } = "Skip";

    /// <summary>Match duplicates by external ID</summary>
    public bool MatchByExternalId { get; set; } = true;

    /// <summary>Match duplicates by title and date</summary>
    public bool MatchByTitleAndDate { get; set; } = false;

    /// <summary>Default status for imported tasks</summary>
    public TodoTaskStatus DefaultStatus { get; set; } = TodoTaskStatus.Inbox;

    /// <summary>Default project to assign (if any)</summary>
    public Guid? DefaultProjectId { get; set; }

    /// <summary>Tags to add to all imported tasks</summary>
    public List<string> AddTags { get; set; } = new();

    /// <summary>Preview mode - don't actually import</summary>
    public bool PreviewOnly { get; set; } = false;
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<ImportedItemResult> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result for a single imported item
/// </summary>
public class ImportedItemResult
{
    public string? ExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Created, Updated, Skipped, Failed
    public Guid? InternalId { get; set; }
    public string? Error { get; set; }
}
