using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for AI-powered balance recommendations
/// </summary>
public interface IBalanceAiService
{
    Task<bool> IsAvailableAsync();
    Task<BalanceRecommendationResult?> GenerateRecommendationsAsync(BalanceRecommendationRequest request);
}

/// <summary>
/// Implementation of balance AI service using LLM
/// </summary>
public class BalanceAiService : IBalanceAiService
{
    private readonly ILlmService _llmService;

    public BalanceAiService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _llmService.IsAvailableAsync();
    }

    public async Task<BalanceRecommendationResult?> GenerateRecommendationsAsync(BalanceRecommendationRequest request)
    {
        var context = BuildBalanceContext(request);
        var prompt = string.Format(BalanceRecommendationPrompt,
            context,
            request.SuggestTasks ? "yes" : "no",
            request.SuggestGoals ? "yes" : "no",
            request.ShowInsights ? "yes" : "no",
            request.Threshold);

        return await _llmService.GenerateStructuredAsync<BalanceRecommendationResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 4096,
            TimeoutSeconds = 180
        });
    }

    private static string BuildBalanceContext(BalanceRecommendationRequest request)
    {
        var parts = new List<string>();

        // Add current mode context
        parts.Add($"Current Focus Mode: {request.CurrentMode}");
        parts.Add("");

        // Add balance dimensions with their ratings
        parts.Add("=== LIFE BALANCE DIMENSIONS ===");
        foreach (var dimension in request.Dimensions)
        {
            var rating = request.Ratings.TryGetValue(dimension.Id, out var r) ? r : 5;
            var status = rating <= 3 ? "CRITICAL" : rating <= 5 ? "NEEDS_ATTENTION" : rating <= 7 ? "OK" : "THRIVING";
            parts.Add($"- {dimension.Name} ({dimension.Id}): {rating}/10 [{status}]");
            parts.Add($"  Description: {dimension.Description}");

            // Count linked goals for this dimension
            var linkedGoalIds = request.ActiveGoals
                .Where(g => g.BalanceDimensionIds?.Contains(dimension.Id) == true)
                .Select(g => g.Id)
                .ToHashSet();
            var linkedGoals = linkedGoalIds.Count;
            // Count tasks linked through goals
            var linkedTasks = request.ActiveTasks.Count(t => t.GoalIds?.Any(gid => linkedGoalIds.Contains(gid)) == true);
            if (linkedGoals > 0 || linkedTasks > 0)
            {
                parts.Add($"  Linked items: {linkedGoals} goals, {linkedTasks} tasks");
            }
        }
        parts.Add("");

        // Add active goals
        if (request.ActiveGoals.Any())
        {
            parts.Add("=== CURRENT ACTIVE GOALS ===");
            foreach (var goal in request.ActiveGoals.Take(10))
            {
                var dims = goal.BalanceDimensionIds?.Any() == true
                    ? $" [Dimensions: {string.Join(", ", goal.BalanceDimensionIds)}]"
                    : "";
                parts.Add($"- {goal.Title} ({goal.Category}, {goal.Timeframe}){dims}");
                if (!string.IsNullOrEmpty(goal.DesiredOutcome))
                {
                    parts.Add($"  Outcome: {goal.DesiredOutcome}");
                }
                parts.Add($"  Progress: {goal.ProgressPercent}%");
            }
            parts.Add("");
        }

        // Add active tasks summary
        if (request.ActiveTasks.Any())
        {
            parts.Add("=== CURRENT ACTIVE TASKS SUMMARY ===");
            parts.Add($"Total active tasks: {request.ActiveTasks.Count}");

            // Group tasks by dimension (through their linked goals)
            var allGoalDimensionMappings = request.ActiveGoals
                .Where(g => g.BalanceDimensionIds?.Any() == true)
                .SelectMany(g => g.BalanceDimensionIds!.Select(d => new { DimId = d, GoalId = g.Id }))
                .ToLookup(x => x.GoalId, x => x.DimId);

            var tasksByDimension = request.ActiveTasks
                .Where(t => t.GoalIds?.Any() == true)
                .SelectMany(t => t.GoalIds!
                    .SelectMany(gid => allGoalDimensionMappings[gid])
                    .Distinct()
                    .Select(d => new { DimId = d, Task = t }))
                .GroupBy(x => x.DimId)
                .ToList();

            if (tasksByDimension.Any())
            {
                parts.Add("Tasks by dimension:");
                foreach (var group in tasksByDimension)
                {
                    parts.Add($"  - {group.Key}: {group.Count()} tasks");
                }
            }

            // Show some sample tasks for context
            parts.Add("Sample tasks:");
            foreach (var task in request.ActiveTasks.Take(5))
            {
                parts.Add($"  - {task.Title}");
            }
            parts.Add("");
        }

        // Add dimensions needing attention (below threshold)
        var lowRatedDimensions = request.Dimensions
            .Where(d => request.Ratings.TryGetValue(d.Id, out var r) && r <= request.Threshold)
            .ToList();

        if (lowRatedDimensions.Any())
        {
            parts.Add("=== DIMENSIONS NEEDING ATTENTION (below threshold) ===");
            foreach (var dim in lowRatedDimensions.OrderBy(d => request.Ratings.GetValueOrDefault(d.Id, 5)))
            {
                var rating = request.Ratings.GetValueOrDefault(dim.Id, 5);
                parts.Add($"- {dim.Name}: {rating}/10 - {dim.Description}");
            }
        }

        return string.Join("\n", parts);
    }

    private const string BalanceRecommendationPrompt = @"You are a life balance coach and productivity expert helping users achieve better work-life balance using GTD (Getting Things Done) methodology and holistic wellbeing principles.

Analyze the user's current life balance ratings and existing commitments to provide personalized recommendations for improving areas that need attention.

CONTEXT:
{0}

GENERATION PREFERENCES:
- Generate tasks: {1}
- Generate goals: {2}
- Generate insights: {3}
- Focus on dimensions rated {4}/10 or below

GUIDELINES:
1. Focus recommendations on the lowest-rated dimensions that don't already have significant effort (goals/tasks) allocated
2. Tasks should be specific, actionable, and completable in one sitting (15-120 minutes)
3. Goals should be meaningful but achievable, with clear outcomes
4. Insights should reveal patterns, celebrate strengths, or highlight opportunities
5. Consider the user's current mode (Balanced, Focus, Sprint) when making suggestions
6. Don't suggest tasks/goals for dimensions the user is already actively working on unless they're still critically low
7. Use markdown formatting in rationale and descriptions for better readability

Respond with a JSON object:
{{
    ""overallAssessment"": ""A 2-3 sentence summary of the user's current balance state, highlighting key areas of concern and strength"",
    ""suggestedTasks"": [
        {{
            ""title"": ""specific action starting with a verb (e.g., 'Schedule a 30-minute walk')"",
            ""description"": ""why this helps and how to approach it"",
            ""dimensionId"": ""the exact dimension ID this task improves"",
            ""estimatedMinutes"": 30,
            ""priority"": 2,
            ""suggestedContexts"": [""@home"", ""@phone""],
            ""rationale"": ""explanation of why this task will help improve this dimension""
        }}
    ],
    ""suggestedGoals"": [
        {{
            ""title"": ""meaningful goal title"",
            ""description"": ""what this goal entails"",
            ""desiredOutcome"": ""vivid description of what success looks like"",
            ""dimensionId"": ""the exact dimension ID this goal addresses"",
            ""suggestedTimeframe"": ""Week|Month|Quarter|Year"",
            ""rationale"": ""why this goal is important for their balance""
        }}
    ],
    ""insights"": [
        {{
            ""type"": ""Pattern|Strength|Concern|Opportunity"",
            ""title"": ""brief insight title"",
            ""description"": ""detailed explanation with actionable advice"",
            ""affectedDimensions"": [""dimension IDs this insight relates to""]
        }}
    ]
}}

IMPORTANT:
- Generate 2-4 tasks if tasks are requested
- Generate 1-2 goals if goals are requested
- Generate 2-3 insights if insights are requested
- Use EXACT dimension IDs from the context (e.g., ""health"", ""career"", ""relationships"")
- Priority: 1=High, 2=Normal, 3=Low
- Insight types: Pattern (recurring behavior), Strength (doing well), Concern (needs attention), Opportunity (potential improvement)";
}
