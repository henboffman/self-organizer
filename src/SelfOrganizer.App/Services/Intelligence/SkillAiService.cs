using System.Text.Json.Serialization;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for AI-powered skill suggestions and recommendations
/// </summary>
public interface ISkillAiService
{
    Task<bool> IsAvailableAsync();
    Task<SkillSuggestionResult?> GenerateSkillsForGoalAsync(Goal goal);
    Task<GoalSuggestionForSkillResult?> SuggestGoalsForSkillAsync(Skill skill, IReadOnlyList<Goal> existingGoals);
    Task<HabitSuggestionForSkillResult?> SuggestHabitsForSkillAsync(Skill skill);
}

/// <summary>
/// Result of AI skill generation for a goal
/// </summary>
public class SkillSuggestionResult
{
    [JsonPropertyName("goalTitle")]
    public string GoalTitle { get; set; } = string.Empty;

    [JsonPropertyName("overallStrategy")]
    public string OverallStrategy { get; set; } = string.Empty;

    [JsonPropertyName("suggestedSkills")]
    public List<SuggestedSkill> SuggestedSkills { get; set; } = new();
}

/// <summary>
/// A single AI-suggested skill
/// </summary>
public class SuggestedSkill
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Technical";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Want"; // Have or Want

    [JsonPropertyName("recommendedProficiency")]
    public int RecommendedProficiency { get; set; } = 3;

    [JsonPropertyName("impactLevel")]
    public string ImpactLevel { get; set; } = "Medium";

    [JsonPropertyName("learningPath")]
    public string? LearningPath { get; set; }
}

/// <summary>
/// Result of AI goal suggestions for a skill
/// </summary>
public class GoalSuggestionForSkillResult
{
    [JsonPropertyName("skillName")]
    public string SkillName { get; set; } = string.Empty;

    [JsonPropertyName("matchedGoals")]
    public List<SkillGoalMatch> MatchedGoals { get; set; } = new();

    [JsonPropertyName("suggestedNewGoals")]
    public List<SuggestedGoalForSkill> SuggestedNewGoals { get; set; } = new();
}

/// <summary>
/// A matched existing goal for a skill
/// </summary>
public class SkillGoalMatch
{
    [JsonPropertyName("goalId")]
    public string GoalId { get; set; } = string.Empty;

    [JsonPropertyName("relevanceScore")]
    public int RelevanceScore { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// A suggested new goal based on a skill
/// </summary>
public class SuggestedGoalForSkill
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
    public string Category { get; set; } = "Learning";
}

/// <summary>
/// Result of AI habit suggestions for developing a skill
/// </summary>
public class HabitSuggestionForSkillResult
{
    [JsonPropertyName("skillName")]
    public string SkillName { get; set; } = string.Empty;

    [JsonPropertyName("currentProficiency")]
    public int CurrentProficiency { get; set; }

    [JsonPropertyName("targetProficiency")]
    public int TargetProficiency { get; set; }

    [JsonPropertyName("developmentStrategy")]
    public string DevelopmentStrategy { get; set; } = string.Empty;

    [JsonPropertyName("suggestedHabits")]
    public List<SuggestedHabitForSkill> SuggestedHabits { get; set; } = new();
}

/// <summary>
/// A suggested habit for developing a skill
/// </summary>
public class SuggestedHabitForSkill
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

    [JsonPropertyName("startSmallTip")]
    public string? StartSmallTip { get; set; }
}

/// <summary>
/// Implementation of skill AI service using LLM
/// </summary>
public class SkillAiService : ISkillAiService
{
    private readonly ILlmService _llmService;

    public SkillAiService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _llmService.IsAvailableAsync();
    }

    public async Task<SkillSuggestionResult?> GenerateSkillsForGoalAsync(Goal goal)
    {
        var goalContext = BuildGoalContext(goal);
        var prompt = string.Format(SkillGenerationPrompt, goalContext);

        return await _llmService.GenerateStructuredAsync<SkillSuggestionResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 120
        });
    }

    public async Task<GoalSuggestionForSkillResult?> SuggestGoalsForSkillAsync(Skill skill, IReadOnlyList<Goal> existingGoals)
    {
        var skillContext = BuildSkillContextWithGoals(skill, existingGoals);
        var prompt = string.Format(GoalSuggestionForSkillPrompt, skillContext);

        return await _llmService.GenerateStructuredAsync<GoalSuggestionForSkillResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 120
        });
    }

    public async Task<HabitSuggestionForSkillResult?> SuggestHabitsForSkillAsync(Skill skill)
    {
        var skillContext = BuildSkillContext(skill);
        var prompt = string.Format(HabitSuggestionForSkillPrompt, skillContext);

        return await _llmService.GenerateStructuredAsync<HabitSuggestionForSkillResult>(prompt, new LlmOptions
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

    private static string BuildSkillContext(Skill skill)
    {
        var parts = new List<string>
        {
            $"Skill: {skill.Name}",
            $"Category: {skill.Category}",
            $"Type: {(skill.Type == SkillType.Have ? "Currently have" : "Want to develop")}",
            $"Current Proficiency: {skill.CurrentProficiency}/5 ({Skill.GetProficiencyName(skill.CurrentProficiency)})",
            $"Target Proficiency: {skill.TargetProficiency}/5 ({Skill.GetProficiencyName(skill.TargetProficiency)})"
        };

        if (!string.IsNullOrEmpty(skill.Description))
            parts.Add($"Description: {skill.Description}");

        if (!string.IsNullOrEmpty(skill.Notes))
            parts.Add($"Notes: {skill.Notes}");

        if (skill.TargetDate.HasValue)
            parts.Add($"Target Date: {skill.TargetDate.Value:MMMM d, yyyy}");

        return string.Join("\n", parts);
    }

    private static string BuildSkillContextWithGoals(Skill skill, IReadOnlyList<Goal> existingGoals)
    {
        var parts = new List<string>
        {
            BuildSkillContext(skill)
        };

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

    private const string SkillGenerationPrompt = @"You are a career development and learning expert. Your task is to suggest skills that would help someone achieve their goal.

Consider skills across these categories:
- Technical: Programming, engineering, technical knowledge
- SoftSkills: Communication, leadership, collaboration
- Creative: Design, writing, artistic abilities
- DomainKnowledge: Industry expertise, subject matter knowledge
- ToolsSoftware: Specific tools, platforms, software proficiency

Focus on skills that are:
1. Directly relevant to achieving the goal
2. Transferable and valuable beyond this specific goal
3. Learnable within a reasonable timeframe
4. Complementary to each other (skills that build on one another)

Respond with a JSON object:
{{
    ""goalTitle"": ""the goal title"",
    ""overallStrategy"": ""brief explanation of how these skills support the goal"",
    ""suggestedSkills"": [
        {{
            ""name"": ""clear, specific skill name"",
            ""description"": ""what this skill encompasses"",
            ""rationale"": ""why this skill is important for the goal"",
            ""category"": ""Technical|SoftSkills|Creative|DomainKnowledge|ToolsSoftware"",
            ""type"": ""Want"",
            ""recommendedProficiency"": 3,
            ""impactLevel"": ""High|Medium|Low"",
            ""learningPath"": ""suggested approach to develop this skill""
        }}
    ]
}}

Suggest 3-5 skills, ordered by impact level (highest first).
Include a mix of skill categories when appropriate.
Consider both hard and soft skills.

Goal Information:
{0}";

    private const string GoalSuggestionForSkillPrompt = @"You are a goal-setting coach. Analyze this skill and identify which existing goals it supports, and suggest potential new goals for developing this skill.

For existing goals, evaluate how relevant this skill is to achieving that goal (1-10 relevance score).
For new goal suggestions, think about practical applications and milestones for this skill.

Respond with a JSON object:
{{
    ""skillName"": ""the skill name"",
    ""matchedGoals"": [
        {{
            ""goalId"": ""the goal ID from the list"",
            ""relevanceScore"": 8,
            ""explanation"": ""how this skill directly supports this goal""
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
Suggest 0-2 new goals that focus on applying or advancing this skill.

Skill Information:
{0}";

    private const string HabitSuggestionForSkillPrompt = @"You are a learning and skill development expert. Your task is to suggest daily/weekly habits that will help someone develop their skill from current proficiency to target proficiency.

Focus on habits that are:
1. Small and sustainable (2-minute rule friendly to start)
2. Evidence-based for skill acquisition (deliberate practice)
3. Progressive (can scale up as proficiency grows)
4. Measurable and trackable

Respond with a JSON object:
{{
    ""skillName"": ""the skill name"",
    ""currentProficiency"": 1,
    ""targetProficiency"": 5,
    ""developmentStrategy"": ""brief explanation of the learning approach"",
    ""suggestedHabits"": [
        {{
            ""name"": ""short, action-oriented habit name"",
            ""description"": ""what specifically to do"",
            ""rationale"": ""why this habit develops the skill (be specific about the learning mechanism)"",
            ""frequency"": ""Daily|Weekdays|Weekly"",
            ""targetCount"": 1,
            ""preferredTimeOfDay"": ""Morning|Afternoon|Evening|Anytime"",
            ""category"": ""Learning|Practice|Review|Application"",
            ""startSmallTip"": ""how to make this habit tiny to start""
        }}
    ]
}}

Suggest 3-5 habits that cover:
- Active learning (acquiring new knowledge)
- Deliberate practice (applying and refining)
- Review and reflection (consolidating learning)

Consider the gap between current and target proficiency when suggesting intensity.

Skill Information:
{0}";
}
