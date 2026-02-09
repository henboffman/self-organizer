using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Service for parsing and executing natural language commands.
/// Uses pattern matching and entity extraction to understand user intent.
/// </summary>
public class NaturalLanguageCommandService : INaturalLanguageCommandService
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IGoalService _goalService;
    private readonly ICaptureService _captureService;
    private readonly ISearchService _searchService;
    private readonly ICommandHistory _commandHistory;
    private readonly ILlmService _llmService;
    private readonly NavigationManager _navigationManager;

    private static readonly List<CommandPattern> _commandPatterns = new()
    {
        // Navigation commands
        new CommandPattern
        {
            Intent = CommandIntent.Navigate,
            Patterns = new[] { @"^(?:go\s+to|open|show|view)\s+(.+)$" },
            Description = "Navigate to a page or section",
            Examples = new[] { "go to tasks", "open projects", "show calendar", "view goals" },
            Category = "Navigation"
        },

        // Create task commands
        new CommandPattern
        {
            Intent = CommandIntent.CreateTask,
            Patterns = new[] {
                @"^(?:create|add|new|make)\s+(?:a\s+)?task\s+(.+)$",
                @"^task[:\s]+(.+)$"
            },
            Description = "Create a new task",
            Examples = new[] { "create task buy groceries", "add task call mom", "new task review report" },
            Category = "Creation"
        },

        // Create project commands
        new CommandPattern
        {
            Intent = CommandIntent.CreateProject,
            Patterns = new[] {
                @"^(?:create|add|new|make)\s+(?:a\s+)?project\s+(.+)$",
                @"^project[:\s]+(.+)$"
            },
            Description = "Create a new project",
            Examples = new[] { "create project website redesign", "new project Q1 planning" },
            Category = "Creation"
        },

        // Create goal commands
        new CommandPattern
        {
            Intent = CommandIntent.CreateGoal,
            Patterns = new[] {
                @"^(?:create|add|new|make)\s+(?:a\s+)?goal\s+(.+)$",
                @"^goal[:\s]+(.+)$"
            },
            Description = "Create a new goal",
            Examples = new[] { "create goal lose 10 pounds", "new goal learn Spanish" },
            Category = "Creation"
        },

        // Capture/quick add commands
        new CommandPattern
        {
            Intent = CommandIntent.Capture,
            Patterns = new[] {
                @"^(?:capture|quick\s+add|jot|remember)\s+(.+)$",
                @"^[\+!]\s*(.+)$"
            },
            Description = "Quick capture to inbox",
            Examples = new[] { "capture meeting idea", "quick add call dentist", "+ pick up dry cleaning" },
            Category = "Creation"
        },

        // Search/query commands
        new CommandPattern
        {
            Intent = CommandIntent.Search,
            Patterns = new[] {
                @"^(?:find|search|search\s+for|look\s+for)\s+(.+)$",
                @"^(?:show\s+me|what|where)\s+(.+)$"
            },
            Description = "Search for items",
            Examples = new[] { "find tasks about groceries", "search for project meeting", "show me tasks from last week" },
            Category = "Query"
        },

        // List tasks commands
        new CommandPattern
        {
            Intent = CommandIntent.ListTasks,
            Patterns = new[] {
                @"^(?:list|show|get)\s+(?:all\s+)?(?:my\s+)?tasks(?:\s+(.+))?$",
                @"^tasks(?:\s+(.+))?$",
                @"^(?:what\s+are\s+)?my\s+tasks(?:\s+(.+))?$"
            },
            Description = "List tasks with optional filters",
            Examples = new[] { "list tasks", "show tasks due today", "tasks for this week", "my tasks completed yesterday" },
            Category = "Query"
        },

        // List projects commands
        new CommandPattern
        {
            Intent = CommandIntent.ListProjects,
            Patterns = new[] {
                @"^(?:list|show|get)\s+(?:all\s+)?(?:my\s+)?projects(?:\s+(.+))?$",
                @"^projects(?:\s+(.+))?$"
            },
            Description = "List projects",
            Examples = new[] { "list projects", "show active projects" },
            Category = "Query"
        },

        // Complete task commands
        new CommandPattern
        {
            Intent = CommandIntent.CompleteTask,
            Patterns = new[] {
                @"^(?:complete|done|finish|mark\s+done|mark\s+complete)\s+(?:task\s+)?(.+)$",
                @"^(?:i\s+)?(?:did|finished|completed)\s+(.+)$"
            },
            Description = "Mark a task as complete",
            Examples = new[] { "complete task buy groceries", "done with report", "finished the presentation" },
            Category = "Action"
        },

        // Schedule task commands
        new CommandPattern
        {
            Intent = CommandIntent.ScheduleTask,
            Patterns = new[] {
                @"^schedule\s+(?:task\s+)?(.+?)\s+(?:for|on|to)\s+(.+)$",
                @"^(?:move|reschedule)\s+(?:task\s+)?(.+?)\s+(?:to|for)\s+(.+)$"
            },
            Description = "Schedule a task for a date",
            Examples = new[] { "schedule task report for tomorrow", "move meeting prep to Friday" },
            Category = "Action"
        },

        // Set priority commands
        new CommandPattern
        {
            Intent = CommandIntent.SetPriority,
            Patterns = new[] {
                @"^(?:set|change)\s+priority\s+(?:of\s+)?(.+?)\s+to\s+(high|medium|low|\d+)$",
                @"^(?:prioritize|make)\s+(.+?)\s+(high|medium|low)\s+priority$"
            },
            Description = "Set task priority",
            Examples = new[] { "set priority of report to high", "make groceries low priority" },
            Category = "Action"
        },

        // Delete commands
        new CommandPattern
        {
            Intent = CommandIntent.DeleteTask,
            Patterns = new[] {
                @"^(?:delete|remove)\s+(?:task\s+)?(.+)$"
            },
            Description = "Delete a task",
            Examples = new[] { "delete task old item", "remove duplicate entry" },
            Category = "Action"
        },

        // Focus/timer commands
        new CommandPattern
        {
            Intent = CommandIntent.StartFocus,
            Patterns = new[] {
                @"^(?:start\s+)?focus(?:\s+on)?\s+(.+)$",
                @"^(?:work\s+on|pomodoro)\s+(.+)$"
            },
            Description = "Start a focus session",
            Examples = new[] { "focus on report", "start focus writing", "work on presentation" },
            Category = "Focus"
        },

        new CommandPattern
        {
            Intent = CommandIntent.StopFocus,
            Patterns = new[] {
                @"^(?:stop|end|finish)\s+focus$",
                @"^(?:stop|end)\s+(?:working|timer)$"
            },
            Description = "Stop the focus timer",
            Examples = new[] { "stop focus", "end timer" },
            Category = "Focus"
        },

        // Undo/Redo commands
        new CommandPattern
        {
            Intent = CommandIntent.Undo,
            Patterns = new[] { @"^undo$" },
            Description = "Undo the last action",
            Examples = new[] { "undo" },
            Category = "Utility"
        },

        new CommandPattern
        {
            Intent = CommandIntent.Redo,
            Patterns = new[] { @"^redo$" },
            Description = "Redo the last undone action",
            Examples = new[] { "redo" },
            Category = "Utility"
        },

        // Help commands
        new CommandPattern
        {
            Intent = CommandIntent.Help,
            Patterns = new[] {
                @"^(?:help|what\s+can\s+(?:i|you)\s+do|\?)$",
                @"^(?:show\s+)?commands$"
            },
            Description = "Show available commands",
            Examples = new[] { "help", "what can I do", "commands" },
            Category = "Utility"
        },

        // Export commands
        new CommandPattern
        {
            Intent = CommandIntent.Export,
            Patterns = new[] {
                @"^export\s+(.+?)(?:\s+(?:as|to)\s+(csv|json|markdown))?$",
                @"^download\s+(.+)$"
            },
            Description = "Export data",
            Examples = new[] { "export tasks", "export projects as csv" },
            Category = "Utility"
        }
    };

    // Navigation mappings for the Navigate intent
    private static readonly Dictionary<string, string> _navigationMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "tasks", "tasks" },
        { "task list", "tasks" },
        { "projects", "projects" },
        { "project list", "projects" },
        { "calendar", "calendar" },
        { "goals", "goals" },
        { "goal list", "goals" },
        { "inbox", "inbox" },
        { "capture", "capture" },
        { "home", "" },
        { "dashboard", "" },
        { "settings", "settings" },
        { "review", "review/daily" },
        { "daily review", "review/daily" },
        { "weekly review", "review/weekly" },
        { "reports", "reports" },
        { "focus", "focus" },
        { "timer", "focus" },
        { "ideas", "ideas" },
        { "reference", "reference" },
        { "tags", "tags" },
        { "contexts", "contexts" },
        { "habits", "habits" },
        { "balance", "reports/balance" }
    };

    // Date parsing patterns
    private static readonly Dictionary<string, Func<DateTime>> _relativeDates = new(StringComparer.OrdinalIgnoreCase)
    {
        { "today", () => DateTime.Today },
        { "tomorrow", () => DateTime.Today.AddDays(1) },
        { "yesterday", () => DateTime.Today.AddDays(-1) },
        { "this week", () => DateTime.Today },
        { "next week", () => DateTime.Today.AddDays(7) },
        { "this month", () => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) },
        { "next month", () => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1) },
        { "monday", () => GetNextDayOfWeek(DayOfWeek.Monday) },
        { "tuesday", () => GetNextDayOfWeek(DayOfWeek.Tuesday) },
        { "wednesday", () => GetNextDayOfWeek(DayOfWeek.Wednesday) },
        { "thursday", () => GetNextDayOfWeek(DayOfWeek.Thursday) },
        { "friday", () => GetNextDayOfWeek(DayOfWeek.Friday) },
        { "saturday", () => GetNextDayOfWeek(DayOfWeek.Saturday) },
        { "sunday", () => GetNextDayOfWeek(DayOfWeek.Sunday) }
    };

    public NaturalLanguageCommandService(
        ITaskService taskService,
        IProjectService projectService,
        IGoalService goalService,
        ICaptureService captureService,
        ISearchService searchService,
        ICommandHistory commandHistory,
        ILlmService llmService,
        NavigationManager navigationManager)
    {
        _taskService = taskService;
        _projectService = projectService;
        _goalService = goalService;
        _captureService = captureService;
        _searchService = searchService;
        _commandHistory = commandHistory;
        _llmService = llmService;
        _navigationManager = navigationManager;
    }

    public async Task<CommandParseResult> ParseCommandAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new CommandParseResult
            {
                Success = false,
                Error = "Please enter a command"
            };
        }

        var normalizedInput = input.Trim().ToLowerInvariant();

        // Try pattern matching first
        foreach (var pattern in _commandPatterns)
        {
            foreach (var regexPattern in pattern.Patterns)
            {
                var match = Regex.Match(normalizedInput, regexPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var command = new ParsedCommand
                    {
                        Intent = pattern.Intent,
                        OriginalInput = input
                    };

                    // Extract captured groups as parameters
                    if (match.Groups.Count > 1)
                    {
                        command.Subject = match.Groups[1].Value.Trim();
                    }

                    if (match.Groups.Count > 2)
                    {
                        command.Parameters["secondary"] = match.Groups[2].Value.Trim();
                    }

                    // Process intent-specific parameters
                    await EnrichCommandAsync(command);

                    return new CommandParseResult
                    {
                        Success = true,
                        Command = command,
                        Confidence = 0.9
                    };
                }
            }
        }

        // If no pattern matches, try LLM-based parsing as fallback
        if (await _llmService.IsAvailableAsync())
        {
            return await ParseWithLlmAsync(input);
        }

        // Return suggestions for what the user might have meant
        var suggestions = await GetSuggestionsAsync(input);
        return new CommandParseResult
        {
            Success = false,
            Error = "I didn't understand that command. Did you mean one of these?",
            AlternativeSuggestions = suggestions.ToList(),
            Confidence = 0.0
        };
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(ParsedCommand command)
    {
        try
        {
            return command.Intent switch
            {
                CommandIntent.Navigate => ExecuteNavigate(command),
                CommandIntent.CreateTask => await ExecuteCreateTask(command),
                CommandIntent.CreateProject => await ExecuteCreateProject(command),
                CommandIntent.CreateGoal => await ExecuteCreateGoal(command),
                CommandIntent.Capture => await ExecuteCapture(command),
                CommandIntent.Search => await ExecuteSearch(command),
                CommandIntent.ListTasks => await ExecuteListTasks(command),
                CommandIntent.ListProjects => await ExecuteListProjects(command),
                CommandIntent.CompleteTask => await ExecuteCompleteTask(command),
                CommandIntent.ScheduleTask => await ExecuteScheduleTask(command),
                CommandIntent.SetPriority => await ExecuteSetPriority(command),
                CommandIntent.DeleteTask => await ExecuteDeleteTask(command),
                CommandIntent.StartFocus => ExecuteStartFocus(command),
                CommandIntent.StopFocus => ExecuteStopFocus(command),
                CommandIntent.Undo => await ExecuteUndo(command),
                CommandIntent.Redo => await ExecuteRedo(command),
                CommandIntent.Help => ExecuteHelp(command),
                CommandIntent.Export => await ExecuteExport(command),
                _ => new CommandExecutionResult
                {
                    Success = false,
                    Error = $"Unknown command intent: {command.Intent}",
                    ExecutedIntent = command.Intent
                }
            };
        }
        catch (Exception ex)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = $"Error executing command: {ex.Message}",
                ExecutedIntent = command.Intent
            };
        }
    }

    public async Task<CommandExecutionResult> ProcessCommandAsync(string input)
    {
        var parseResult = await ParseCommandAsync(input);

        if (!parseResult.Success || parseResult.Command == null)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = parseResult.Error ?? "Failed to parse command",
                ResultData = parseResult.AlternativeSuggestions
            };
        }

        return await ExecuteCommandAsync(parseResult.Command);
    }

    public async Task<IEnumerable<CommandSuggestion>> GetSuggestionsAsync(string partialInput)
    {
        var suggestions = new List<CommandSuggestion>();
        var normalizedInput = partialInput.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedInput))
        {
            // Return common commands when input is empty
            suggestions.AddRange(new[]
            {
                new CommandSuggestion { DisplayText = "Create task...", Command = "create task ", Intent = CommandIntent.CreateTask, Icon = "oi-plus", Relevance = 1.0 },
                new CommandSuggestion { DisplayText = "Show tasks", Command = "list tasks", Intent = CommandIntent.ListTasks, Icon = "oi-task", Relevance = 0.9 },
                new CommandSuggestion { DisplayText = "Search...", Command = "find ", Intent = CommandIntent.Search, Icon = "oi-magnifying-glass", Relevance = 0.85 },
                new CommandSuggestion { DisplayText = "Go to...", Command = "go to ", Intent = CommandIntent.Navigate, Icon = "oi-arrow-right", Relevance = 0.8 },
                new CommandSuggestion { DisplayText = "Start focus", Command = "focus on ", Intent = CommandIntent.StartFocus, Icon = "oi-timer", Relevance = 0.75 },
                new CommandSuggestion { DisplayText = "Help", Command = "help", Intent = CommandIntent.Help, Icon = "oi-question-mark", Relevance = 0.5 }
            });
            return suggestions;
        }

        // Match against command patterns
        foreach (var pattern in _commandPatterns)
        {
            foreach (var example in pattern.Examples)
            {
                if (example.StartsWith(normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    LevenshteinDistance(normalizedInput, example.ToLowerInvariant().Split(' ')[0]) <= 2)
                {
                    suggestions.Add(new CommandSuggestion
                    {
                        DisplayText = example,
                        Command = example,
                        Intent = pattern.Intent,
                        Description = pattern.Description,
                        Icon = GetIconForIntent(pattern.Intent),
                        Relevance = CalculateRelevance(normalizedInput, example)
                    });
                }
            }
        }

        // Add task-specific suggestions if it looks like a task query
        if (normalizedInput.Contains("task") || normalizedInput.StartsWith("show") || normalizedInput.StartsWith("list"))
        {
            var tasks = await _taskService.GetNextActionsAsync();
            foreach (var task in tasks.Take(5))
            {
                suggestions.Add(new CommandSuggestion
                {
                    DisplayText = $"Complete: {task.Title}",
                    Command = $"complete task {task.Title}",
                    Intent = CommandIntent.CompleteTask,
                    Icon = "oi-check",
                    Relevance = 0.7
                });
            }
        }

        // Sort by relevance and return top suggestions
        return suggestions
            .OrderByDescending(s => s.Relevance)
            .Take(10);
    }

    public IEnumerable<CommandPattern> GetAvailableCommands()
    {
        return _commandPatterns;
    }

    #region Command Execution Methods

    private CommandExecutionResult ExecuteNavigate(ParsedCommand command)
    {
        var target = command.Subject?.ToLowerInvariant() ?? string.Empty;

        if (_navigationMappings.TryGetValue(target, out var url))
        {
            _navigationManager.NavigateTo(url);
            return new CommandExecutionResult
            {
                Success = true,
                Message = $"Navigating to {target}",
                NavigationUrl = url,
                ExecutedIntent = CommandIntent.Navigate
            };
        }

        // Try partial matching
        var partialMatch = _navigationMappings.Keys
            .FirstOrDefault(k => k.Contains(target) || target.Contains(k));

        if (partialMatch != null)
        {
            var matchedUrl = _navigationMappings[partialMatch];
            _navigationManager.NavigateTo(matchedUrl);
            return new CommandExecutionResult
            {
                Success = true,
                Message = $"Navigating to {partialMatch}",
                NavigationUrl = matchedUrl,
                ExecutedIntent = CommandIntent.Navigate
            };
        }

        return new CommandExecutionResult
        {
            Success = false,
            Error = $"Unknown navigation target: {target}. Try 'tasks', 'projects', 'calendar', 'goals', etc.",
            ExecutedIntent = CommandIntent.Navigate
        };
    }

    private async Task<CommandExecutionResult> ExecuteCreateTask(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify a task title",
                ExecutedIntent = CommandIntent.CreateTask
            };
        }

        var task = new TodoTask
        {
            Title = command.Subject,
            Status = TodoTaskStatus.NextAction
        };

        // Check for date in the title
        foreach (var (dateKeyword, dateFunc) in _relativeDates)
        {
            if (command.Subject.Contains(dateKeyword, StringComparison.OrdinalIgnoreCase))
            {
                task.DueDate = dateFunc();
                task.Title = command.Subject.Replace(dateKeyword, "", StringComparison.OrdinalIgnoreCase).Trim();
                break;
            }
        }

        // Check for priority keywords
        if (command.Subject.Contains("urgent", StringComparison.OrdinalIgnoreCase) ||
            command.Subject.Contains("important", StringComparison.OrdinalIgnoreCase))
        {
            task.Priority = 1;
        }

        var createdTask = await _taskService.CreateAsync(task);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Created task: {createdTask.Title}",
            NavigationUrl = $"tasks/{createdTask.Id}",
            ResultData = createdTask,
            ExecutedIntent = CommandIntent.CreateTask
        };
    }

    private async Task<CommandExecutionResult> ExecuteCreateProject(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify a project name",
                ExecutedIntent = CommandIntent.CreateProject
            };
        }

        var project = new Project
        {
            Name = command.Subject,
            Status = ProjectStatus.Active
        };

        var createdProject = await _projectService.CreateAsync(project);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Created project: {createdProject.Name}",
            NavigationUrl = $"projects/{createdProject.Id}",
            ResultData = createdProject,
            ExecutedIntent = CommandIntent.CreateProject
        };
    }

    private async Task<CommandExecutionResult> ExecuteCreateGoal(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify a goal title",
                ExecutedIntent = CommandIntent.CreateGoal
            };
        }

        var goal = new Goal
        {
            Title = command.Subject,
            Status = GoalStatus.Active,
            Timeframe = GoalTimeframe.Year
        };

        var createdGoal = await _goalService.CreateAsync(goal);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Created goal: {createdGoal.Title}",
            NavigationUrl = $"goals/{createdGoal.Id}",
            ResultData = createdGoal,
            ExecutedIntent = CommandIntent.CreateGoal
        };
    }

    private async Task<CommandExecutionResult> ExecuteCapture(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify what to capture",
                ExecutedIntent = CommandIntent.Capture
            };
        }

        var capture = await _captureService.CaptureAsync(command.Subject);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Captured: {command.Subject}",
            NavigationUrl = "inbox",
            ResultData = capture,
            ExecutedIntent = CommandIntent.Capture
        };
    }

    private async Task<CommandExecutionResult> ExecuteSearch(ParsedCommand command)
    {
        var query = command.Subject ?? string.Empty;
        var results = await _searchService.SearchAsync(query);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Found {results.TotalCount} results for '{query}'",
            NavigationUrl = $"search?q={Uri.EscapeDataString(query)}",
            ResultData = results,
            ExecutedIntent = CommandIntent.Search
        };
    }

    private async Task<CommandExecutionResult> ExecuteListTasks(ParsedCommand command)
    {
        var filter = command.Subject?.ToLowerInvariant() ?? string.Empty;
        IEnumerable<TodoTask> tasks;
        var description = "all tasks";

        if (filter.Contains("due today") || filter.Contains("today"))
        {
            tasks = (await _taskService.GetScheduledAsync(DateTime.Today, DateTime.Today.AddDays(1)))
                .Concat(await _taskService.GetOverdueAsync());
            description = "tasks due today";
        }
        else if (filter.Contains("overdue"))
        {
            tasks = await _taskService.GetOverdueAsync();
            description = "overdue tasks";
        }
        else if (filter.Contains("this week"))
        {
            var endOfWeek = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek);
            tasks = await _taskService.GetScheduledAsync(DateTime.Today, endOfWeek);
            description = "tasks for this week";
        }
        else if (filter.Contains("waiting"))
        {
            tasks = await _taskService.GetWaitingForAsync();
            description = "waiting for tasks";
        }
        else if (filter.Contains("someday"))
        {
            tasks = await _taskService.GetSomedayMaybeAsync();
            description = "someday/maybe tasks";
        }
        else if (filter.Contains("completed"))
        {
            tasks = await _taskService.GetByStatusAsync(TodoTaskStatus.Completed);
            description = "completed tasks";
        }
        else
        {
            tasks = await _taskService.GetNextActionsAsync();
            description = "next actions";
        }

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Found {tasks.Count()} {description}",
            NavigationUrl = "tasks",
            ResultData = tasks,
            ExecutedIntent = CommandIntent.ListTasks
        };
    }

    private async Task<CommandExecutionResult> ExecuteListProjects(ParsedCommand command)
    {
        var projects = await _projectService.GetActiveAsync();

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Found {projects.Count()} active projects",
            NavigationUrl = "projects",
            ResultData = projects,
            ExecutedIntent = CommandIntent.ListProjects
        };
    }

    private async Task<CommandExecutionResult> ExecuteCompleteTask(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify which task to complete",
                ExecutedIntent = CommandIntent.CompleteTask
            };
        }

        // Try to find the task by title substring
        var tasks = await _taskService.SearchAsync(command.Subject);
        var task = tasks.FirstOrDefault(t => t.Status != TodoTaskStatus.Completed);

        if (task == null)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = $"Could not find an active task matching '{command.Subject}'",
                ExecutedIntent = CommandIntent.CompleteTask
            };
        }

        await _taskService.CompleteAsync(task.Id);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Completed: {task.Title}",
            ResultData = task,
            ExecutedIntent = CommandIntent.CompleteTask
        };
    }

    private async Task<CommandExecutionResult> ExecuteScheduleTask(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify which task to schedule",
                ExecutedIntent = CommandIntent.ScheduleTask
            };
        }

        var dateString = command.Parameters.TryGetValue("secondary", out var date) ? date.ToString() : null;
        DateTime? scheduledDate = null;

        if (!string.IsNullOrEmpty(dateString))
        {
            if (_relativeDates.TryGetValue(dateString!, out var dateFunc))
            {
                scheduledDate = dateFunc();
            }
            else if (DateTime.TryParse(dateString, out var parsedDate))
            {
                scheduledDate = parsedDate;
            }
        }

        if (!scheduledDate.HasValue)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Could not parse the date. Try 'tomorrow', 'monday', or a specific date.",
                ExecutedIntent = CommandIntent.ScheduleTask
            };
        }

        var tasks = await _taskService.SearchAsync(command.Subject);
        var task = tasks.FirstOrDefault(t => t.Status != TodoTaskStatus.Completed);

        if (task == null)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = $"Could not find a task matching '{command.Subject}'",
                ExecutedIntent = CommandIntent.ScheduleTask
            };
        }

        await _taskService.ScheduleAsync(task.Id, scheduledDate.Value);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Scheduled '{task.Title}' for {scheduledDate.Value:dddd, MMMM d}",
            ResultData = task,
            ExecutedIntent = CommandIntent.ScheduleTask
        };
    }

    private async Task<CommandExecutionResult> ExecuteSetPriority(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify which task to prioritize",
                ExecutedIntent = CommandIntent.SetPriority
            };
        }

        var priorityString = command.Parameters.TryGetValue("secondary", out var p) ? p.ToString() : null;
        int priority = priorityString?.ToLowerInvariant() switch
        {
            "high" => 1,
            "medium" => 2,
            "low" => 3,
            _ when int.TryParse(priorityString, out var num) => num,
            _ => 2
        };

        var tasks = await _taskService.SearchAsync(command.Subject);
        var task = tasks.FirstOrDefault();

        if (task == null)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = $"Could not find a task matching '{command.Subject}'",
                ExecutedIntent = CommandIntent.SetPriority
            };
        }

        task.Priority = priority;
        await _taskService.UpdateAsync(task);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Set priority of '{task.Title}' to {GetPriorityName(priority)}",
            ResultData = task,
            ExecutedIntent = CommandIntent.SetPriority
        };
    }

    private async Task<CommandExecutionResult> ExecuteDeleteTask(ParsedCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Please specify which task to delete",
                ExecutedIntent = CommandIntent.DeleteTask
            };
        }

        var tasks = await _taskService.SearchAsync(command.Subject);
        var task = tasks.FirstOrDefault();

        if (task == null)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = $"Could not find a task matching '{command.Subject}'",
                ExecutedIntent = CommandIntent.DeleteTask
            };
        }

        await _taskService.DeleteAsync(task.Id);

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Deleted task: {task.Title}",
            ExecutedIntent = CommandIntent.DeleteTask
        };
    }

    private CommandExecutionResult ExecuteStartFocus(ParsedCommand command)
    {
        var taskQuery = command.Subject ?? string.Empty;
        _navigationManager.NavigateTo($"focus?task={Uri.EscapeDataString(taskQuery)}");

        return new CommandExecutionResult
        {
            Success = true,
            Message = string.IsNullOrEmpty(taskQuery)
                ? "Starting focus session"
                : $"Starting focus on: {taskQuery}",
            NavigationUrl = $"focus?task={Uri.EscapeDataString(taskQuery)}",
            ExecutedIntent = CommandIntent.StartFocus
        };
    }

    private CommandExecutionResult ExecuteStopFocus(ParsedCommand command)
    {
        // Navigate to focus page where timer can be stopped
        _navigationManager.NavigateTo("focus");

        return new CommandExecutionResult
        {
            Success = true,
            Message = "Navigate to focus page to stop timer",
            NavigationUrl = "focus",
            ExecutedIntent = CommandIntent.StopFocus
        };
    }

    private async Task<CommandExecutionResult> ExecuteUndo(ParsedCommand command)
    {
        if (!_commandHistory.CanUndo)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Nothing to undo",
                ExecutedIntent = CommandIntent.Undo
            };
        }

        var description = _commandHistory.NextUndoDescription;
        await _commandHistory.UndoAsync();

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Undid: {description}",
            ExecutedIntent = CommandIntent.Undo
        };
    }

    private async Task<CommandExecutionResult> ExecuteRedo(ParsedCommand command)
    {
        if (!_commandHistory.CanRedo)
        {
            return new CommandExecutionResult
            {
                Success = false,
                Error = "Nothing to redo",
                ExecutedIntent = CommandIntent.Redo
            };
        }

        var description = _commandHistory.NextRedoDescription;
        await _commandHistory.RedoAsync();

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Redid: {description}",
            ExecutedIntent = CommandIntent.Redo
        };
    }

    private CommandExecutionResult ExecuteHelp(ParsedCommand command)
    {
        var helpText = GetAvailableCommands()
            .GroupBy(c => c.Category)
            .Select(g => $"**{g.Key}**:\n" + string.Join("\n", g.SelectMany(c => c.Examples.Take(2)).Select(e => $"  - {e}")))
            .Aggregate((a, b) => $"{a}\n\n{b}");

        return new CommandExecutionResult
        {
            Success = true,
            Message = "Available commands:\n" + helpText,
            ResultData = GetAvailableCommands(),
            ExecutedIntent = CommandIntent.Help
        };
    }

    private async Task<CommandExecutionResult> ExecuteExport(ParsedCommand command)
    {
        // Navigate to the search page with export capability
        var query = command.Subject ?? "tasks";
        var format = command.Parameters.TryGetValue("secondary", out var f) ? f.ToString() : "csv";

        return new CommandExecutionResult
        {
            Success = true,
            Message = $"Navigate to export {query} as {format}",
            NavigationUrl = $"search?q={Uri.EscapeDataString(query)}&export={format}",
            ExecutedIntent = CommandIntent.Export
        };
    }

    #endregion

    #region Helper Methods

    private async Task EnrichCommandAsync(ParsedCommand command)
    {
        // Try to resolve entity references in the command
        if (command.Subject != null && command.Intent != CommandIntent.CreateTask)
        {
            // Check if it might be referencing an existing task
            if (command.Intent == CommandIntent.CompleteTask ||
                command.Intent == CommandIntent.ScheduleTask ||
                command.Intent == CommandIntent.SetPriority ||
                command.Intent == CommandIntent.DeleteTask)
            {
                var tasks = await _taskService.SearchAsync(command.Subject);
                var matchingTask = tasks.FirstOrDefault();
                if (matchingTask != null)
                {
                    command.EntityId = matchingTask.Id;
                    command.EntityType = "task";
                }
            }
        }
    }

    private async Task<CommandParseResult> ParseWithLlmAsync(string input)
    {
        // Use LLM to understand the command if pattern matching fails
        var prompt = $@"Parse this natural language command for a GTD productivity app:

Input: ""{input}""

Identify the intent from: navigate, create_task, create_project, create_goal, capture, search, list_tasks, list_projects, complete_task, schedule_task, set_priority, delete_task, start_focus, stop_focus, undo, redo, help, export

Return JSON:
{{
  ""intent"": ""<intent>"",
  ""subject"": ""<main object/title>"",
  ""parameters"": {{}}
}}";

        try
        {
            var response = await _llmService.GenerateAsync(prompt, new LlmOptions { Temperature = 0.1 });
            // Parse the JSON response... (simplified for now)

            // For now, return as unknown - full LLM integration would parse the response
            return new CommandParseResult
            {
                Success = false,
                Error = "Could not understand the command. Try 'help' to see available commands.",
                Confidence = 0.3
            };
        }
        catch
        {
            return new CommandParseResult
            {
                Success = false,
                Error = "Could not understand the command. Try 'help' to see available commands.",
                Confidence = 0.0
            };
        }
    }

    private static DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysUntil = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // Next week if today is that day
        return today.AddDays(daysUntil);
    }

    private static string GetIconForIntent(CommandIntent intent)
    {
        return intent switch
        {
            CommandIntent.Navigate => "oi-arrow-right",
            CommandIntent.CreateTask => "oi-plus",
            CommandIntent.CreateProject => "oi-folder",
            CommandIntent.CreateGoal => "oi-target",
            CommandIntent.Capture => "oi-inbox",
            CommandIntent.Search => "oi-magnifying-glass",
            CommandIntent.ListTasks => "oi-task",
            CommandIntent.ListProjects => "oi-folder",
            CommandIntent.ListGoals => "oi-target",
            CommandIntent.CompleteTask => "oi-check",
            CommandIntent.ScheduleTask => "oi-calendar",
            CommandIntent.SetPriority => "oi-star",
            CommandIntent.DeleteTask => "oi-trash",
            CommandIntent.StartFocus => "oi-timer",
            CommandIntent.StopFocus => "oi-media-stop",
            CommandIntent.Undo => "oi-action-undo",
            CommandIntent.Redo => "oi-action-redo",
            CommandIntent.Help => "oi-question-mark",
            CommandIntent.Export => "oi-data-transfer-download",
            _ => "oi-terminal"
        };
    }

    private static string GetPriorityName(int priority)
    {
        return priority switch
        {
            1 => "High",
            2 => "Medium",
            3 => "Low",
            _ => priority.ToString()
        };
    }

    private static double CalculateRelevance(string input, string candidate)
    {
        if (candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var distance = LevenshteinDistance(input.ToLowerInvariant(), candidate.ToLowerInvariant());
        return Math.Max(0, 1.0 - (distance / (double)Math.Max(input.Length, candidate.Length)));
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    #endregion
}
