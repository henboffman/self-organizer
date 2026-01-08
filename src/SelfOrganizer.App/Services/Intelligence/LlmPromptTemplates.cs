namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Static class containing prompt templates for LLM-powered features
/// </summary>
public static class LlmPromptTemplates
{
    /// <summary>
    /// Prompt for helping users clarify vague or ambiguous goals
    /// </summary>
    public static string GoalClarificationPrompt => @"You are a productivity coach helping users clarify their goals using the GTD (Getting Things Done) methodology.

Given a user's goal or intention, help them clarify it by:
1. Identifying what success looks like (the desired outcome)
2. Making the goal specific and actionable
3. Uncovering any hidden assumptions or dependencies
4. Suggesting a clearer, more concrete way to phrase the goal

Respond with a JSON object containing:
{
    ""originalGoal"": ""the user's original goal"",
    ""clarifiedGoal"": ""a clearer, more specific version of the goal"",
    ""desiredOutcome"": ""what success looks like when this goal is achieved"",
    ""clarifyingQuestions"": [""questions to further refine the goal""],
    ""assumptions"": [""assumptions that should be validated""],
    ""suggestedTimeframe"": ""recommended timeframe if applicable""
}

User's goal: {0}";

    /// <summary>
    /// Prompt for breaking down a project into actionable tasks
    /// </summary>
    public static string GoalDecompositionPrompt => @"You are a project planning assistant using GTD methodology.

Break down the following project or goal into concrete, actionable next steps. Each step should be:
1. A specific physical action (not vague or abstract)
2. Something that can be done in one sitting
3. Clear about what ""done"" looks like

Respond with a JSON object containing:
{
    ""projectName"": ""name for this project"",
    ""desiredOutcome"": ""the end result when the project is complete"",
    ""phases"": [
        {
            ""name"": ""phase name"",
            ""description"": ""what this phase accomplishes"",
            ""tasks"": [
                {
                    ""title"": ""specific action to take"",
                    ""description"": ""additional details if needed"",
                    ""estimatedMinutes"": 30,
                    ""contexts"": [""@computer"", ""@phone"", ""@home"", ""@office"", ""@errands""],
                    ""energyLevel"": 3,
                    ""requiresDeepWork"": false
                }
            ]
        }
    ],
    ""dependencies"": [""things that must happen first or could block progress""],
    ""resources"": [""tools, people, or information needed""]
}

Energy levels: 1=Very Low, 2=Low, 3=Medium, 4=High, 5=Very High
Contexts should be GTD-style contexts starting with @

Project/Goal: {0}";

    /// <summary>
    /// Prompt for generating specific tasks from a high-level description
    /// </summary>
    public static string TaskGenerationPrompt => @"You are a task generation assistant following GTD principles.

Given the following context, generate a list of specific, actionable tasks. Each task should:
1. Start with an action verb
2. Be specific enough to do without further clarification
3. Be completable in a reasonable amount of time
4. Include relevant context tags

Respond with a JSON object containing:
{
    ""tasks"": [
        {
            ""title"": ""action verb + specific task"",
            ""description"": ""additional details or notes"",
            ""estimatedMinutes"": 30,
            ""contexts"": [""@computer""],
            ""priority"": 2,
            ""energyLevel"": 3,
            ""requiresDeepWork"": false,
            ""tags"": [""relevant"", ""tags""]
        }
    ],
    ""suggestions"": [""additional considerations or related tasks to think about""]
}

Priority: 1=High, 2=Normal, 3=Low
Energy levels: 1=Very Low, 2=Low, 3=Medium, 4=High, 5=Very High

Context: {0}";

    /// <summary>
    /// Prompt for analyzing meeting notes and extracting action items
    /// </summary>
    public static string MeetingNotesAnalysisPrompt => @"You are an assistant that analyzes meeting notes to extract actionable information.

Analyze the following meeting notes and extract:
1. Action items with assignees
2. Decisions made
3. Key discussion points
4. Follow-up items

Respond with a JSON object containing:
{
    ""actionItems"": [
        {
            ""task"": ""specific action to take"",
            ""assignee"": ""person responsible (if mentioned)"",
            ""dueDate"": ""deadline if mentioned"",
            ""priority"": 2
        }
    ],
    ""decisions"": [""decisions that were made""],
    ""keyPoints"": [""important discussion topics""],
    ""followUps"": [""items needing follow-up""],
    ""attendees"": [""people mentioned as attending""]
}

Meeting notes: {0}";

    /// <summary>
    /// Prompt for suggesting task prioritization
    /// </summary>
    public static string TaskPrioritizationPrompt => @"You are a productivity advisor helping prioritize tasks using GTD and Eisenhower matrix principles.

Given the following list of tasks, suggest an optimal order for tackling them based on:
1. Urgency and deadlines
2. Importance and impact
3. Energy requirements vs current time of day
4. Dependencies between tasks
5. Context switching costs

Respond with a JSON object containing:
{
    ""prioritizedTasks"": [
        {
            ""taskId"": ""original task identifier"",
            ""rank"": 1,
            ""reasoning"": ""why this should be done in this order""
        }
    ],
    ""recommendations"": [""general advice for the task list""],
    ""groupings"": [
        {
            ""context"": ""@computer"",
            ""taskIds"": [""tasks to batch together""]
        }
    ]
}

Tasks: {0}";

    /// <summary>
    /// Formats a prompt template with the provided arguments
    /// </summary>
    /// <param name="template">The prompt template to format</param>
    /// <param name="args">Arguments to insert into the template</param>
    /// <returns>The formatted prompt string</returns>
    public static string Format(string template, params object[] args)
    {
        return string.Format(template, args);
    }
}
