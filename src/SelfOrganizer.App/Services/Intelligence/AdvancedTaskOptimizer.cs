using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Advanced multi-objective task optimization engine using mathematical modeling.
/// Implements Pareto-optimal selection, temporal decay functions, context-switching
/// cost modeling, dependency graph analysis, and intelligent batching.
/// </summary>
public class AdvancedTaskOptimizer
{
    private readonly IEntityExtractionService _entityExtraction;

    public AdvancedTaskOptimizer(IEntityExtractionService entityExtraction)
    {
        _entityExtraction = entityExtraction;
    }

    #region Core Optimization

    /// <summary>
    /// Computes a comprehensive multi-dimensional score vector for a task.
    /// Returns individual dimension scores for Pareto analysis.
    /// </summary>
    public TaskScoreVector ComputeScoreVector(
        TodoTask task,
        OptimizationContext context,
        IReadOnlyList<TodoTask> allTasks)
    {
        var vector = new TaskScoreVector { TaskId = task.Id };

        // 1. Urgency Score (temporal dimension)
        vector.UrgencyScore = ComputeUrgencyScore(task, context);

        // 2. Importance Score (value dimension)
        vector.ImportanceScore = ComputeImportanceScore(task, context);

        // 3. Effort Score (cost dimension - inverted, lower effort = higher score)
        vector.EffortScore = ComputeEffortScore(task, context);

        // 4. Context Fit Score (environmental dimension)
        vector.ContextFitScore = ComputeContextFitScore(task, context);

        // 5. Energy Alignment Score (physiological dimension)
        vector.EnergyAlignmentScore = ComputeEnergyAlignmentScore(task, context);

        // 6. Momentum Score (psychological dimension)
        vector.MomentumScore = ComputeMomentumScore(task, context, allTasks);

        // 7. Dependency Score (structural dimension)
        vector.DependencyScore = ComputeDependencyScore(task, context, allTasks);

        // 8. Staleness Penalty (time-value decay)
        vector.StalenessScore = ComputeStalenessScore(task, context);

        // 9. Opportunity Cost (what we're NOT doing)
        vector.OpportunityCostScore = ComputeOpportunityCostScore(task, context, allTasks);

        // 10. Batching Affinity (context-switching minimization)
        vector.BatchingAffinityScore = ComputeBatchingAffinityScore(task, context);

        return vector;
    }

    /// <summary>
    /// Computes final weighted score using adaptive weight synthesis.
    /// Weights are dynamically adjusted based on context.
    /// </summary>
    public double ComputeFinalScore(TaskScoreVector vector, OptimizationContext context)
    {
        var weights = ComputeAdaptiveWeights(context);

        // Weighted geometric mean for multiplicative interaction
        // Using log-space for numerical stability
        var logScore =
            weights.Urgency * Math.Log(Math.Max(0.001, vector.UrgencyScore)) +
            weights.Importance * Math.Log(Math.Max(0.001, vector.ImportanceScore)) +
            weights.Effort * Math.Log(Math.Max(0.001, vector.EffortScore)) +
            weights.ContextFit * Math.Log(Math.Max(0.001, vector.ContextFitScore)) +
            weights.EnergyAlignment * Math.Log(Math.Max(0.001, vector.EnergyAlignmentScore)) +
            weights.Momentum * Math.Log(Math.Max(0.001, vector.MomentumScore)) +
            weights.Dependency * Math.Log(Math.Max(0.001, vector.DependencyScore)) +
            weights.Staleness * Math.Log(Math.Max(0.001, vector.StalenessScore)) +
            weights.OpportunityCost * Math.Log(Math.Max(0.001, vector.OpportunityCostScore)) +
            weights.BatchingAffinity * Math.Log(Math.Max(0.001, vector.BatchingAffinityScore));

        var totalWeight =
            weights.Urgency + weights.Importance + weights.Effort +
            weights.ContextFit + weights.EnergyAlignment + weights.Momentum +
            weights.Dependency + weights.Staleness + weights.OpportunityCost +
            weights.BatchingAffinity;

        return Math.Exp(logScore / totalWeight) * 100;
    }

    #endregion

    #region Urgency Computation

    /// <summary>
    /// Computes urgency using multiple temporal decay models.
    /// Combines sigmoid, exponential, and hyperbolic decay for realistic urgency curves.
    /// </summary>
    private double ComputeUrgencyScore(TodoTask task, OptimizationContext context)
    {
        if (!task.DueDate.HasValue)
        {
            // Tasks without due dates get moderate urgency that increases with age
            var daysSinceCreation = (context.TargetDate.ToDateTime(TimeOnly.MinValue) - task.CreatedAt).TotalDays;
            return SigmoidDecay(daysSinceCreation, midpoint: 14, steepness: 0.15, inverted: true) * 0.5;
        }

        var daysUntilDue = (task.DueDate.Value - context.TargetDate.ToDateTime(TimeOnly.MinValue)).TotalDays;

        if (daysUntilDue < 0)
        {
            // Overdue: urgency increases hyperbolically
            return 1.0 + HyperbolicGrowth(Math.Abs(daysUntilDue), scale: 0.3, max: 0.5);
        }

        // Combine multiple decay functions for nuanced urgency curve
        var sigmoidUrgency = SigmoidDecay(daysUntilDue, midpoint: 3, steepness: 0.8, inverted: false);
        var exponentialUrgency = ExponentialDecay(daysUntilDue, halfLife: 7);

        // Weight sigmoid more heavily for near-term deadlines
        var blendWeight = SigmoidDecay(daysUntilDue, midpoint: 7, steepness: 0.5, inverted: false);
        return sigmoidUrgency * blendWeight + exponentialUrgency * (1 - blendWeight);
    }

    #endregion

    #region Importance Computation

    /// <summary>
    /// Computes importance using Eisenhower matrix principles with continuous scoring.
    /// Incorporates priority, stakeholder value, and project criticality.
    /// </summary>
    private double ComputeImportanceScore(TodoTask task, OptimizationContext context)
    {
        var score = 0.0;
        var maxScore = 0.0;

        // Base priority (exponential scale for stronger differentiation)
        // Priority 1 (high) = 1.0, 2 (normal) = 0.5, 3 (low) = 0.25
        var priorityScore = Math.Pow(0.5, task.Priority - 1);
        score += priorityScore * 3.0;
        maxScore += 3.0;

        // Stakeholder multiplier (work for others often has external commitments)
        if (!string.IsNullOrEmpty(task.WhoFor))
        {
            var stakeholderBoost = task.WhoFor.Equals("self", StringComparison.OrdinalIgnoreCase) ? 0.5 : 1.0;
            score += stakeholderBoost;
            maxScore += 1.0;
        }

        // Project association (tasks tied to projects often support larger goals)
        if (task.ProjectId.HasValue)
        {
            score += 0.7;
            maxScore += 1.0;

            // Check if this is on the critical path
            if (context.CriticalPathTaskIds.Contains(task.Id))
            {
                score += 1.0;
                maxScore += 1.0;
            }
        }

        // Deep work tasks often represent important cognitive work
        if (task.RequiresDeepWork)
        {
            score += 0.5;
            maxScore += 0.5;
        }

        // Tasks blocking other tasks have derived importance
        var blockingCount = context.TasksBlockedBy.GetValueOrDefault(task.Id, 0);
        if (blockingCount > 0)
        {
            score += Math.Min(1.0, blockingCount * 0.3);
            maxScore += 1.0;
        }

        return maxScore > 0 ? score / maxScore : 0.5;
    }

    #endregion

    #region Effort Computation

    /// <summary>
    /// Computes effort score (inverted - lower effort = higher score for quick wins).
    /// Uses logarithmic scaling for diminishing returns on long tasks.
    /// </summary>
    private double ComputeEffortScore(TodoTask task, OptimizationContext context)
    {
        var estimatedMinutes = task.EstimatedMinutes > 0
            ? task.EstimatedMinutes
            : context.Preferences.DefaultTaskDurationMinutes;

        // Available time in current block
        var availableMinutes = context.AvailableBlockMinutes;

        // Can't fit in available time = very low score
        if (estimatedMinutes > availableMinutes)
        {
            return 0.1 * (availableMinutes / (double)estimatedMinutes);
        }

        // Logarithmic decay: quick tasks score higher
        // 5 min = ~1.0, 15 min = ~0.85, 30 min = ~0.75, 60 min = ~0.65, 120 min = ~0.55
        var effortScore = 1.0 / (1.0 + Math.Log(1 + estimatedMinutes / 5.0) * 0.3);

        // Bonus for tasks that fit well in the available block (Tetris-like efficiency)
        var fitRatio = estimatedMinutes / (double)availableMinutes;
        if (fitRatio >= 0.8 && fitRatio <= 1.0)
        {
            effortScore *= 1.15; // 15% bonus for good fit
        }

        return Math.Min(1.0, effortScore);
    }

    #endregion

    #region Context Fit Computation

    /// <summary>
    /// Computes context fit using Jaccard similarity and hierarchical context matching.
    /// </summary>
    private double ComputeContextFitScore(TodoTask task, OptimizationContext context)
    {
        if (!task.Contexts.Any())
        {
            // No context = universal, moderate fit
            return 0.6;
        }

        if (!context.AvailableContexts.Any())
        {
            // No context constraints = everything fits
            return 0.8;
        }

        // Jaccard similarity coefficient
        var intersection = task.Contexts.Intersect(context.AvailableContexts, StringComparer.OrdinalIgnoreCase).Count();
        var union = task.Contexts.Union(context.AvailableContexts, StringComparer.OrdinalIgnoreCase).Count();

        if (union == 0) return 0.6;

        var jaccardSimilarity = intersection / (double)union;

        // Boost for exact context matches
        var exactMatchBoost = intersection > 0 ? 0.2 : 0;

        return Math.Min(1.0, jaccardSimilarity + exactMatchBoost);
    }

    #endregion

    #region Energy Alignment Computation

    /// <summary>
    /// Computes energy alignment using circadian rhythm modeling and ultradian cycles.
    /// Models the natural ~90-minute energy fluctuation cycles.
    /// </summary>
    private double ComputeEnergyAlignmentScore(TodoTask task, OptimizationContext context)
    {
        if (!task.EnergyLevel.HasValue)
        {
            // Unknown energy requirement = neutral alignment
            return 0.7;
        }

        var taskEnergy = task.EnergyLevel.Value; // 1-5
        var currentEnergy = ComputeCurrentEnergyLevel(context);

        // Gaussian alignment: perfect match = 1.0, deviation reduces score
        var energyDiff = Math.Abs(taskEnergy - currentEnergy);
        var alignment = GaussianScore(energyDiff, sigma: 1.5);

        // Penalty for attempting high-energy tasks during energy troughs
        if (taskEnergy >= 4 && currentEnergy <= 2)
        {
            alignment *= 0.5;
        }

        // Slight bonus for matching low-energy tasks to energy troughs (productive use of downtime)
        if (taskEnergy <= 2 && currentEnergy <= 2)
        {
            alignment *= 1.1;
        }

        return Math.Min(1.0, alignment);
    }

    /// <summary>
    /// Models current energy using circadian rhythm with ultradian oscillations.
    /// </summary>
    private double ComputeCurrentEnergyLevel(OptimizationContext context)
    {
        var hour = context.TargetHour;
        var prefs = context.Preferences;

        // Base circadian rhythm (bimodal with morning and afternoon peaks)
        var morningPeak = prefs.MorningEnergyPeak;
        var afternoonPeak = prefs.AfternoonEnergyPeak;

        // Gaussian peaks centered on preferred times
        var morningEnergy = 5.0 * GaussianScore(hour - morningPeak, sigma: 2.0);
        var afternoonEnergy = 4.0 * GaussianScore(hour - afternoonPeak, sigma: 2.5);

        // Post-lunch dip (12:30-14:30)
        var lunchDip = hour >= 12.5 && hour <= 14.5
            ? 1.5 * GaussianScore(hour - 13.5, sigma: 1.0)
            : 0;

        // Ultradian rhythm (~90 min cycles) as sinusoidal modulation
        var ultradianMinutes = (hour * 60) % 90;
        var ultradianModulation = 0.3 * Math.Sin(2 * Math.PI * ultradianMinutes / 90);

        // Combine components
        var baseEnergy = Math.Max(morningEnergy, afternoonEnergy) - lunchDip;
        var energy = 2.5 + baseEnergy + ultradianModulation;

        return Math.Clamp(energy, 1, 5);
    }

    #endregion

    #region Momentum Computation

    /// <summary>
    /// Computes momentum score based on task similarity to recently completed work.
    /// Leverages cognitive priming and reduced context-switching costs.
    /// </summary>
    private double ComputeMomentumScore(
        TodoTask task,
        OptimizationContext context,
        IReadOnlyList<TodoTask> allTasks)
    {
        if (!context.RecentlyCompletedTaskIds.Any())
        {
            return 0.5; // No momentum context
        }

        var recentTasks = allTasks
            .Where(t => context.RecentlyCompletedTaskIds.Contains(t.Id))
            .ToList();

        if (!recentTasks.Any())
        {
            return 0.5;
        }

        // Compute similarity to each recent task
        var similarities = recentTasks.Select(rt => ComputeTaskSimilarity(task, rt)).ToList();

        // Weighted by recency (exponential decay)
        var weightedSum = 0.0;
        var weightSum = 0.0;
        for (int i = 0; i < similarities.Count; i++)
        {
            var recencyWeight = Math.Exp(-i * 0.3); // More recent = higher weight
            weightedSum += similarities[i] * recencyWeight;
            weightSum += recencyWeight;
        }

        var momentumScore = weightSum > 0 ? weightedSum / weightSum : 0.5;

        // Boost for continuing the same project
        if (task.ProjectId.HasValue &&
            recentTasks.Any(rt => rt.ProjectId == task.ProjectId))
        {
            momentumScore = Math.Min(1.0, momentumScore * 1.3);
        }

        return momentumScore;
    }

    #endregion

    #region Dependency Computation

    /// <summary>
    /// Computes dependency score using topological analysis.
    /// Tasks that unblock others or are on the critical path score higher.
    /// </summary>
    private double ComputeDependencyScore(
        TodoTask task,
        OptimizationContext context,
        IReadOnlyList<TodoTask> allTasks)
    {
        // Blocked tasks get very low scores
        if (task.IsBlocked)
        {
            var blockingTasks = allTasks.Where(t => task.BlockedByTaskIds.Contains(t.Id)).ToList();
            var anyBlockerCompleted = !blockingTasks.Any(); // All blockers might be completed

            if (!anyBlockerCompleted)
            {
                // Penalty proportional to blocking severity
                var blockingPenalty = context.Preferences.BlockedTaskPenalty / 100.0;
                return Math.Max(0.01, 1.0 - blockingPenalty);
            }
        }

        var score = 0.7; // Base score for unblocked tasks

        // Bonus for tasks that unblock others
        var unblocksCount = context.TasksBlockedBy.GetValueOrDefault(task.Id, 0);
        if (unblocksCount > 0)
        {
            // Logarithmic scaling for diminishing returns
            score += Math.Min(0.3, Math.Log(1 + unblocksCount) * 0.15);
        }

        // Critical path bonus
        if (context.CriticalPathTaskIds.Contains(task.Id))
        {
            score *= 1.2;
        }

        // Subtasks: completing parent task prerequisites
        if (task.ParentTaskId.HasValue)
        {
            var parentTask = allTasks.FirstOrDefault(t => t.Id == task.ParentTaskId);
            if (parentTask != null && parentTask.Priority == 1)
            {
                score *= 1.1; // Boost for high-priority parent
            }
        }

        return Math.Min(1.0, score);
    }

    #endregion

    #region Staleness Computation

    /// <summary>
    /// Computes staleness penalty using time-value decay.
    /// Old tasks that haven't been touched lose relevance over time.
    /// </summary>
    private double ComputeStalenessScore(TodoTask task, OptimizationContext context)
    {
        var daysSinceCreation = (context.TargetDate.ToDateTime(TimeOnly.MinValue) - task.CreatedAt).TotalDays;

        // Very new tasks get full score
        if (daysSinceCreation < 1)
        {
            return 1.0;
        }

        // Freshness decay using hyperbolic-tangent blend
        // Tasks stay fresh for ~7 days, then decay accelerates
        var freshnessHalfLife = 14.0; // Days until 50% freshness
        var freshnessScore = 1.0 / (1.0 + Math.Pow(daysSinceCreation / freshnessHalfLife, 1.5));

        // BUT: very old tasks might need attention (inbox zero principle)
        // After 30+ days, slight urgency boost to prevent infinite backlog
        if (daysSinceCreation > 30)
        {
            var neglectUrgency = SigmoidDecay(daysSinceCreation - 30, midpoint: 30, steepness: 0.1, inverted: true);
            freshnessScore = Math.Max(freshnessScore, neglectUrgency * 0.4);
        }

        return freshnessScore;
    }

    #endregion

    #region Opportunity Cost Computation

    /// <summary>
    /// Computes opportunity cost - what we're giving up by doing this task.
    /// Tasks with high opportunity cost should be done when their benefit is highest.
    /// </summary>
    private double ComputeOpportunityCostScore(
        TodoTask task,
        OptimizationContext context,
        IReadOnlyList<TodoTask> allTasks)
    {
        // If this is the only task, no opportunity cost
        if (allTasks.Count <= 1)
        {
            return 1.0;
        }

        // Estimate value of this task
        var thisTaskValue = EstimateTaskValue(task, context);

        // Estimate average value of alternatives
        var otherTasks = allTasks.Where(t => t.Id != task.Id).ToList();
        var avgAlternativeValue = otherTasks.Average(t => EstimateTaskValue(t, context));
        var maxAlternativeValue = otherTasks.Max(t => EstimateTaskValue(t, context));

        // Opportunity cost ratio: high value tasks relative to alternatives
        if (maxAlternativeValue <= 0)
        {
            return 1.0;
        }

        // Score is higher when this task's value exceeds alternatives
        var relativeValue = thisTaskValue / maxAlternativeValue;
        var avgRelativeValue = avgAlternativeValue > 0 ? thisTaskValue / avgAlternativeValue : 1.0;

        // Combine with weighted average (max comparison matters more)
        return Math.Min(1.0, relativeValue * 0.7 + Math.Min(1.0, avgRelativeValue) * 0.3);
    }

    private double EstimateTaskValue(TodoTask task, OptimizationContext context)
    {
        // Quick heuristic value estimation
        var value = 0.0;

        // Priority contribution
        value += (4 - task.Priority) * 2.0;

        // Urgency contribution
        if (task.DueDate.HasValue)
        {
            var daysUntilDue = (task.DueDate.Value - context.TargetDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (daysUntilDue <= 0) value += 5.0; // Overdue
            else if (daysUntilDue <= 1) value += 3.0;
            else if (daysUntilDue <= 3) value += 2.0;
            else if (daysUntilDue <= 7) value += 1.0;
        }

        // Blocking contribution
        value += context.TasksBlockedBy.GetValueOrDefault(task.Id, 0) * 1.5;

        // Project contribution
        if (task.ProjectId.HasValue) value += 1.0;

        // Stakeholder contribution
        if (!string.IsNullOrEmpty(task.WhoFor)) value += 0.5;

        return value;
    }

    #endregion

    #region Batching Affinity Computation

    /// <summary>
    /// Computes batching affinity - how well this task groups with recent/planned work.
    /// Minimizes cognitive context-switching costs.
    /// </summary>
    private double ComputeBatchingAffinityScore(TodoTask task, OptimizationContext context)
    {
        var affinityScore = 0.5; // Neutral baseline

        // Category batching
        if (!string.IsNullOrEmpty(task.Category) &&
            context.RecentCategories.Contains(task.Category, StringComparer.OrdinalIgnoreCase))
        {
            affinityScore += 0.2;
        }

        // Project batching (strongest affinity)
        if (task.ProjectId.HasValue &&
            context.RecentProjectIds.Contains(task.ProjectId.Value))
        {
            affinityScore += 0.25;
        }

        // Tag similarity (partial credit for overlapping tags)
        if (task.Tags.Any() && context.RecentTags.Any())
        {
            var tagOverlap = task.Tags.Intersect(context.RecentTags, StringComparer.OrdinalIgnoreCase).Count();
            var tagUnion = task.Tags.Union(context.RecentTags, StringComparer.OrdinalIgnoreCase).Count();
            if (tagUnion > 0)
            {
                affinityScore += 0.15 * (tagOverlap / (double)tagUnion);
            }
        }

        // Stakeholder batching
        if (!string.IsNullOrEmpty(task.WhoFor) &&
            task.WhoFor.Equals(context.CurrentStakeholder, StringComparison.OrdinalIgnoreCase))
        {
            affinityScore += 0.15;
        }

        // Context batching
        if (task.Contexts.Any() && context.RecentContexts.Any())
        {
            var contextOverlap = task.Contexts.Intersect(context.RecentContexts, StringComparer.OrdinalIgnoreCase).Count();
            if (contextOverlap > 0)
            {
                affinityScore += 0.1;
            }
        }

        return Math.Min(1.0, affinityScore);
    }

    #endregion

    #region Task Similarity

    /// <summary>
    /// Computes similarity between two tasks using multiple dimensions.
    /// Uses weighted combination of categorical and semantic similarity.
    /// </summary>
    public double ComputeTaskSimilarity(TodoTask task1, TodoTask task2)
    {
        var similarities = new List<(double score, double weight)>();

        // Category similarity
        if (!string.IsNullOrEmpty(task1.Category) && !string.IsNullOrEmpty(task2.Category))
        {
            var catSim = task1.Category.Equals(task2.Category, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            similarities.Add((catSim, 2.0));
        }

        // Project similarity
        if (task1.ProjectId.HasValue && task2.ProjectId.HasValue)
        {
            var projSim = task1.ProjectId == task2.ProjectId ? 1.0 : 0.0;
            similarities.Add((projSim, 3.0));
        }

        // Context overlap (Jaccard)
        if (task1.Contexts.Any() && task2.Contexts.Any())
        {
            var intersection = task1.Contexts.Intersect(task2.Contexts, StringComparer.OrdinalIgnoreCase).Count();
            var union = task1.Contexts.Union(task2.Contexts, StringComparer.OrdinalIgnoreCase).Count();
            var ctxSim = union > 0 ? intersection / (double)union : 0;
            similarities.Add((ctxSim, 1.5));
        }

        // Tag overlap (Jaccard)
        if (task1.Tags.Any() && task2.Tags.Any())
        {
            var intersection = task1.Tags.Intersect(task2.Tags, StringComparer.OrdinalIgnoreCase).Count();
            var union = task1.Tags.Union(task2.Tags, StringComparer.OrdinalIgnoreCase).Count();
            var tagSim = union > 0 ? intersection / (double)union : 0;
            similarities.Add((tagSim, 1.5));
        }

        // Stakeholder similarity
        if (!string.IsNullOrEmpty(task1.WhoFor) && !string.IsNullOrEmpty(task2.WhoFor))
        {
            var stakeSim = task1.WhoFor.Equals(task2.WhoFor, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            similarities.Add((stakeSim, 1.0));
        }

        // Energy level proximity
        if (task1.EnergyLevel.HasValue && task2.EnergyLevel.HasValue)
        {
            var energyDiff = Math.Abs(task1.EnergyLevel.Value - task2.EnergyLevel.Value);
            var energySim = 1.0 - (energyDiff / 4.0);
            similarities.Add((energySim, 0.5));
        }

        // Title/description semantic similarity using entity extraction
        var textSim = ComputeTextSimilarity(task1, task2);
        similarities.Add((textSim, 1.0));

        if (!similarities.Any())
        {
            return 0.3; // Default low similarity
        }

        // Weighted average
        var totalWeight = similarities.Sum(s => s.weight);
        var weightedSum = similarities.Sum(s => s.score * s.weight);

        return weightedSum / totalWeight;
    }

    private double ComputeTextSimilarity(TodoTask task1, TodoTask task2)
    {
        var text1 = $"{task1.Title} {task1.Description}";
        var text2 = $"{task2.Title} {task2.Description}";

        var extraction1 = _entityExtraction.ExtractEntities(text1);
        var extraction2 = _entityExtraction.ExtractEntities(text2);

        // Compare extracted entities
        var allEntities1 = extraction1.Acronyms
            .Concat(extraction1.ProperNouns)
            .Concat(extraction1.Hashtags)
            .Concat(extraction1.RepeatedTerms.Select(t => t.Term))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allEntities2 = extraction2.Acronyms
            .Concat(extraction2.ProperNouns)
            .Concat(extraction2.Hashtags)
            .Concat(extraction2.RepeatedTerms.Select(t => t.Term))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allEntities1.Any() || !allEntities2.Any())
        {
            return 0.3;
        }

        var intersection = allEntities1.Intersect(allEntities2).Count();
        var union = allEntities1.Union(allEntities2).Count();

        return union > 0 ? intersection / (double)union : 0.3;
    }

    #endregion

    #region Adaptive Weight Computation

    /// <summary>
    /// Computes adaptive weights based on context.
    /// Weights shift dynamically based on time pressure, energy state, and workload.
    /// </summary>
    private AdaptiveWeights ComputeAdaptiveWeights(OptimizationContext context)
    {
        var prefs = context.Preferences;
        var weights = new AdaptiveWeights
        {
            // Start with user preferences (normalized to 0-1)
            Urgency = prefs.DueDateUrgencyWeight / 100.0,
            ContextFit = prefs.ContextGroupingWeight / 100.0,
            EnergyAlignment = prefs.EnergyMatchingWeight / 100.0,
            BatchingAffinity = (prefs.SimilarWorkGroupingWeight + prefs.StakeholderGroupingWeight + prefs.TagSimilarityWeight) / 300.0,
            Importance = 0.5, // Base importance weight
            Effort = 0.3, // Base effort weight
            Momentum = 0.3, // Base momentum weight
            Dependency = 0.4, // Base dependency weight
            Staleness = 0.2, // Base staleness weight
            OpportunityCost = 0.3 // Base opportunity cost weight
        };

        // Dynamic adjustments based on context

        // High time pressure: boost urgency and effort (favor quick wins)
        if (context.HighTimePressure)
        {
            weights.Urgency *= 1.5;
            weights.Effort *= 1.3;
            weights.BatchingAffinity *= 0.7; // Less concern for batching under pressure
        }

        // Low energy: boost energy alignment and reduce importance of demanding tasks
        if (context.CurrentEnergyLevel <= 2)
        {
            weights.EnergyAlignment *= 1.5;
            weights.Effort *= 1.2; // Prefer easier tasks
        }

        // Large backlog: boost importance and urgency, reduce staleness
        if (context.TotalBacklogSize > 50)
        {
            weights.Importance *= 1.2;
            weights.Urgency *= 1.1;
            weights.Staleness *= 0.8;
        }

        // Morning deep work window: boost deep work importance
        if (IsDeepWorkWindow(context))
        {
            weights.Importance *= 1.3; // Favor important tasks
            weights.Effort *= 0.8; // Less concern about effort
            weights.Momentum *= 1.2; // Favor focused flow
        }

        // End of day: favor quick wins and completion
        if (context.TargetHour >= 16)
        {
            weights.Effort *= 1.4;
            weights.Urgency *= 1.2;
        }

        return weights;
    }

    private bool IsDeepWorkWindow(OptimizationContext context)
    {
        var hour = context.TargetHour;
        var morningPeak = context.Preferences.MorningEnergyPeak;
        return Math.Abs(hour - morningPeak) <= 2;
    }

    #endregion

    #region Mathematical Functions

    /// <summary>
    /// Sigmoid decay function. Returns value approaching 0 as x increases.
    /// </summary>
    private double SigmoidDecay(double x, double midpoint, double steepness, bool inverted)
    {
        var sigmoid = 1.0 / (1.0 + Math.Exp(steepness * (x - midpoint)));
        return inverted ? 1.0 - sigmoid : sigmoid;
    }

    /// <summary>
    /// Exponential decay with specified half-life.
    /// </summary>
    private double ExponentialDecay(double x, double halfLife)
    {
        return Math.Pow(0.5, x / halfLife);
    }

    /// <summary>
    /// Hyperbolic growth function. Approaches max asymptotically.
    /// </summary>
    private double HyperbolicGrowth(double x, double scale, double max)
    {
        return max * x / (scale + x);
    }

    /// <summary>
    /// Gaussian scoring function. Peak at 0, decays with distance.
    /// </summary>
    private double GaussianScore(double x, double sigma)
    {
        return Math.Exp(-(x * x) / (2 * sigma * sigma));
    }

    #endregion

    #region Pareto Optimization

    /// <summary>
    /// Identifies Pareto-optimal tasks (non-dominated in multi-objective space).
    /// </summary>
    public IReadOnlyList<TaskScoreVector> FindParetoFrontier(IReadOnlyList<TaskScoreVector> vectors)
    {
        var frontier = new List<TaskScoreVector>();

        foreach (var candidate in vectors)
        {
            var dominated = false;

            foreach (var other in vectors)
            {
                if (other.TaskId == candidate.TaskId) continue;

                if (Dominates(other, candidate))
                {
                    dominated = true;
                    break;
                }
            }

            if (!dominated)
            {
                frontier.Add(candidate);
            }
        }

        return frontier;
    }

    /// <summary>
    /// Returns true if vector A dominates vector B (better or equal in all dimensions, strictly better in at least one).
    /// </summary>
    private bool Dominates(TaskScoreVector a, TaskScoreVector b)
    {
        var aDims = new[] { a.UrgencyScore, a.ImportanceScore, a.EffortScore, a.ContextFitScore, a.EnergyAlignmentScore };
        var bDims = new[] { b.UrgencyScore, b.ImportanceScore, b.EffortScore, b.ContextFitScore, b.EnergyAlignmentScore };

        var betterInAll = true;
        var strictlyBetterInOne = false;

        for (int i = 0; i < aDims.Length; i++)
        {
            if (aDims[i] < bDims[i])
            {
                betterInAll = false;
                break;
            }
            if (aDims[i] > bDims[i])
            {
                strictlyBetterInOne = true;
            }
        }

        return betterInAll && strictlyBetterInOne;
    }

    #endregion

    #region Clustering for Batching

    /// <summary>
    /// Clusters tasks using k-medoids algorithm for optimal batching.
    /// </summary>
    public IReadOnlyList<TaskCluster> ClusterTasks(
        IReadOnlyList<TodoTask> tasks,
        int maxClusters = 5)
    {
        if (tasks.Count <= maxClusters)
        {
            // Each task is its own cluster
            return tasks.Select(t => new TaskCluster
            {
                MedoidTask = t,
                Tasks = new List<TodoTask> { t },
                Cohesion = 1.0
            }).ToList();
        }

        // Compute similarity matrix
        var n = tasks.Count;
        var dissimilarity = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var sim = ComputeTaskSimilarity(tasks[i], tasks[j]);
                var dissim = 1.0 - sim;
                dissimilarity[i, j] = dissim;
                dissimilarity[j, i] = dissim;
            }
        }

        // K-medoids (PAM algorithm simplified)
        var medoidIndices = SelectInitialMedoids(dissimilarity, n, maxClusters);
        var assignments = AssignToClusters(dissimilarity, medoidIndices, n);

        // Build clusters
        var clusters = new List<TaskCluster>();
        for (int k = 0; k < medoidIndices.Count; k++)
        {
            var medoidIdx = medoidIndices[k];
            var clusterTasks = tasks
                .Select((t, i) => (task: t, idx: i))
                .Where(x => assignments[x.idx] == k)
                .Select(x => x.task)
                .ToList();

            if (clusterTasks.Any())
            {
                var cohesion = ComputeClusterCohesion(clusterTasks);
                clusters.Add(new TaskCluster
                {
                    MedoidTask = tasks[medoidIdx],
                    Tasks = clusterTasks,
                    Cohesion = cohesion
                });
            }
        }

        return clusters.OrderByDescending(c => c.Cohesion * c.Tasks.Count).ToList();
    }

    private List<int> SelectInitialMedoids(double[,] dissimilarity, int n, int k)
    {
        // Greedy initialization: pick points that maximize inter-medoid distance
        var medoids = new List<int>();

        // First medoid: most central point
        var centrality = Enumerable.Range(0, n)
            .Select(i => Enumerable.Range(0, n).Sum(j => dissimilarity[i, j]))
            .ToList();
        medoids.Add(centrality.IndexOf(centrality.Min()));

        // Remaining medoids: maximize distance to nearest existing medoid
        while (medoids.Count < k)
        {
            var maxMinDist = -1.0;
            var nextMedoid = -1;

            for (int i = 0; i < n; i++)
            {
                if (medoids.Contains(i)) continue;

                var minDistToMedoid = medoids.Min(m => dissimilarity[i, m]);
                if (minDistToMedoid > maxMinDist)
                {
                    maxMinDist = minDistToMedoid;
                    nextMedoid = i;
                }
            }

            if (nextMedoid >= 0)
            {
                medoids.Add(nextMedoid);
            }
            else
            {
                break;
            }
        }

        return medoids;
    }

    private int[] AssignToClusters(double[,] dissimilarity, List<int> medoids, int n)
    {
        var assignments = new int[n];

        for (int i = 0; i < n; i++)
        {
            var minDist = double.MaxValue;
            var bestCluster = 0;

            for (int k = 0; k < medoids.Count; k++)
            {
                var dist = dissimilarity[i, medoids[k]];
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCluster = k;
                }
            }

            assignments[i] = bestCluster;
        }

        return assignments;
    }

    private double ComputeClusterCohesion(IReadOnlyList<TodoTask> clusterTasks)
    {
        if (clusterTasks.Count <= 1) return 1.0;

        var totalSim = 0.0;
        var count = 0;

        for (int i = 0; i < clusterTasks.Count; i++)
        {
            for (int j = i + 1; j < clusterTasks.Count; j++)
            {
                totalSim += ComputeTaskSimilarity(clusterTasks[i], clusterTasks[j]);
                count++;
            }
        }

        return count > 0 ? totalSim / count : 1.0;
    }

    #endregion

    #region Critical Path Analysis

    /// <summary>
    /// Computes critical path through task dependency graph.
    /// Returns tasks that lie on the longest dependency chain.
    /// </summary>
    public IReadOnlySet<Guid> ComputeCriticalPath(IReadOnlyList<TodoTask> tasks)
    {
        var criticalPath = new HashSet<Guid>();

        // Build dependency graph
        var taskMap = tasks.ToDictionary(t => t.Id);
        var inDegree = tasks.ToDictionary(t => t.Id, _ => 0);
        var longestPath = tasks.ToDictionary(t => t.Id, _ => 0);
        var predecessor = new Dictionary<Guid, Guid?>();

        // Calculate in-degrees
        foreach (var task in tasks)
        {
            foreach (var blockerId in task.BlockedByTaskIds)
            {
                if (taskMap.ContainsKey(blockerId))
                {
                    inDegree[task.Id]++;
                }
            }
            predecessor[task.Id] = null;
        }

        // Topological sort with longest path calculation
        var queue = new Queue<Guid>();
        foreach (var task in tasks.Where(t => inDegree[t.Id] == 0))
        {
            queue.Enqueue(task.Id);
            longestPath[task.Id] = GetTaskWeight(taskMap[task.Id]);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var current = taskMap[currentId];

            // Find tasks blocked by current
            var dependents = tasks.Where(t => t.BlockedByTaskIds.Contains(currentId));

            foreach (var dependent in dependents)
            {
                var newPath = longestPath[currentId] + GetTaskWeight(dependent);

                if (newPath > longestPath[dependent.Id])
                {
                    longestPath[dependent.Id] = newPath;
                    predecessor[dependent.Id] = currentId;
                }

                inDegree[dependent.Id]--;
                if (inDegree[dependent.Id] == 0)
                {
                    queue.Enqueue(dependent.Id);
                }
            }
        }

        // Trace back from longest path endpoint
        if (longestPath.Any())
        {
            var endpointId = longestPath.OrderByDescending(kv => kv.Value).First().Key;
            var current = (Guid?)endpointId;

            while (current.HasValue)
            {
                criticalPath.Add(current.Value);
                current = predecessor.GetValueOrDefault(current.Value);
            }
        }

        return criticalPath;
    }

    private int GetTaskWeight(TodoTask task)
    {
        // Weight based on estimated time and priority
        var timeWeight = task.EstimatedMinutes > 0 ? task.EstimatedMinutes : 30;
        var priorityWeight = (4 - task.Priority) * 10;
        return timeWeight + priorityWeight;
    }

    #endregion
}

#region Data Models

/// <summary>
/// Multi-dimensional score vector for Pareto optimization.
/// </summary>
public class TaskScoreVector
{
    public Guid TaskId { get; set; }
    public double UrgencyScore { get; set; }
    public double ImportanceScore { get; set; }
    public double EffortScore { get; set; }
    public double ContextFitScore { get; set; }
    public double EnergyAlignmentScore { get; set; }
    public double MomentumScore { get; set; }
    public double DependencyScore { get; set; }
    public double StalenessScore { get; set; }
    public double OpportunityCostScore { get; set; }
    public double BatchingAffinityScore { get; set; }
}

/// <summary>
/// Extended optimization context with rich contextual information.
/// </summary>
public class OptimizationContext
{
    public required UserPreferences Preferences { get; init; }
    public DateOnly TargetDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    public int TargetHour { get; init; } = DateTime.Now.Hour;
    public double CurrentEnergyLevel { get; init; } = 3.0;
    public int AvailableBlockMinutes { get; init; } = 60;
    public bool HighTimePressure { get; init; }
    public int TotalBacklogSize { get; init; }

    // Context state
    public IReadOnlyList<string> AvailableContexts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecentCategories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Guid> RecentProjectIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<string> RecentTags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecentContexts { get; init; } = Array.Empty<string>();
    public string? CurrentStakeholder { get; init; }
    public IReadOnlyList<Guid> RecentlyCompletedTaskIds { get; init; } = Array.Empty<Guid>();

    // Dependency analysis results
    public IReadOnlySet<Guid> CriticalPathTaskIds { get; init; } = new HashSet<Guid>();
    public IReadOnlyDictionary<Guid, int> TasksBlockedBy { get; init; } = new Dictionary<Guid, int>();
}

/// <summary>
/// Adaptive weights that change based on context.
/// </summary>
public class AdaptiveWeights
{
    public double Urgency { get; set; }
    public double Importance { get; set; }
    public double Effort { get; set; }
    public double ContextFit { get; set; }
    public double EnergyAlignment { get; set; }
    public double Momentum { get; set; }
    public double Dependency { get; set; }
    public double Staleness { get; set; }
    public double OpportunityCost { get; set; }
    public double BatchingAffinity { get; set; }
}

/// <summary>
/// Cluster of similar tasks for batching.
/// </summary>
public class TaskCluster
{
    public required TodoTask MedoidTask { get; init; }
    public required IReadOnlyList<TodoTask> Tasks { get; init; }
    public double Cohesion { get; init; }

    public string GetClusterName()
    {
        // Use the medoid's most distinctive feature
        if (MedoidTask.ProjectId.HasValue)
            return $"Project: {MedoidTask.ProjectId}";
        if (!string.IsNullOrEmpty(MedoidTask.Category))
            return MedoidTask.Category;
        if (MedoidTask.Contexts.Any())
            return $"@{MedoidTask.Contexts.First()}";
        if (!string.IsNullOrEmpty(MedoidTask.WhoFor))
            return $"For: {MedoidTask.WhoFor}";
        return "General";
    }
}

#endregion
