using System.Text.Json.Serialization;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

public interface ICareerAiService
{
    Task<bool> IsAvailableAsync();
    Task<CareerPlanImprovementResult?> ImprovePlanAsync(CareerPlan plan);
    Task<MilestoneGenerationResult?> GenerateMilestonesAsync(CareerPlan plan);
}

public class CareerPlanImprovementResult
{
    [JsonPropertyName("gapAnalysis")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> GapAnalysisList { get; set; } = new();

    [JsonPropertyName("strengths")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> Strengths { get; set; } = new();

    [JsonPropertyName("risks")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> Risks { get; set; } = new();

    [JsonPropertyName("coachingQuestions")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> CoachingQuestions { get; set; } = new();

    [JsonPropertyName("suggestedSkillAreas")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> SuggestedSkillAreas { get; set; } = new();

    [JsonPropertyName("timelineAssessment")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> TimelineAssessmentList { get; set; } = new();

    [JsonIgnore]
    public string GapAnalysis => string.Join("\n", GapAnalysisList);

    [JsonIgnore]
    public string TimelineAssessment => string.Join("\n", TimelineAssessmentList);
}

public class MilestoneGenerationResult
{
    [JsonPropertyName("suggestedMilestones")]
    public List<SuggestedMilestone> SuggestedMilestones { get; set; } = new();
}

public class SuggestedMilestone
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Other";

    [JsonPropertyName("targetMonthsFromNow")]
    public int TargetMonthsFromNow { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;
}

public class CareerAiService : ICareerAiService
{
    private readonly ILlmService _llmService;

    public CareerAiService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _llmService.IsAvailableAsync();
    }

    public async Task<CareerPlanImprovementResult?> ImprovePlanAsync(CareerPlan plan)
    {
        var context = BuildPlanContext(plan);
        var prompt = string.Format(CoachingPrompt, context);

        return await _llmService.GenerateStructuredAsync<CareerPlanImprovementResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 2048,
            TimeoutSeconds = 180
        });
    }

    public async Task<MilestoneGenerationResult?> GenerateMilestonesAsync(CareerPlan plan)
    {
        var context = BuildPlanContext(plan);
        var prompt = string.Format(MilestonePrompt, context);

        return await _llmService.GenerateStructuredAsync<MilestoneGenerationResult>(prompt, new LlmOptions
        {
            Temperature = 0.7,
            MaxTokens = 4096,
            TimeoutSeconds = 180
        });
    }

    private static string BuildPlanContext(CareerPlan plan)
    {
        var parts = new List<string>
        {
            $"Career Plan: {plan.Title}"
        };

        if (!string.IsNullOrEmpty(plan.Description))
            parts.Add($"Description: {plan.Description}");

        if (!string.IsNullOrEmpty(plan.CurrentRole))
            parts.Add($"Current Role: {plan.CurrentRole}");

        if (!string.IsNullOrEmpty(plan.TargetRole))
            parts.Add($"Target Role: {plan.TargetRole}");

        parts.Add($"Status: {plan.Status}");

        if (plan.StartDate.HasValue)
            parts.Add($"Start Date: {plan.StartDate.Value:MMMM d, yyyy}");

        if (plan.TargetDate.HasValue)
            parts.Add($"Target Date: {plan.TargetDate.Value:MMMM d, yyyy}");

        if (plan.Milestones.Count > 0)
        {
            parts.Add("");
            parts.Add("Existing Milestones:");
            foreach (var m in plan.Milestones.OrderBy(m => m.SortOrder))
            {
                var dateStr = m.TargetDate.HasValue ? $" (target: {m.TargetDate.Value:MMMM yyyy})" : "";
                parts.Add($"- [{m.Category}] {m.Title} - {m.Status}{dateStr}");
            }
        }

        if (!string.IsNullOrEmpty(plan.Notes))
        {
            parts.Add("");
            parts.Add($"Notes: {plan.Notes}");
        }

        return string.Join("\n", parts);
    }

    private const string CoachingPrompt = @"You are an experienced career coach helping a professional plan their career growth. Analyze the following career plan and provide structured coaching feedback.

Focus on:
1. Gap analysis between current and target roles
2. Identifying strengths in the current plan
3. Risks or blind spots that could derail progress
4. Thought-provoking coaching questions to refine the plan
5. Skill areas that should be developed
6. Whether the timeline is realistic

Respond with a JSON object:
{{
    ""gapAnalysis"": ""detailed analysis of the gap between current role and target role, including what skills, experience, and accomplishments are typically needed"",
    ""strengths"": [""specific strengths or good decisions in the current plan""],
    ""risks"": [""potential risks, blind spots, or challenges to anticipate""],
    ""coachingQuestions"": [""thought-provoking questions to help refine the career plan""],
    ""suggestedSkillAreas"": [""specific skill areas to develop for the target role""],
    ""timelineAssessment"": ""assessment of whether the timeline is realistic and suggestions for adjustment""
}}

Provide 3-5 items for each list. Be specific and actionable, not generic.

Career Plan Information:
{0}";

    private const string MilestonePrompt = @"You are an experienced career coach helping a professional create actionable milestones for their career growth plan.

Given the career plan information below, generate 5-8 concrete milestones that will help this person progress from their current role to their target role. Each milestone should be:
1. Specific and measurable
2. Categorized appropriately (Role, Certification, Skill, Project, Education, Networking, Leadership, or Other)
3. Given a realistic timeline in months from now
4. Accompanied by a brief rationale explaining why this milestone matters

Consider the existing milestones (if any) and avoid duplicating them. Fill gaps in the plan.

Respond with a JSON object:
{{
    ""suggestedMilestones"": [
        {{
            ""title"": ""specific, actionable milestone title"",
            ""description"": ""what achieving this milestone looks like"",
            ""category"": ""one of: Role, Certification, Skill, Project, Education, Networking, Leadership, Other"",
            ""targetMonthsFromNow"": 3,
            ""rationale"": ""why this milestone is important for reaching the target role""
        }}
    ]
}}

Distribute milestones across different categories for a well-rounded plan. Order them chronologically by targetMonthsFromNow.

Career Plan Information:
{0}";
}
