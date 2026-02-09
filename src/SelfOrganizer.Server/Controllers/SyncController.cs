using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Server.Data;
using SelfOrganizer.Server.Services.Auth;

namespace SelfOrganizer.Server.Controllers;

/// <summary>
/// Controller for data synchronization between client IndexedDB and server SQL database.
/// </summary>
[Authorize]
[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly SelfOrganizerDbContext _context;
    private readonly IServerAuthService _authService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SyncController(SelfOrganizerDbContext context, IServerAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// GET /api/sync/health - Check if sync server is available
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// GET /api/sync/changes?since={timestamp} - Get all changes since a timestamp
    /// </summary>
    [HttpGet("changes")]
    public async Task<IActionResult> GetChanges([FromQuery] DateTime? since)
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var sinceTime = since ?? DateTime.MinValue;
        var changes = new List<SyncChangeDto>();

        // Query each entity type for changes
        changes.AddRange(await GetEntityChangesAsync<TodoTask>("tasks", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Project>("projects", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Goal>("goals", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Idea>("ideas", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<CaptureItem>("captures", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<CalendarEvent>("events", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Habit>("habits", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<HabitLog>("habitlogs", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<ReferenceItem>("references", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Context>("contexts", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<CategoryDefinition>("categories", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<UserPreferences>("preferences", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<DailySnapshot>("dailysnapshots", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<WeeklySnapshot>("weeklysnapshots", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<TimeBlock>("timeblocks", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<Contact>("contacts", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<EntityLinkRule>("entitylinkrules", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<FocusSessionLog>("focussessionlogs", userId, sinceTime));
        changes.AddRange(await GetEntityChangesAsync<TaskReminderSnooze>("taskremindersnoozes", userId, sinceTime));

        return Ok(new SyncChangesResponseDto
        {
            Changes = changes,
            ServerTime = DateTime.UtcNow
        });
    }

    /// <summary>
    /// POST /api/sync/batch - Batch upsert entities
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> BatchSync([FromBody] SyncBatchRequestDto request)
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conflicts = new List<SyncConflictDto>();
        var itemsSynced = 0;

        foreach (var item in request.Items)
        {
            try
            {
                var conflict = await UpsertEntityAsync(request.EntityType, item, userId);
                if (conflict != null)
                {
                    conflicts.Add(conflict);
                }
                else
                {
                    itemsSynced++;
                }
            }
            catch (Exception ex)
            {
                conflicts.Add(new SyncConflictDto
                {
                    EntityType = request.EntityType,
                    EntityId = TryGetEntityId(item),
                    Error = ex.Message
                });
            }
        }

        return Ok(new SyncBatchResponseDto
        {
            Success = conflicts.Count == 0,
            ItemsSynced = itemsSynced,
            Conflicts = conflicts
        });
    }

    /// <summary>
    /// POST /api/sync/conflicts/resolve - Resolve a sync conflict
    /// </summary>
    [HttpPost("conflicts/resolve")]
    public async Task<IActionResult> ResolveConflict([FromBody] ResolveConflictRequestDto request)
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            switch (request.Resolution)
            {
                case "keep_local":
                    // Client version wins - upsert the local data
                    if (request.LocalData.HasValue)
                    {
                        await UpsertEntityAsync(request.EntityType, request.LocalData.Value, userId);
                    }
                    break;

                case "keep_server":
                    // Server version wins - nothing to do, client will pull server version
                    break;

                case "merge":
                    // For merge, client sends merged data
                    if (request.MergedData.HasValue)
                    {
                        await UpsertEntityAsync(request.EntityType, request.MergedData.Value, userId);
                    }
                    break;

                default:
                    return BadRequest($"Unknown resolution: {request.Resolution}");
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    #region Helper Methods

    private async Task<List<SyncChangeDto>> GetEntityChangesAsync<T>(string entityType, string userId, DateTime since)
        where T : BaseEntity
    {
        var entities = await _context.Set<T>()
            .Where(e => EF.Property<string>(e, "UserId") == userId && e.ModifiedAt > since)
            .ToListAsync();

        return entities.Select(e => new SyncChangeDto
        {
            EntityType = entityType,
            EntityId = e.Id,
            Operation = SyncOperation.Update, // For simplicity, treat all as updates
            Data = JsonSerializer.Serialize(e, JsonOptions),
            ModifiedAt = e.ModifiedAt
        }).ToList();
    }

    private async Task<SyncConflictDto?> UpsertEntityAsync(string entityType, JsonElement data, string userId)
    {
        var entityId = TryGetEntityId(data);
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity must have an Id");
        }

        // Get the entity type
        var type = entityType.ToLowerInvariant() switch
        {
            "tasks" => typeof(TodoTask),
            "projects" => typeof(Project),
            "goals" => typeof(Goal),
            "ideas" => typeof(Idea),
            "captures" => typeof(CaptureItem),
            "events" => typeof(CalendarEvent),
            "habits" => typeof(Habit),
            "habitlogs" => typeof(HabitLog),
            "references" => typeof(ReferenceItem),
            "contexts" => typeof(Context),
            "categories" => typeof(CategoryDefinition),
            "preferences" => typeof(UserPreferences),
            "dailysnapshots" => typeof(DailySnapshot),
            "weeklysnapshots" => typeof(WeeklySnapshot),
            "timeblocks" => typeof(TimeBlock),
            "contacts" => typeof(Contact),
            "entitylinkrules" => typeof(EntityLinkRule),
            "focussessionlogs" => typeof(FocusSessionLog),
            "taskremindersnoozes" => typeof(TaskReminderSnooze),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };

        var entity = JsonSerializer.Deserialize(data.GetRawText(), type, JsonOptions) as BaseEntity;
        if (entity == null)
        {
            throw new ArgumentException("Invalid entity data");
        }

        // Check for existing entity
        var existingEntity = await _context.FindAsync(type, entityId);

        if (existingEntity != null)
        {
            var existing = existingEntity as BaseEntity;

            // Check for conflict (if local modification is older than server)
            if (existing != null && entity.ModifiedAt < existing.ModifiedAt)
            {
                return new SyncConflictDto
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    LocalModifiedAt = entity.ModifiedAt,
                    ServerModifiedAt = existing.ModifiedAt,
                    ServerVersion = JsonSerializer.Serialize(existing, type, JsonOptions)
                };
            }

            // Update existing
            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            _context.Entry(existingEntity).Property("UserId").CurrentValue = userId;
        }
        else
        {
            // Add new
            entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
            entity.ModifiedAt = DateTime.UtcNow;

            var entry = _context.Add(entity);
            entry.Property("UserId").CurrentValue = userId;
        }

        await _context.SaveChangesAsync();
        return null;
    }

    private static Guid TryGetEntityId(JsonElement data)
    {
        if (data.TryGetProperty("id", out var idProperty) ||
            data.TryGetProperty("Id", out idProperty))
        {
            if (idProperty.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idProperty.GetString(), out var id))
            {
                return id;
            }
        }
        return Guid.Empty;
    }

    #endregion
}

#region DTOs

public class SyncChangeDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncOperation Operation { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
}

public class SyncChangesResponseDto
{
    public List<SyncChangeDto> Changes { get; set; } = new();
    public DateTime ServerTime { get; set; }
}

public class SyncBatchRequestDto
{
    public string EntityType { get; set; } = string.Empty;
    public List<JsonElement> Items { get; set; } = new();
}

public class SyncBatchResponseDto
{
    public bool Success { get; set; }
    public int ItemsSynced { get; set; }
    public List<SyncConflictDto> Conflicts { get; set; } = new();
}

public class SyncConflictDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime LocalModifiedAt { get; set; }
    public DateTime ServerModifiedAt { get; set; }
    public string? ServerVersion { get; set; }
    public string? Error { get; set; }
}

public class ResolveConflictRequestDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Resolution { get; set; } = string.Empty; // "keep_local", "keep_server", "merge"
    public JsonElement? LocalData { get; set; }
    public JsonElement? MergedData { get; set; }
}

#endregion
