using System.Text.Json;
using SelfOrganizer.App.Services.Data;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public interface IDataSyncService
{
    // Configuration management
    Task<List<DataSourceConfiguration>> GetAllConfigurationsAsync();
    Task<DataSourceConfiguration?> GetConfigurationAsync(Guid id);
    Task<DataSourceConfiguration?> GetConfigurationByTypeAsync(DataSourceType type);
    Task<DataSourceConfiguration> SaveConfigurationAsync(DataSourceConfiguration config);
    Task DeleteConfigurationAsync(Guid id);

    // Sync job management
    Task<List<SyncJob>> GetSyncJobsAsync(int limit = 50);
    Task<List<SyncJob>> GetSyncJobsForSourceAsync(DataSourceType type, int limit = 20);
    Task<SyncJob?> GetSyncJobAsync(Guid id);
    Task<SyncJob> CreateSyncJobAsync(DataSourceType type, SyncOperationType operation);
    Task<SyncJob> UpdateSyncJobAsync(SyncJob job);

    // Task interchange operations
    Task<string> ExportTasksAsync(ExportTaskOptions? options = null);
    Task<ImportResult> ImportTasksAsync(string jsonContent, ImportOptions? options = null);
    Task<ImportResult> PreviewImportAsync(string jsonContent);

    // Events for UI updates
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
}

public class ExportTaskOptions
{
    public bool IncludeCompleted { get; set; } = false;
    public bool IncludeProjects { get; set; } = true;
    public Guid? ProjectId { get; set; }
    public List<TodoTaskStatus>? Statuses { get; set; }
}

public class SyncProgressEventArgs : EventArgs
{
    public Guid JobId { get; set; }
    public string Phase { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public int ProcessedItems { get; set; }
    public int TotalItems { get; set; }
}

public class DataSyncService : IDataSyncService
{
    private readonly IIndexedDbService _dbService;
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    public DataSyncService(
        IIndexedDbService dbService,
        ITaskService taskService,
        IProjectService projectService)
    {
        _dbService = dbService;
        _taskService = taskService;
        _projectService = projectService;
    }

    #region Configuration Management

    public async Task<List<DataSourceConfiguration>> GetAllConfigurationsAsync()
    {
        return await _dbService.GetAllAsync<DataSourceConfiguration>(StoreNames.DataSourceConfigs);
    }

    public async Task<DataSourceConfiguration?> GetConfigurationAsync(Guid id)
    {
        return await _dbService.GetAsync<DataSourceConfiguration>(StoreNames.DataSourceConfigs, id.ToString());
    }

    public async Task<DataSourceConfiguration?> GetConfigurationByTypeAsync(DataSourceType type)
    {
        var all = await GetAllConfigurationsAsync();
        return all.FirstOrDefault(c => c.SourceType == type);
    }

    public async Task<DataSourceConfiguration> SaveConfigurationAsync(DataSourceConfiguration config)
    {
        config.ModifiedAt = DateTime.UtcNow;
        return await _dbService.PutAsync(StoreNames.DataSourceConfigs, config);
    }

    public async Task DeleteConfigurationAsync(Guid id)
    {
        await _dbService.DeleteAsync(StoreNames.DataSourceConfigs, id.ToString());
    }

    #endregion

    #region Sync Job Management

    public async Task<List<SyncJob>> GetSyncJobsAsync(int limit = 50)
    {
        var all = await _dbService.GetAllAsync<SyncJob>(StoreNames.SyncJobs);
        return all.OrderByDescending(j => j.CreatedAt).Take(limit).ToList();
    }

    public async Task<List<SyncJob>> GetSyncJobsForSourceAsync(DataSourceType type, int limit = 20)
    {
        var all = await _dbService.GetAllAsync<SyncJob>(StoreNames.SyncJobs);
        return all.Where(j => j.SourceType == type)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<SyncJob?> GetSyncJobAsync(Guid id)
    {
        return await _dbService.GetAsync<SyncJob>(StoreNames.SyncJobs, id.ToString());
    }

    public async Task<SyncJob> CreateSyncJobAsync(DataSourceType type, SyncOperationType operation)
    {
        var job = new SyncJob
        {
            SourceType = type,
            OperationType = operation,
            Status = SyncJobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        return await _dbService.AddAsync(StoreNames.SyncJobs, job);
    }

    public async Task<SyncJob> UpdateSyncJobAsync(SyncJob job)
    {
        job.ModifiedAt = DateTime.UtcNow;
        return await _dbService.PutAsync(StoreNames.SyncJobs, job);
    }

    #endregion

    #region Task Interchange Operations

    public async Task<string> ExportTasksAsync(ExportTaskOptions? options = null)
    {
        options ??= new ExportTaskOptions();

        var tasks = await _taskService.GetAllAsync();
        var filteredTasks = tasks.Where(t =>
        {
            if (!options.IncludeCompleted && t.Status == TodoTaskStatus.Completed)
                return false;
            if (options.ProjectId.HasValue && t.ProjectId != options.ProjectId)
                return false;
            if (options.Statuses != null && !options.Statuses.Contains(t.Status))
                return false;
            return true;
        }).ToList();

        var interchange = new TaskInterchangeFormat
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Source = "self-organizer",
            Tasks = filteredTasks.Select(MapTaskToInterchange).ToList()
        };

        if (options.IncludeProjects)
        {
            var projects = await _projectService.GetAllAsync();
            interchange.Projects = projects.Select(MapProjectToInterchange).ToList();
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(interchange, jsonOptions);
    }

    public async Task<ImportResult> PreviewImportAsync(string jsonContent)
    {
        var options = new ImportOptions { PreviewOnly = true };
        return await ImportTasksAsync(jsonContent, options);
    }

    public async Task<ImportResult> ImportTasksAsync(string jsonContent, ImportOptions? options = null)
    {
        options ??= new ImportOptions();
        var result = new ImportResult { Success = true };

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            TaskInterchangeFormat? interchange;
            try
            {
                interchange = JsonSerializer.Deserialize<TaskInterchangeFormat>(jsonContent, jsonOptions);
            }
            catch (JsonException ex)
            {
                result.Success = false;
                result.Errors.Add($"Invalid JSON format: {ex.Message}");
                return result;
            }

            if (interchange == null || (interchange.Tasks.Count == 0 && interchange.Projects.Count == 0))
            {
                result.Success = false;
                result.Errors.Add("No tasks or projects found in the import file.");
                return result;
            }

            // Get existing data for duplicate detection
            var existingTasks = await _taskService.GetAllAsync();
            var existingProjects = await _projectService.GetAllAsync();

            // Process projects first (if any)
            var projectMap = new Dictionary<string, Guid>(); // ExternalId -> InternalId
            foreach (var projectData in interchange.Projects)
            {
                result.TotalProcessed++;
                var itemResult = new ImportedItemResult
                {
                    ExternalId = projectData.ExternalId,
                    Title = projectData.Name
                };

                // Check for duplicates
                Project? existing = null;
                if (options.MatchByExternalId && !string.IsNullOrEmpty(projectData.ExternalId))
                {
                    existing = existingProjects.FirstOrDefault(p =>
                        p.Id.ToString() == projectData.ExternalId ||
                        (p.Id == projectData.InternalId));
                }

                if (existing != null)
                {
                    if (options.DuplicateStrategy == "Skip")
                    {
                        itemResult.Action = "Skipped";
                        itemResult.InternalId = existing.Id;
                        result.Skipped++;
                    }
                    else if (options.DuplicateStrategy == "Update" && !options.PreviewOnly)
                    {
                        UpdateProjectFromInterchange(existing, projectData);
                        await _projectService.UpdateAsync(existing);
                        itemResult.Action = "Updated";
                        itemResult.InternalId = existing.Id;
                        result.Updated++;
                    }
                    projectMap[projectData.ExternalId ?? projectData.Name] = existing.Id;
                }
                else if (!options.PreviewOnly)
                {
                    var newProject = CreateProjectFromInterchange(projectData);
                    await _projectService.CreateAsync(newProject);
                    itemResult.Action = "Created";
                    itemResult.InternalId = newProject.Id;
                    result.Created++;
                    projectMap[projectData.ExternalId ?? projectData.Name] = newProject.Id;
                }
                else
                {
                    itemResult.Action = "Would Create";
                    result.Created++;
                }

                result.Items.Add(itemResult);
            }

            // Process tasks
            foreach (var taskData in interchange.Tasks)
            {
                result.TotalProcessed++;
                var itemResult = new ImportedItemResult
                {
                    ExternalId = taskData.ExternalId,
                    Title = taskData.Title
                };

                try
                {
                    // Check for duplicates
                    TodoTask? existing = null;
                    if (options.MatchByExternalId && !string.IsNullOrEmpty(taskData.ExternalId))
                    {
                        existing = existingTasks.FirstOrDefault(t =>
                            t.Id.ToString() == taskData.ExternalId ||
                            (t.Id == taskData.InternalId));
                    }
                    else if (options.MatchByTitleAndDate && taskData.DueDate.HasValue)
                    {
                        existing = existingTasks.FirstOrDefault(t =>
                            t.Title.Equals(taskData.Title, StringComparison.OrdinalIgnoreCase) &&
                            t.DueDate?.Date == taskData.DueDate.Value.Date);
                    }

                    if (existing != null)
                    {
                        if (options.DuplicateStrategy == "Skip")
                        {
                            itemResult.Action = "Skipped";
                            itemResult.InternalId = existing.Id;
                            result.Skipped++;
                        }
                        else if (options.DuplicateStrategy == "Update" && !options.PreviewOnly)
                        {
                            UpdateTaskFromInterchange(existing, taskData, projectMap, options);
                            await _taskService.UpdateAsync(existing);
                            itemResult.Action = "Updated";
                            itemResult.InternalId = existing.Id;
                            result.Updated++;
                        }
                    }
                    else if (!options.PreviewOnly)
                    {
                        var newTask = CreateTaskFromInterchange(taskData, projectMap, options);
                        await _taskService.CreateAsync(newTask);
                        itemResult.Action = "Created";
                        itemResult.InternalId = newTask.Id;
                        result.Created++;
                    }
                    else
                    {
                        itemResult.Action = "Would Create";
                        result.Created++;
                    }
                }
                catch (Exception ex)
                {
                    itemResult.Action = "Failed";
                    itemResult.Error = ex.Message;
                    result.Failed++;
                    result.Errors.Add($"Task '{taskData.Title}': {ex.Message}");
                }

                result.Items.Add(itemResult);
            }

            result.Success = result.Failed == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region Mapping Helpers

    private static TaskInterchangeItem MapTaskToInterchange(TodoTask task)
    {
        return new TaskInterchangeItem
        {
            InternalId = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = MapStatusToString(task.Status),
            Priority = task.Priority,
            DueDate = task.DueDate,
            ScheduledDate = task.ScheduledDate,
            CompletedAt = task.CompletedAt,
            Category = task.Category,
            Tags = task.Tags?.ToList() ?? new List<string>(),
            Contexts = task.Contexts?.ToList() ?? new List<string>(),
            EstimatedMinutes = task.EstimatedMinutes,
            EnergyLevel = task.EnergyLevel,
            Project = task.ProjectId.HasValue
                ? new ProjectReference { InternalId = task.ProjectId }
                : null
        };
    }

    private static string MapStatusToString(TodoTaskStatus status)
    {
        return status switch
        {
            TodoTaskStatus.Completed => "Completed",
            TodoTaskStatus.Deleted => "Deleted",
            TodoTaskStatus.SomedayMaybe => "OnHold",
            _ => "Active"
        };
    }

    private static TodoTaskStatus MapStringToStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "completed" or "done" => TodoTaskStatus.Completed,
            "deleted" or "cancelled" => TodoTaskStatus.Deleted,
            "onhold" or "hold" or "someday" => TodoTaskStatus.SomedayMaybe,
            "waiting" or "waitingfor" => TodoTaskStatus.WaitingFor,
            "scheduled" => TodoTaskStatus.Scheduled,
            _ => TodoTaskStatus.Inbox
        };
    }

    private static ProjectInterchangeItem MapProjectToInterchange(Project project)
    {
        return new ProjectInterchangeItem
        {
            InternalId = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            Color = project.Color,
            DueDate = project.DueDate
        };
    }

    private static TodoTask CreateTaskFromInterchange(
        TaskInterchangeItem data,
        Dictionary<string, Guid> projectMap,
        ImportOptions options)
    {
        var task = new TodoTask
        {
            Title = data.Title,
            Description = data.Description,
            Priority = data.Priority,
            DueDate = data.DueDate,
            ScheduledDate = data.ScheduledDate,
            CompletedAt = data.CompletedAt,
            Category = data.Category,
            Tags = data.Tags ?? new List<string>(),
            Contexts = data.Contexts ?? new List<string>(),
            EstimatedMinutes = data.EstimatedMinutes ?? 0,
            EnergyLevel = data.EnergyLevel,
            Status = MapStringToStatus(data.Status)
        };

        // Override status if configured
        if (task.Status != TodoTaskStatus.Completed)
        {
            task.Status = options.DefaultStatus;
        }

        // Add configured tags
        if (options.AddTags.Any())
        {
            task.Tags = task.Tags.Union(options.AddTags).ToList();
        }

        // Set project if specified
        if (options.DefaultProjectId.HasValue)
        {
            task.ProjectId = options.DefaultProjectId;
        }
        else if (data.Project != null)
        {
            var projectKey = data.Project.ExternalId ?? data.Project.Name;
            if (!string.IsNullOrEmpty(projectKey) && projectMap.TryGetValue(projectKey, out var projectId))
            {
                task.ProjectId = projectId;
            }
            else if (data.Project.InternalId.HasValue)
            {
                task.ProjectId = data.Project.InternalId;
            }
        }

        return task;
    }

    private static void UpdateTaskFromInterchange(
        TodoTask task,
        TaskInterchangeItem data,
        Dictionary<string, Guid> projectMap,
        ImportOptions options)
    {
        task.Title = data.Title;
        task.Description = data.Description;
        task.Priority = data.Priority;
        task.DueDate = data.DueDate;
        task.ScheduledDate = data.ScheduledDate;
        task.CompletedAt = data.CompletedAt;
        task.Category = data.Category;
        task.EstimatedMinutes = data.EstimatedMinutes ?? task.EstimatedMinutes;
        task.EnergyLevel = data.EnergyLevel ?? task.EnergyLevel;

        if (data.Tags?.Any() == true)
        {
            task.Tags = data.Tags.Union(task.Tags ?? new List<string>()).ToList();
        }

        if (data.Contexts?.Any() == true)
        {
            task.Contexts = data.Contexts.Union(task.Contexts ?? new List<string>()).ToList();
        }

        // Update project if specified in data
        if (data.Project != null)
        {
            var projectKey = data.Project.ExternalId ?? data.Project.Name;
            if (!string.IsNullOrEmpty(projectKey) && projectMap.TryGetValue(projectKey, out var projectId))
            {
                task.ProjectId = projectId;
            }
            else if (data.Project.InternalId.HasValue)
            {
                task.ProjectId = data.Project.InternalId;
            }
        }

        task.ModifiedAt = DateTime.UtcNow;
    }

    private static Project CreateProjectFromInterchange(ProjectInterchangeItem data)
    {
        return new Project
        {
            Name = data.Name,
            Description = data.Description,
            Status = Enum.TryParse<ProjectStatus>(data.Status, true, out var status)
                ? status
                : ProjectStatus.Active,
            Color = data.Color,
            DueDate = data.DueDate
        };
    }

    private static void UpdateProjectFromInterchange(Project project, ProjectInterchangeItem data)
    {
        project.Name = data.Name;
        project.Description = data.Description ?? project.Description;
        project.Color = data.Color ?? project.Color;
        project.DueDate = data.DueDate ?? project.DueDate;

        if (Enum.TryParse<ProjectStatus>(data.Status, true, out var status))
        {
            project.Status = status;
        }

        project.ModifiedAt = DateTime.UtcNow;
    }

    protected void OnSyncProgressChanged(SyncProgressEventArgs args)
    {
        SyncProgressChanged?.Invoke(this, args);
    }

    #endregion
}
