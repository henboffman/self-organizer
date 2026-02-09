using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for parsing and executing natural language commands.
/// Supports commands like "create task buy groceries", "show tasks due today", etc.
/// </summary>
public interface INaturalLanguageCommandService
{
    /// <summary>
    /// Parses a natural language command and returns the parsed intent.
    /// </summary>
    Task<CommandParseResult> ParseCommandAsync(string input);

    /// <summary>
    /// Executes a parsed command and returns the result.
    /// </summary>
    Task<CommandExecutionResult> ExecuteCommandAsync(ParsedCommand command);

    /// <summary>
    /// Parses and executes a natural language command in one step.
    /// </summary>
    Task<CommandExecutionResult> ProcessCommandAsync(string input);

    /// <summary>
    /// Gets command suggestions based on partial input.
    /// </summary>
    Task<IEnumerable<CommandSuggestion>> GetSuggestionsAsync(string partialInput);

    /// <summary>
    /// Gets all available command patterns for help/documentation.
    /// </summary>
    IEnumerable<CommandPattern> GetAvailableCommands();
}

/// <summary>
/// The type of intent recognized from a natural language command.
/// </summary>
public enum CommandIntent
{
    Unknown,

    // Navigation intents
    Navigate,           // "go to tasks", "open projects", "show calendar"

    // Creation intents
    CreateTask,         // "create task", "add task", "new task"
    CreateProject,      // "create project", "new project"
    CreateGoal,         // "create goal", "new goal"
    CreateEvent,        // "create event", "schedule meeting"
    Capture,            // "capture", "quick add", "jot down"

    // Query intents
    Search,             // "find", "search for", "show me"
    ListTasks,          // "list tasks", "show tasks"
    ListProjects,       // "list projects"
    ListGoals,          // "list goals"

    // Modification intents
    CompleteTask,       // "complete task", "done with", "finish"
    UpdateTask,         // "update task", "change task"
    ScheduleTask,       // "schedule task for"
    SetPriority,        // "set priority to"
    AddTag,             // "tag task with"
    LinkToProject,      // "link to project"
    LinkToGoal,         // "link to goal"

    // Deletion intents
    DeleteTask,         // "delete task", "remove task"
    ArchiveProject,     // "archive project"

    // Timer/Focus intents
    StartFocus,         // "start focus", "focus on"
    StopFocus,          // "stop focus", "end focus"

    // Utility intents
    Undo,               // "undo"
    Redo,               // "redo"
    Help,               // "help", "what can I do"

    // Export intents
    Export              // "export tasks", "download"
}

/// <summary>
/// Result of parsing a natural language command.
/// </summary>
public class CommandParseResult
{
    public bool Success { get; set; }
    public ParsedCommand? Command { get; set; }
    public string? Error { get; set; }
    public double Confidence { get; set; }
    public List<CommandSuggestion> AlternativeSuggestions { get; set; } = new();
}

/// <summary>
/// A parsed command ready for execution.
/// </summary>
public class ParsedCommand
{
    public CommandIntent Intent { get; set; }
    public string OriginalInput { get; set; } = string.Empty;

    /// <summary>
    /// The main subject/object of the command (e.g., task title, project name).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Extracted parameters from the command.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Entity ID if the command references a specific entity.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Entity type if known (task, project, goal, event, etc.)
    /// </summary>
    public string? EntityType { get; set; }
}

/// <summary>
/// Result of executing a command.
/// </summary>
public class CommandExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NavigationUrl { get; set; }
    public object? ResultData { get; set; }
    public string? Error { get; set; }
    public CommandIntent ExecutedIntent { get; set; }
}

/// <summary>
/// A command suggestion for auto-complete.
/// </summary>
public class CommandSuggestion
{
    public string DisplayText { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public CommandIntent Intent { get; set; }
    public string? Description { get; set; }
    public string Icon { get; set; } = "oi-terminal";
    public double Relevance { get; set; }
}

/// <summary>
/// A command pattern for documentation.
/// </summary>
public class CommandPattern
{
    public CommandIntent Intent { get; set; }
    public string[] Patterns { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
    public string[] Examples { get; set; } = Array.Empty<string>();
    public string Category { get; set; } = "General";
}
