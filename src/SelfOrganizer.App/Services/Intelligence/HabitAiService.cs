using System.Text.Json.Serialization;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for AI-powered habit generation and suggestions
/// </summary>
public interface IHabitAiService
{
    Task<bool> IsAvailableAsync();
    Task<HabitSuggestionResult?> GenerateHabitsForGoalAsync(Goal goal);
    Task<GoalSuggestionResult?> SuggestGoalsForHabitAsync(Habit habit, IReadOnlyList<Goal> existingGoals);
}

/// <summary>
/// Result of AI habit generation for a goal
/// </summary>
public class HabitSuggestionResult
{
    [JsonPropertyName("goalTitle")]
    public string GoalTitle { get; set; } = string.Empty;

    [JsonPropertyName("overallStrategy")]
    public string OverallStrategy { get; set; } = string.Empty;

    [JsonPropertyName("suggestedHabits")]
    public List<SuggestedHabit> SuggestedHabits { get; set; } = new();
}

/// <summary>
/// A single AI-suggested habit
/// </summary>
public class SuggestedHabit
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = "Daily";

    [JsonPropertyName("targetCount")]
    public int TargetCount { get; set; } = 1;

    [JsonPropertyName("preferredTimeOfDay")]
    public string? PreferredTimeOfDay { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("impactLevel")]
    public string ImpactLevel { get; set; } = "Medium";

    [JsonPropertyName("startSmallTip")]
    public string? StartSmallTip { get; set; }
}

/// <summary>
/// Result of AI goal suggestions for a habit
/// </summary>
public class GoalSuggestionResult
{
    [JsonPropertyName("habitName")]
    public string HabitName { get; set; } = string.Empty;

    [JsonPropertyName("matchedGoals")]
    public List<GoalMatch> MatchedGoals { get; set; } = new();

    [JsonPropertyName("suggestedNewGoals")]
    public List<SuggestedGoalFromHabit> SuggestedNewGoals { get; set; } = new();
}

/// <summary>
/// A matched existing goal for a habit
/// </summary>
public class GoalMatch
{
    [JsonPropertyName("goalId")]
    public string GoalId { get; set; } = string.Empty;

    [JsonPropertyName("relevanceScore")]
    public int RelevanceScore { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// A suggested new goal based on a habit
/// </summary>
public class SuggestedGoalFromHabit
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("desiredOutcome")]
    public string DesiredOutcome { get; set; } = string.Empty;

    [JsonPropertyName("timeframe")]
    public string Timeframe { get; set; } = "Quarter";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Personal";
}

/// <summary>
/// Implementation of habit AI service using LLM
/// </summary>
public class HabitAiService : IHabitAiService
{
    private readonly ILlmService _llmService;

    public HabitAiService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _llmService.IsAvailableAsync();
    }

    public async Task<HabitSuggestionResult?> GenerateHabitsForGoalAsync(Goal goal)
    {
        var goalContext = BuildGoalContext(goal);
        var prompt = string.Format(HabitGenerationPrompt, goalContext);

        return await _llmService.GenerateStructuredAsync<HabitSuggestionResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 120
        });
    }

    public async Task<GoalSuggestionResult?> SuggestGoalsForHabitAsync(Habit habit, IReadOnlyList<Goal> existingGoals)
    {
        var habitContext = BuildHabitContext(habit, existingGoals);
        var prompt = string.Format(GoalSuggestionPrompt, habitContext);

        return await _llmService.GenerateStructuredAsync<GoalSuggestionResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 120
        });
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

        parts.Add($"Category: {goal.Category}");
        parts.Add($"Timeframe: {goal.Timeframe}");

        if (goal.TargetDate.HasValue)
            parts.Add($"Target Date: {goal.TargetDate.Value:MMMM d, yyyy}");

        return string.Join("\n", parts);
    }

    private static string BuildHabitContext(Habit habit, IReadOnlyList<Goal> existingGoals)
    {
        var parts = new List<string>
        {
            $"Habit: {habit.Name}",
            $"Frequency: {habit.Frequency}"
        };

        if (!string.IsNullOrEmpty(habit.Description))
            parts.Add($"Description: {habit.Description}");

        if (!string.IsNullOrEmpty(habit.Category))
            parts.Add($"Category: {habit.Category}");

        if (existingGoals.Any())
        {
            parts.Add("");
            parts.Add("User's Existing Goals:");
            foreach (var goal in existingGoals.Where(g => g.Status == GoalStatus.Active))
            {
                parts.Add($"- [{goal.Id}] {goal.Title} ({goal.Category}, {goal.Timeframe})");
                if (!string.IsNullOrEmpty(goal.DesiredOutcome))
                    parts.Add($"  Outcome: {goal.DesiredOutcome}");
            }
        }

        return string.Join("\n", parts);
    }

    private const string HabitGenerationPrompt = @"You are a behavioral science expert and habit coach. Your task is to suggest daily/weekly habits that will help someone achieve their goal.

Focus on habits that are:
1. Small and easy to start (2-minute rule friendly)
2. Directly connected to the goal's success
3. Evidence-based and effective
4. Stackable with existing routines
5. Measurable and trackable

Respond with a JSON object:
{{
    ""goalTitle"": ""the goal title"",
    ""overallStrategy"": ""brief explanation of how habits will help achieve this goal"",
    ""suggestedHabits"": [
        {{
            ""name"": ""short, action-oriented habit name (e.g., 'Morning stretch routine', 'Read 10 pages')"",
            ""description"": ""what specifically to do"",
            ""rationale"": ""why this habit helps achieve the goal (be specific about the connection)"",
            ""frequency"": ""Daily|Weekdays|Weekly"",
            ""targetCount"": 1,
            ""preferredTimeOfDay"": ""Morning|Afternoon|Evening|Anytime"",
            ""category"": ""Health|Productivity|Learning|Mindfulness|Social|Creative|Finance"",
            ""impactLevel"": ""High|Medium|Low"",
            ""startSmallTip"": ""how to make this habit tiny to start (the 2-minute version)""
        }}
    ]
}}

Suggest 3-5 habits, ordered by impact level (highest first).
Focus on keystone habits that create positive cascades.
Include at least one habit that addresses common obstacles.

Goal Information:
{0}";

    private const string GoalSuggestionPrompt = @"You are a goal-setting coach. Analyze this habit and identify which existing goals it supports, and suggest potential new goals.

For existing goals, evaluate how directly this habit contributes to achieving that goal (1-10 relevance score).
For new goal suggestions, think about the bigger ""why"" behind this habit.

Respond with a JSON object:
{{
    ""habitName"": ""the habit name"",
    ""matchedGoals"": [
        {{
            ""goalId"": ""the goal ID from the list"",
            ""relevanceScore"": 8,
            ""explanation"": ""how this habit directly supports this goal""
        }}
    ],
    ""suggestedNewGoals"": [
        {{
            ""title"": ""suggested goal title"",
            ""description"": ""what achieving this goal means"",
            ""desiredOutcome"": ""vivid description of success"",
            ""timeframe"": ""Quarter|Year|Multi-Year"",
            ""category"": ""Personal|Career|Health|Financial|Relationships|Learning|Creative""
        }}
    ]
}}

Only include matchedGoals with relevance score >= 6.
Suggest 0-2 new goals that this habit could naturally support.

Habit Information:
{0}";
}
