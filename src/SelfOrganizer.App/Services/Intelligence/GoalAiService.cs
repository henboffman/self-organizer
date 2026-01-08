using System.Text.Json.Serialization;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for AI-powered goal operations
/// </summary>
public interface IGoalAiService
{
    Task<bool> IsAvailableAsync();
    Task<GoalClarificationResult?> ClarifyGoalAsync(Goal goal);
    Task<GoalDecompositionResult?> DecomposeGoalAsync(Goal goal);
    Task<List<TodoTask>> GenerateTasksFromGoalAsync(Goal goal);
}

/// <summary>
/// Result of goal clarification
/// </summary>
public class GoalClarificationResult
{
    public string OriginalGoal { get; set; } = string.Empty;
    public string ClarifiedGoal { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public List<string> ClarifyingQuestions { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public string? SuggestedTimeframe { get; set; }

    // These can come as either strings or arrays from the LLM
    // Using JsonPropertyName to match what the LLM returns
    [JsonPropertyName("successCriteria")]
    public List<string> SuccessCriteriaList { get; set; } = new();

    [JsonPropertyName("obstacles")]
    public List<string> ObstaclesList { get; set; } = new();

    [JsonPropertyName("resources")]
    public List<string> ResourcesList { get; set; } = new();

    // Computed properties that convert lists to strings for the Goal model
    [JsonIgnore]
    public string? SuccessCriteria => SuccessCriteriaList.Any()
        ? string.Join("\n• ", SuccessCriteriaList.Prepend(""))
        : null;

    [JsonIgnore]
    public string? Obstacles => ObstaclesList.Any()
        ? string.Join("\n• ", ObstaclesList.Prepend(""))
        : null;

    [JsonIgnore]
    public string? Resources => ResourcesList.Any()
        ? string.Join("\n• ", ResourcesList.Prepend(""))
        : null;
}

/// <summary>
/// Result of goal decomposition
/// </summary>
public class GoalDecompositionResult
{
    public string ProjectName { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public List<GoalPhase> Phases { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<string> Resources { get; set; } = new();
}

public class GoalPhase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<GeneratedTask> Tasks { get; set; } = new();
}

public class GeneratedTask
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public List<string> Contexts { get; set; } = new();
    public int EnergyLevel { get; set; } = 3;
    public bool RequiresDeepWork { get; set; }
}

/// <summary>
/// Implementation of goal AI service using LLM
/// </summary>
public class GoalAiService : IGoalAiService
{
    private readonly ILlmService _llmService;

    public GoalAiService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _llmService.IsAvailableAsync();
    }

    public async Task<GoalClarificationResult?> ClarifyGoalAsync(Goal goal)
    {
        var goalContext = BuildGoalContext(goal);
        var prompt = string.Format(ClarificationPrompt, goalContext);

        return await _llmService.GenerateStructuredAsync<GoalClarificationResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 180 // Longer timeout for first request when model may need to load
        });
    }

    public async Task<GoalDecompositionResult?> DecomposeGoalAsync(Goal goal)
    {
        var goalContext = BuildGoalContext(goal);
        var prompt = string.Format(DecompositionPrompt, goalContext);

        return await _llmService.GenerateStructuredAsync<GoalDecompositionResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 4096,
            TimeoutSeconds = 180 // Longer timeout for when model may need to load
        });
    }

    public async Task<List<TodoTask>> GenerateTasksFromGoalAsync(Goal goal)
    {
        var decomposition = await DecomposeGoalAsync(goal);
        if (decomposition == null)
            return new List<TodoTask>();

        var tasks = new List<TodoTask>();
        foreach (var phase in decomposition.Phases)
        {
            foreach (var generatedTask in phase.Tasks)
            {
                tasks.Add(new TodoTask
                {
                    Id = Guid.NewGuid(),
                    Title = generatedTask.Title,
                    Description = generatedTask.Description,
                    EstimatedMinutes = generatedTask.EstimatedMinutes,
                    Contexts = generatedTask.Contexts,
                    EnergyLevel = generatedTask.EnergyLevel,
                    RequiresDeepWork = generatedTask.RequiresDeepWork,
                    Status = TodoTaskStatus.NextAction,
                    Priority = 2,
                    GoalIds = new List<Guid> { goal.Id },
                    Tags = new List<string> { phase.Name.ToLowerInvariant().Replace(" ", "-") },
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });
            }
        }

        return tasks;
    }

    private static string BuildGoalContext(Goal goal)
    {
        var parts = new List<string>
        {
            $"Goal: {goal.Title}"
        };

        if (!string.IsNullOrEmpty(goal.Description))
            parts.Add($"Description: {goal.Description}");

        if (!string.IsNullOrEmpty(goal.DesiredOutcome))
            parts.Add($"Desired Outcome: {goal.DesiredOutcome}");

        if (!string.IsNullOrEmpty(goal.SuccessCriteria))
            parts.Add($"Success Criteria: {goal.SuccessCriteria}");

        if (!string.IsNullOrEmpty(goal.Obstacles))
            parts.Add($"Known Obstacles: {goal.Obstacles}");

        if (!string.IsNullOrEmpty(goal.Resources))
            parts.Add($"Available Resources: {goal.Resources}");

        parts.Add($"Category: {goal.Category}");
        parts.Add($"Timeframe: {goal.Timeframe}");

        if (goal.TargetDate.HasValue)
            parts.Add($"Target Date: {goal.TargetDate.Value:MMMM d, yyyy}");

        return string.Join("\n", parts);
    }

    private const string ClarificationPrompt = @"You are a productivity coach helping users clarify and improve their goals using GTD (Getting Things Done) methodology and SMART goal principles.

Given the following goal information, help improve and clarify it by:
1. Making the goal more specific and actionable
2. Defining what success looks like (the desired outcome)
3. Identifying measurable success criteria
4. Uncovering potential obstacles
5. Identifying resources needed
6. Suggesting clarifying questions if the goal is still vague

Respond with a JSON object:
{{
    ""originalGoal"": ""the user's original goal title"",
    ""clarifiedGoal"": ""a clearer, more specific version of the goal"",
    ""desiredOutcome"": ""vivid description of what success looks like when achieved"",
    ""successCriteria"": [""specific, measurable criteria to know when the goal is complete""],
    ""obstacles"": [""potential challenges or blockers to anticipate""],
    ""resources"": [""tools, skills, people, or time needed to achieve this""],
    ""clarifyingQuestions"": [""questions that would help further refine the goal""],
    ""assumptions"": [""assumptions that should be validated""],
    ""suggestedTimeframe"": ""recommended timeframe if the current one seems off""
}}

Goal Information:
{0}";

    private const string DecompositionPrompt = @"You are a project planning assistant using GTD methodology.

Break down the following goal into concrete, actionable phases and tasks. Each task should be:
1. A specific physical action (not vague or abstract)
2. Something that can be done in one sitting (ideally under 2 hours)
3. Clear about what ""done"" looks like
4. Start with an action verb

Organize tasks into logical phases that build toward the goal.

Respond with a JSON object:
{{
    ""projectName"": ""name for this project based on the goal"",
    ""desiredOutcome"": ""the end result when complete"",
    ""phases"": [
        {{
            ""name"": ""phase name (e.g., Research, Planning, Execution, Review)"",
            ""description"": ""what this phase accomplishes"",
            ""tasks"": [
                {{
                    ""title"": ""specific action starting with verb (e.g., 'Research competing products', 'Draft outline for proposal')"",
                    ""description"": ""additional details if needed"",
                    ""estimatedMinutes"": 30,
                    ""contexts"": [""@computer"", ""@phone"", ""@home"", ""@office"", ""@errands""],
                    ""energyLevel"": 3,
                    ""requiresDeepWork"": false
                }}
            ]
        }}
    ],
    ""dependencies"": [""external factors that could block progress""],
    ""resources"": [""tools, people, or information needed""]
}}

Energy levels: 1=Very Low (mindless tasks), 2=Low, 3=Medium, 4=High, 5=Very High (deep focus required)
Contexts are GTD-style starting with @ indicating where/how the task is done.
Limit to 3-5 phases with 3-7 tasks each for manageability.

Goal Information:
{0}";
}
