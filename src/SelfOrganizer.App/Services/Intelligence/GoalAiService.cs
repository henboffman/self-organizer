using System.Text.Json;
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
    Task<GoalClarificationResult?> ClarifyGoalAsync(Goal goal, IReadOnlyList<BalanceDimension>? availableDimensions = null);
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

    // Balance dimensions suggested by AI
    [JsonPropertyName("suggestedBalanceDimensions")]
    public List<string> SuggestedBalanceDimensions { get; set; } = new();

    // These can come as either strings or arrays from the LLM
    // Using JsonPropertyName to match what the LLM returns
    // Using custom converter to handle both string and array formats
    [JsonPropertyName("successCriteria")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> SuccessCriteriaList { get; set; } = new();

    [JsonPropertyName("obstacles")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> ObstaclesList { get; set; } = new();

    [JsonPropertyName("resources")]
    [JsonConverter(typeof(StringOrArrayConverter))]
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

    public async Task<GoalClarificationResult?> ClarifyGoalAsync(Goal goal, IReadOnlyList<BalanceDimension>? availableDimensions = null)
    {
        var goalContext = BuildGoalContext(goal, availableDimensions);
        var prompt = availableDimensions?.Any() == true
            ? string.Format(ClarificationWithBalancePrompt, goalContext)
            : string.Format(ClarificationPrompt, goalContext);

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
                    // Strip @ prefix from AI-generated contexts (ContextBadge adds it for display)
                    Contexts = generatedTask.Contexts.Select(c => c.TrimStart('@')).ToList(),
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

    private static string BuildGoalContext(Goal goal, IReadOnlyList<BalanceDimension>? availableDimensions = null)
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

        // Add available balance dimensions if provided
        if (availableDimensions?.Any() == true)
        {
            parts.Add("");
            parts.Add("Available Life Balance Dimensions:");
            foreach (var dim in availableDimensions)
            {
                parts.Add($"- {dim.Id}: {dim.Name} ({dim.Description})");
            }
        }

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

    private const string ClarificationWithBalancePrompt = @"You are a productivity coach helping users clarify and improve their goals using GTD (Getting Things Done) methodology and SMART goal principles.

Given the following goal information, help improve and clarify it by:
1. Making the goal more specific and actionable
2. Defining what success looks like (the desired outcome)
3. Identifying measurable success criteria
4. Uncovering potential obstacles
5. Identifying resources needed
6. Suggesting clarifying questions if the goal is still vague
7. Identifying which life balance dimensions this goal impacts

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
    ""suggestedTimeframe"": ""recommended timeframe if the current one seems off"",
    ""suggestedBalanceDimensions"": [""dimension IDs from the list below that this goal impacts""]
}}

Important: For suggestedBalanceDimensions, use the exact dimension IDs provided in the Available Life Balance Dimensions section. Select 1-3 dimensions that this goal will most positively impact when achieved.

Goal Information:
{0}";
}

/// <summary>
/// JSON converter that handles both string and array inputs for List&lt;string&gt; properties.
/// LLMs sometimes return a single string instead of an array.
/// </summary>
public class StringOrArrayConverter : JsonConverter<List<string>>
{
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<string>();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Single string - convert to list by splitting on common delimiters or keeping as single item
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            // If it contains newlines or bullet points, split into multiple items
            if (value.Contains('\n') || value.Contains("•") || value.Contains("- "))
            {
                var items = value
                    .Split(new[] { '\n', '•' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.TrimStart('-', ' ', '\t').Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                return items.Count > 0 ? items : new List<string> { value };
            }

            // If it contains commas, split on commas
            if (value.Contains(','))
            {
                var items = value
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                return items.Count > 0 ? items : new List<string> { value };
            }

            return new List<string> { value };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var item = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        // Unexpected token type - return empty list
        return new List<string>();
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}
