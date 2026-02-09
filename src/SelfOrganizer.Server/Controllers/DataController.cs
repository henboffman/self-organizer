using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Server.Controllers;

/// <summary>
/// Generic CRUD controller for all entity types.
/// Routes: /api/data/{entityType}
/// </summary>
[Authorize]
[ApiController]
[Route("api/data/{entityType}")]
public class DataController : ControllerBase
{
    private readonly IServiceProvider _services;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Map of entity type names to their types
    private static readonly Dictionary<string, Type> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tasks"] = typeof(TodoTask),
        ["projects"] = typeof(Project),
        ["goals"] = typeof(Goal),
        ["ideas"] = typeof(Idea),
        ["captures"] = typeof(CaptureItem),
        ["events"] = typeof(CalendarEvent),
        ["habits"] = typeof(Habit),
        ["habitlogs"] = typeof(HabitLog),
        ["references"] = typeof(ReferenceItem),
        ["contexts"] = typeof(Context),
        ["categories"] = typeof(CategoryDefinition),
        ["preferences"] = typeof(UserPreferences),
        ["dailysnapshots"] = typeof(DailySnapshot),
        ["weeklysnapshots"] = typeof(WeeklySnapshot),
        ["timeblocks"] = typeof(TimeBlock),
        ["contacts"] = typeof(Contact),
        ["entitylinkrules"] = typeof(EntityLinkRule),
        ["focussessionlogs"] = typeof(FocusSessionLog),
        ["taskremindersnoozes"] = typeof(TaskReminderSnooze),
        ["skills"] = typeof(Skill),
        ["careerplans"] = typeof(CareerPlan),
        ["growthsnapshots"] = typeof(GrowthSnapshot)
    };

    public DataController(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// GET /api/data/{entityType} - Get all entities of a type
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(string entityType)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        var result = await InvokeRepositoryMethodAsync(type, "GetAllAsync");
        return Ok(result);
    }

    /// <summary>
    /// GET /api/data/{entityType}/{id} - Get entity by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(string entityType, Guid id)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        var result = await InvokeRepositoryMethodAsync(type, "GetByIdAsync", id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    /// <summary>
    /// POST /api/data/{entityType} - Create new entity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(string entityType, [FromBody] JsonElement body)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        var entity = JsonSerializer.Deserialize(body.GetRawText(), type, JsonOptions);
        if (entity == null)
        {
            return BadRequest("Invalid entity data");
        }

        var result = await InvokeRepositoryMethodAsync(type, "AddAsync", entity);
        var baseEntity = result as BaseEntity;
        return Created($"/api/data/{entityType}/{baseEntity?.Id}", result);
    }

    /// <summary>
    /// PUT /api/data/{entityType}/{id} - Update entity
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(string entityType, Guid id, [FromBody] JsonElement body)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        var entity = JsonSerializer.Deserialize(body.GetRawText(), type, JsonOptions) as BaseEntity;
        if (entity == null)
        {
            return BadRequest("Invalid entity data");
        }

        if (entity.Id != id)
        {
            return BadRequest("Entity ID in URL must match entity ID in body");
        }

        try
        {
            var result = await InvokeRepositoryMethodAsync(type, "UpdateAsync", entity);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// DELETE /api/data/{entityType}/{id} - Delete entity
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(string entityType, Guid id)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        await InvokeRepositoryMethodAsync(type, "DeleteAsync", id);
        return NoContent();
    }

    /// <summary>
    /// GET /api/data/{entityType}/count - Get count of entities
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> Count(string entityType)
    {
        if (!TryGetEntityType(entityType, out var type))
        {
            return BadRequest($"Unknown entity type: {entityType}");
        }

        var result = await InvokeRepositoryMethodAsync(type, "CountAsync", (object?)null);
        return Ok(new { count = result });
    }

    #region Helper Methods

    private static bool TryGetEntityType(string entityTypeName, out Type type)
    {
        return EntityTypeMap.TryGetValue(entityTypeName, out type!);
    }

    private async Task<object?> InvokeRepositoryMethodAsync(Type entityType, string methodName, params object?[] args)
    {
        // Get IRepository<T> for the entity type
        var repositoryType = typeof(IRepository<>).MakeGenericType(entityType);
        var repository = _services.GetRequiredService(repositoryType);

        // Get the method
        var method = repositoryType.GetMethod(methodName);
        if (method == null)
        {
            throw new InvalidOperationException($"Method {methodName} not found on {repositoryType.Name}");
        }

        // Invoke the method
        var task = method.Invoke(repository, args) as Task;
        if (task == null)
        {
            throw new InvalidOperationException($"Method {methodName} did not return a Task");
        }

        await task;

        // Get the result if it's a Task<T>
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    #endregion
}
