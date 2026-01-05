using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Scores and optimizes task ordering based on multiple weighted factors.
/// Uses the AdvancedTaskOptimizer for sophisticated multi-objective optimization.
/// All weights are 0-100, normalized to 0-1 internally.
/// </summary>
public class TaskOptimizerService : ITaskOptimizerService
{
    private readonly IEntityExtractionService _entityExtraction;
    private readonly AdvancedTaskOptimizer _advancedOptimizer;

    public TaskOptimizerService(IEntityExtractionService entityExtraction)
    {
        _entityExtraction = entityExtraction;
        _advancedOptimizer = new AdvancedTaskOptimizer(entityExtraction);
    }

    /// <summary>
    /// Calculates a priority score for a task based on user preferences and context.
    /// Higher score = higher priority for scheduling.
    /// </summary>
    public double CalculateTaskScore(TodoTask task, SchedulingContext context)
    {
        var allTasks = new List<TodoTask> { task };
        return CalculateTaskScoreInternal(task, context, allTasks);
    }

    /// <summary>
    /// Internal scoring with access to all tasks for comparative analysis.
    /// </summary>
    private double CalculateTaskScoreInternal(
        TodoTask task,
        SchedulingContext context,
        IReadOnlyList<TodoTask> allTasks)
    {
        // Build optimization context
        var optContext = BuildOptimizationContext(context, allTasks);

        // Compute multi-dimensional score vector
        var scoreVector = _advancedOptimizer.ComputeScoreVector(task, optContext, allTasks);

        // Compute final weighted score
        return _advancedOptimizer.ComputeFinalScore(scoreVector, optContext);
    }

    /// <summary>
    /// Sorts tasks by optimal execution order for a given time context.
    /// Uses advanced multi-objective optimization with Pareto analysis.
    /// </summary>
    public IReadOnlyList<ScoredTask> OptimizeTasks(
        IEnumerable<TodoTask> tasks,
        SchedulingContext context)
    {
        var taskList = tasks.ToList();

        if (!taskList.Any())
            return Array.Empty<ScoredTask>();

        // Perform dependency analysis
        var criticalPath = _advancedOptimizer.ComputeCriticalPath(taskList);
        var tasksBlockedBy = ComputeBlockingCounts(taskList);

        // Build optimization context with dependency info
        var optContext = BuildOptimizationContext(context, taskList, criticalPath, tasksBlockedBy);

        // Score all tasks
        var scoredTasks = taskList
            .Where(t => !t.IsBlocked || context.Preferences.BlockedTaskPenalty < 100)
            .Select(t =>
            {
                var vector = _advancedOptimizer.ComputeScoreVector(t, optContext, taskList);
                var finalScore = _advancedOptimizer.ComputeFinalScore(vector, optContext);
                return new ScoredTask(t, finalScore, vector);
            })
            .ToList();

        // Find Pareto frontier for top-tier tasks
        var vectors = scoredTasks.Select(st => st.ScoreVector!).ToList();
        var paretoFrontier = _advancedOptimizer.FindParetoFrontier(vectors);
        var paretoTaskIds = paretoFrontier.Select(v => v.TaskId).ToHashSet();

        // Boost Pareto-optimal tasks
        foreach (var scored in scoredTasks)
        {
            if (paretoTaskIds.Contains(scored.Task.Id))
            {
                // 15% boost for Pareto-optimal tasks
                scored.AdjustScore(scored.Score * 1.15);
            }
        }

        // Sort by final score
        return scoredTasks
            .OrderByDescending(st => st.Score)
            .ToList();
    }

    /// <summary>
    /// Optimizes tasks with dynamic context updates for sequential scheduling.
    /// Updates momentum and batching context as tasks are "scheduled".
    /// </summary>
    public IReadOnlyList<ScoredTask> OptimizeTasksSequentially(
        IEnumerable<TodoTask> tasks,
        SchedulingContext initialContext)
    {
        var taskList = tasks.ToList();
        var result = new List<ScoredTask>();
        var remaining = new HashSet<Guid>(taskList.Select(t => t.Id));

        // Build mutable context
        var recentCategories = initialContext.RecentCategories.ToList();
        var recentProjectIds = initialContext.RecentProjectIds.ToList();
        var recentTags = initialContext.RecentTags.ToList();
        var recentContexts = new List<string>();
        var recentlyCompleted = new List<Guid>();
        var currentStakeholder = initialContext.CurrentStakeholder;

        var criticalPath = _advancedOptimizer.ComputeCriticalPath(taskList);
        var tasksBlockedBy = ComputeBlockingCounts(taskList);

        while (remaining.Any())
        {
            // Build context with current state
            var context = new SchedulingContext
            {
                Preferences = initialContext.Preferences,
                TargetDate = initialContext.TargetDate,
                TargetHour = initialContext.TargetHour,
                CurrentEnergyLevel = initialContext.CurrentEnergyLevel,
                PreferredContexts = initialContext.PreferredContexts,
                RecentCategories = recentCategories,
                RecentProjectIds = recentProjectIds,
                RecentTags = recentTags,
                CurrentStakeholder = currentStakeholder
            };

            var optContext = new OptimizationContext
            {
                Preferences = initialContext.Preferences,
                TargetDate = initialContext.TargetDate,
                TargetHour = initialContext.TargetHour,
                CurrentEnergyLevel = initialContext.CurrentEnergyLevel,
                AvailableContexts = initialContext.PreferredContexts,
                RecentCategories = recentCategories,
                RecentProjectIds = recentProjectIds,
                RecentTags = recentTags,
                RecentContexts = recentContexts,
                CurrentStakeholder = currentStakeholder,
                RecentlyCompletedTaskIds = recentlyCompleted,
                CriticalPathTaskIds = criticalPath,
                TasksBlockedBy = tasksBlockedBy,
                TotalBacklogSize = taskList.Count
            };

            // Score remaining tasks
            var remainingTasks = taskList.Where(t => remaining.Contains(t.Id)).ToList();
            var scored = remainingTasks
                .Where(t => !t.IsBlocked || initialContext.Preferences.BlockedTaskPenalty < 100)
                .Select(t =>
                {
                    var vector = _advancedOptimizer.ComputeScoreVector(t, optContext, remainingTasks);
                    var finalScore = _advancedOptimizer.ComputeFinalScore(vector, optContext);
                    return new ScoredTask(t, finalScore, vector);
                })
                .OrderByDescending(st => st.Score)
                .FirstOrDefault();

            if (scored == null)
                break;

            result.Add(scored);
            remaining.Remove(scored.Task.Id);

            // Update context for next iteration (momentum tracking)
            if (!string.IsNullOrEmpty(scored.Task.Category))
            {
                recentCategories.Insert(0, scored.Task.Category);
                if (recentCategories.Count > 5) recentCategories.RemoveAt(5);
            }
            if (scored.Task.ProjectId.HasValue)
            {
                recentProjectIds.Insert(0, scored.Task.ProjectId.Value);
                if (recentProjectIds.Count > 3) recentProjectIds.RemoveAt(3);
            }
            foreach (var tag in scored.Task.Tags.Take(3))
            {
                if (!recentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    recentTags.Insert(0, tag);
                    if (recentTags.Count > 10) recentTags.RemoveAt(10);
                }
            }
            foreach (var ctx in scored.Task.Contexts.Take(2))
            {
                if (!recentContexts.Contains(ctx, StringComparer.OrdinalIgnoreCase))
                {
                    recentContexts.Insert(0, ctx);
                    if (recentContexts.Count > 5) recentContexts.RemoveAt(5);
                }
            }
            if (!string.IsNullOrEmpty(scored.Task.WhoFor))
            {
                currentStakeholder = scored.Task.WhoFor;
            }
            recentlyCompleted.Insert(0, scored.Task.Id);
            if (recentlyCompleted.Count > 10) recentlyCompleted.RemoveAt(10);
        }

        return result;
    }

    /// <summary>
    /// Groups tasks by optimal batching using k-medoids clustering.
    /// </summary>
    public IReadOnlyList<TaskBatch> BatchTasks(
        IEnumerable<TodoTask> tasks,
        UserPreferences preferences)
    {
        var taskList = tasks.ToList();

        if (!taskList.Any())
            return Array.Empty<TaskBatch>();

        // Use advanced clustering
        var clusters = _advancedOptimizer.ClusterTasks(taskList, maxClusters: 7);

        return clusters
            .Select(c => new TaskBatch
            {
                Name = c.GetClusterName(),
                BatchType = InferBatchType(c),
                Tasks = c.Tasks.ToList(),
                Cohesion = c.Cohesion
            })
            .ToList();
    }

    /// <summary>
    /// Extracts entities from task and suggests tags.
    /// </summary>
    public EntityAnalysis AnalyzeTask(TodoTask task)
    {
        var combinedText = $"{task.Title} {task.Description} {task.Notes}";
        var extraction = _entityExtraction.ExtractEntities(combinedText);

        return new EntityAnalysis
        {
            Task = task,
            Extraction = extraction,
            SuggestedTags = _entityExtraction.SuggestTags(extraction),
            DetectedAcronyms = extraction.Acronyms,
            DetectedProperNouns = extraction.ProperNouns
        };
    }

    /// <summary>
    /// Computes similarity between two tasks.
    /// </summary>
    public double ComputeTaskSimilarity(TodoTask task1, TodoTask task2)
    {
        return _advancedOptimizer.ComputeTaskSimilarity(task1, task2);
    }

    /// <summary>
    /// Gets the critical path tasks from a set of tasks.
    /// </summary>
    public IReadOnlySet<Guid> GetCriticalPathTasks(IEnumerable<TodoTask> tasks)
    {
        return _advancedOptimizer.ComputeCriticalPath(tasks.ToList());
    }

    #region Helper Methods

    private OptimizationContext BuildOptimizationContext(
        SchedulingContext context,
        IReadOnlyList<TodoTask> allTasks,
        IReadOnlySet<Guid>? criticalPath = null,
        IReadOnlyDictionary<Guid, int>? tasksBlockedBy = null)
    {
        criticalPath ??= _advancedOptimizer.ComputeCriticalPath(allTasks);
        tasksBlockedBy ??= ComputeBlockingCounts(allTasks);

        // Determine if high time pressure
        var urgentTaskCount = allTasks.Count(t =>
            t.DueDate.HasValue &&
            (t.DueDate.Value - context.TargetDate.ToDateTime(TimeOnly.MinValue)).TotalDays <= 1);
        var highTimePressure = urgentTaskCount >= 3;

        return new OptimizationContext
        {
            Preferences = context.Preferences,
            TargetDate = context.TargetDate,
            TargetHour = context.TargetHour,
            CurrentEnergyLevel = context.CurrentEnergyLevel,
            AvailableBlockMinutes = 60, // Default, would be set by scheduler
            HighTimePressure = highTimePressure,
            TotalBacklogSize = allTasks.Count,
            AvailableContexts = context.PreferredContexts,
            RecentCategories = context.RecentCategories,
            RecentProjectIds = context.RecentProjectIds,
            RecentTags = context.RecentTags,
            RecentContexts = Array.Empty<string>(),
            CurrentStakeholder = context.CurrentStakeholder,
            RecentlyCompletedTaskIds = Array.Empty<Guid>(),
            CriticalPathTaskIds = criticalPath,
            TasksBlockedBy = tasksBlockedBy
        };
    }

    private Dictionary<Guid, int> ComputeBlockingCounts(IReadOnlyList<TodoTask> tasks)
    {
        var counts = new Dictionary<Guid, int>();

        foreach (var task in tasks)
        {
            foreach (var blockerId in task.BlockedByTaskIds)
            {
                counts[blockerId] = counts.GetValueOrDefault(blockerId, 0) + 1;
            }
        }

        return counts;
    }

    private string InferBatchType(TaskCluster cluster)
    {
        var medoid = cluster.MedoidTask;

        if (medoid.ProjectId.HasValue)
            return "Project";
        if (!string.IsNullOrEmpty(medoid.Category))
            return "Category";
        if (medoid.Contexts.Any())
            return "Context";
        if (!string.IsNullOrEmpty(medoid.WhoFor))
            return "Stakeholder";
        if (medoid.Tags.Any())
            return "Tag";

        return "General";
    }

    #endregion
}

/// <summary>
/// Interface for task optimization service
/// </summary>
public interface ITaskOptimizerService
{
    double CalculateTaskScore(TodoTask task, SchedulingContext context);
    IReadOnlyList<ScoredTask> OptimizeTasks(IEnumerable<TodoTask> tasks, SchedulingContext context);
    IReadOnlyList<ScoredTask> OptimizeTasksSequentially(IEnumerable<TodoTask> tasks, SchedulingContext initialContext);
    IReadOnlyList<TaskBatch> BatchTasks(IEnumerable<TodoTask> tasks, UserPreferences preferences);
    EntityAnalysis AnalyzeTask(TodoTask task);
    double ComputeTaskSimilarity(TodoTask task1, TodoTask task2);
    IReadOnlySet<Guid> GetCriticalPathTasks(IEnumerable<TodoTask> tasks);
}

/// <summary>
/// Context for scheduling decisions
/// </summary>
public class SchedulingContext
{
    public required UserPreferences Preferences { get; init; }
    public DateOnly TargetDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    public int TargetHour { get; init; } = DateTime.Now.Hour;
    public int CurrentEnergyLevel { get; init; } = 3; // 1-5, default medium
    public IReadOnlyList<string> PreferredContexts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecentCategories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Guid> RecentProjectIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<string> RecentTags { get; init; } = Array.Empty<string>();
    public string? CurrentStakeholder { get; init; }
}

/// <summary>
/// A task with its calculated priority score and optional detailed vector
/// </summary>
public class ScoredTask
{
    public TodoTask Task { get; }
    public double Score { get; private set; }
    public TaskScoreVector? ScoreVector { get; }

    public ScoredTask(TodoTask task, double score, TaskScoreVector? scoreVector = null)
    {
        Task = task;
        Score = score;
        ScoreVector = scoreVector;
    }

    public void AdjustScore(double newScore)
    {
        Score = newScore;
    }
}

/// <summary>
/// A batch of related tasks
/// </summary>
public class TaskBatch
{
    public required string Name { get; init; }
    public required string BatchType { get; init; }
    public required IReadOnlyList<TodoTask> Tasks { get; init; }
    public double Cohesion { get; init; } = 1.0;
}

/// <summary>
/// Analysis results for a task
/// </summary>
public class EntityAnalysis
{
    public required TodoTask Task { get; init; }
    public required ExtractionResult Extraction { get; init; }
    public IReadOnlyList<string> SuggestedTags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedAcronyms { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DetectedProperNouns { get; init; } = Array.Empty<string>();
}
