# Ideas 4

0. We're going to connect the application to ollama and integrate an LLM service. the service will be used by multiple entities/services in the application, so it should be somewhat genericized, and then each consumer can manage their detailed implementation. Please add the necessary services into the application to prepare the ollama service
1. Allow users to capture their goals in the application. Let them set the window of time or deadline for the goal, and provide helpful fields to help them think pragmatically about how they achieve the goal. From there, it would be nice if the service could decompose the goal into subtasks, but this would only be possible if the application was connected to a local LLM. i'm running ollama locally, so lets add this functionality.
2. There should be three mechanisms to enter goals in the app- bulk import, manual entry, and AI-assisted entry. the AI-assisted entry should leverage ollama and act almost like a chat service with the user to help them think about their goals and break them down into smaller actionable parts
3. If the user bulk imported or manually entered their goals, they should be able to later use the LLM service to augment the goals. Please generate all the necessary prompt templates and data structures for these interactions and to ensure that we're always getting the necessary data format back from the model
4. The application should continue to function completely if Ollama is not available. simply inform the user that the app is not currently configured to use an LLM, and that they can set the connection information in the settings (maybe they're using azure openai or something). Ensure that everything besides the AI functionality continues to work if ollama isnt available
5. When the user changes the prep time or wind down time, use that as the value the next time the dialog is opened. it should always default to the previously used values
6. When the new event dialog opens, the title input should autofocus
7. The Create button should enable after the title has 3 or more characters, and not wait for the user to defocus the title input field before enabling the button
8. the user should be able to press cmd enter from the new event screen to create the event
9. if the user presses cmd shift space, it should display a search dialog like the one on mac os when the user presses cmd space. this search should allow the user to search all their tasks, events, and anything else, as a substring search, with matching results being displayed in the dialog (Same as the search panel in macos). the user should then be able to open any of the search results from the display, loading the relevant page. we have this functionality in /documents/code/ontology-builder/onto-editor/eidos application, if you want to reference it. it also uses the command pattern to enable undo and redo, which we should add to this application.
10. please add undo and redo functionality to the application leveraging the command pattern to make the code clean and reusable
11. When the user uses the auto scheduler, it should also consider the substasks for goals, the deadlines for the goals, progress towards the goals, and let the user know how they're doing towards their goals, and if they should prioritize work towards the goal to stay on track. Please feed the goals functionality in as well to the calendar and scheduling optimizer, and treat the subgoal actions as if they're tasks (we simply track which goal these tasks happen to be associated with (maybe they can have more than one associated))

# Implementation Plan: Ideas-4 Features

## Overview

This document outlines the implementation plan for the features described in ideas-4.md, along with additional items that should be included for a complete implementation.

---

## Phase 1: Core Infrastructure

### 1.1 LLM Service (Ollama Integration)

**Files to Create:**

- `src/SelfOrganizer.Core/Interfaces/ILlmService.cs`
- `src/SelfOrganizer.Core/Models/LlmModels.cs`
- `src/SelfOrganizer.App/Services/Intelligence/LlmService.cs`
- `src/SelfOrganizer.App/Services/Intelligence/LlmPromptTemplates.cs`

**Interface Design:**

```csharp
public interface ILlmService
{
    Task<bool> IsAvailableAsync();
    Task<LlmConnectionStatus> GetConnectionStatusAsync();
    Task<string> GenerateAsync(string prompt, LlmOptions? options = null);
    Task<T?> GenerateStructuredAsync<T>(string prompt, LlmOptions? options = null) where T : class;
    Task<IAsyncEnumerable<string>> StreamGenerateAsync(string prompt, LlmOptions? options = null);
}
```

**Models:**

```csharp
public class LlmOptions
{
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2048;
}

public class LlmConnectionStatus
{
    public bool IsConnected { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> AvailableModels { get; set; } = new();
}

public class LlmSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public bool IsEnabled { get; set; } = true;
}
```

**Additional Items (Overlooked):**

- Settings UI for configuring LLM endpoint, model selection
- Connection test button in settings
- Model dropdown populated from Ollama's /api/tags endpoint
- Timeout configuration
- Retry logic with exponential backoff

---

### 1.2 Command Pattern (Undo/Redo)

**Files to Create:**

- `src/SelfOrganizer.Core/Interfaces/ICommand.cs`
- `src/SelfOrganizer.Core/Interfaces/ICommandHistory.cs`
- `src/SelfOrganizer.App/Services/Commands/CommandHistory.cs`
- `src/SelfOrganizer.App/Services/Commands/CommandFactory.cs`
- `src/SelfOrganizer.App/Services/Commands/TaskCommands.cs`
- `src/SelfOrganizer.App/Services/Commands/ProjectCommands.cs`
- `src/SelfOrganizer.App/Services/Commands/EventCommands.cs`
- `src/SelfOrganizer.App/Services/Commands/GoalCommands.cs`

**Interface Design:**

```csharp
public interface ICommand
{
    string Description { get; }
    Task ExecuteAsync();
    Task UndoAsync();
}

public interface ICommandHistory
{
    event Action? OnHistoryChanged;
    Task ExecuteAsync(ICommand command);
    Task<bool> UndoAsync();
    Task<bool> RedoAsync();
    bool CanUndo { get; }
    bool CanRedo { get; }
    string? NextUndoDescription { get; }
    string? NextRedoDescription { get; }
    void Clear();
}
```

**Additional Items (Overlooked):**

- Toast notification on undo/redo ("Undid: Create task 'Buy groceries'")
- Keyboard shortcut registration (Cmd+Z, Cmd+Shift+Z)
- Max history size (50 items)
- Clear history on logout/data reset

---

### 1.3 Global Search Service

**Files to Create:**

- `src/SelfOrganizer.Core/Interfaces/ISearchService.cs`
- `src/SelfOrganizer.Core/Models/SearchModels.cs`
- `src/SelfOrganizer.App/Services/SearchService.cs`
- `src/SelfOrganizer.App/Components/Shared/GlobalSearch.razor`
- `src/SelfOrganizer.App/Components/Shared/GlobalSearch.razor.cs`
- `src/SelfOrganizer.App/wwwroot/css/components/global-search.css`
- `src/SelfOrganizer.App/wwwroot/js/keyboard-shortcuts.js`

**Interface Design:**

```csharp
public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, SearchOptions? options = null);
    Task<IEnumerable<QuickAction>> GetQuickActionsAsync();
}

public record SearchResult(
    string Type,        // "task", "project", "event", "goal", "capture", "reference"
    Guid Id,
    string Title,
    string? Subtitle,
    string? MatchedField,
    string Icon,
    string? NavigationUrl
);

public record QuickAction(
    string Id,
    string Title,
    string? Shortcut,
    string Icon,
    Func<Task> Action
);
```

**Additional Items (Overlooked):**

- Recent searches history (stored in localStorage)
- Search result type filtering tabs
- Keyboard navigation (arrow keys, Enter to select, Escape to close)
- Search result highlighting (bold matched text)
- Quick actions section (New Task, New Event, New Goal, etc.)
- Fuzzy matching option

---

## Phase 2: Goals Feature

### 2.1 Goal Models and Service

**Files to Create:**

- `src/SelfOrganizer.Core/Models/Goal.cs`
- `src/SelfOrganizer.Core/Models/GoalEnums.cs`
- `src/SelfOrganizer.Core/Interfaces/IGoalService.cs`
- `src/SelfOrganizer.App/Services/Domain/GoalService.cs`

**Model Design:**

```csharp
public class Goal : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DesiredOutcome { get; set; }
    public string? SuccessCriteria { get; set; }  // How will you know you've achieved it?
    public string? Obstacles { get; set; }         // What might get in the way?
    public string? Resources { get; set; }         // What do you need to succeed?
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public GoalCategory Category { get; set; } = GoalCategory.Personal;
    public GoalTimeframe Timeframe { get; set; } = GoalTimeframe.Quarter;
    public DateTime? TargetDate { get; set; }
    public DateTime? StartDate { get; set; }
    public int Priority { get; set; } = 2;  // 1=High, 2=Medium, 3=Low
    public int ProgressPercent { get; set; } = 0;
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedTaskIds { get; set; } = new();  // Direct subtasks
    public List<string> Tags { get; set; } = new();
    public string? AiGeneratedPlan { get; set; }  // Stored AI decomposition
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public enum GoalStatus { Active, OnHold, Completed, Archived }
public enum GoalCategory { Career, Health, Financial, Personal, Learning, Relationships, Creative, Other }
public enum GoalTimeframe { Week, Month, Quarter, Year, MultiYear }
```

**Additional Items (Overlooked):**

- Goal templates (common goal structures)
- Goal milestones (intermediate checkpoints)
- Goal reflection prompts (weekly/monthly check-ins)
- Goal-to-goal dependencies (prerequisite goals)
- Progress history tracking

---

### 2.2 Goal Entry Methods

**Files to Create:**

- `src/SelfOrganizer.App/Pages/Goals/Goals.razor`
- `src/SelfOrganizer.App/Pages/Goals/Goals.razor.cs`
- `src/SelfOrganizer.App/Pages/Goals/Goals.razor.css`
- `src/SelfOrganizer.App/Components/Goals/GoalCard.razor`
- `src/SelfOrganizer.App/Components/Goals/GoalForm.razor`
- `src/SelfOrganizer.App/Components/Goals/GoalBulkImport.razor`
- `src/SelfOrganizer.App/Components/Goals/GoalAiAssistant.razor`
- `src/SelfOrganizer.App/Components/Goals/GoalDecomposition.razor`

**Entry Methods:**

1. **Manual Entry**: Standard form with all fields
2. **Bulk Import**: CSV/JSON import with field mapping
3. **AI-Assisted Entry**: Chat-like interface that helps user clarify and structure their goal

**AI Assistant Flow:**

```
User: "I want to get healthier"
AI: "That's a great goal! Let me help you make it more specific.
     When you say 'healthier', what does that look like for you?
     (e.g., lose weight, exercise more, eat better, reduce stress)"
User: "I want to lose 20 pounds"
AI: "Got it! By when would you like to achieve this?"
User: "By summer, so about 6 months"
AI: "That's a healthy pace of about 3-4 lbs per month.
     Here's a structured goal I've created:

     Title: Lose 20 pounds
     Target Date: June 2026
     Success Criteria: Reach target weight of [X] lbs

     Would you like me to break this down into actionable tasks?"
User: "Yes please"
AI: [Generates subtasks like: "Research diet plans", "Set up meal prep routine", etc.]
```

**Additional Items (Overlooked):**

- Goal wizard (step-by-step guided creation)
- Import from popular goal-setting apps
- Goal sharing/export functionality
- Visual progress indicators (progress bars, charts)

---

### 2.3 LLM Integration for Goals

**Prompt Templates to Create:**

1. `GoalClarificationPrompt` - Help user refine vague goals
2. `GoalDecompositionPrompt` - Break goal into projects/tasks
3. `GoalProgressAnalysisPrompt` - Analyze progress and suggest adjustments
4. `GoalObstacleIdentificationPrompt` - Identify potential blockers
5. `GoalTimelineEstimationPrompt` - Estimate realistic timeframes

**Structured Output Schema:**

```csharp
public class GoalDecompositionResult
{
    public List<SuggestedProject> Projects { get; set; } = new();
    public List<SuggestedTask> DirectTasks { get; set; } = new();
    public List<string> Milestones { get; set; } = new();
    public string? TimelineAnalysis { get; set; }
    public List<string> PotentialObstacles { get; set; } = new();
}

public class SuggestedProject
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<SuggestedTask> Tasks { get; set; } = new();
}

public class SuggestedTask
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedMinutes { get; set; }
    public string? SuggestedContext { get; set; }
}
```

---

## Phase 3: UX Improvements

### 3.1 Event Dialog Improvements

**Changes to Calendar.razor:**

1. Autofocus title input on modal open
2. Enable Create button after 3+ characters (reactive binding)
3. Cmd+Enter keyboard shortcut to submit
4. Remember last prep/wind-down times

**Implementation:**

```csharp
// Autofocus
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (_showModal && _titleInput != null)
    {
        await _titleInput.FocusAsync();
    }
}

// Reactive button enable
private bool CanCreateEvent => _eventTitle?.Length >= 3;

// Keyboard shortcut handler (in JS interop)
// Cmd+Enter triggers form submission
```

### 3.2 Keyboard Shortcuts System

**Files to Create:**

- `src/SelfOrganizer.App/wwwroot/js/keyboard-shortcuts.js`
- `src/SelfOrganizer.App/Services/KeyboardShortcutService.cs`

**Shortcuts to Implement:**

| Shortcut | Action |
|----------|--------|
| Cmd+Shift+Space | Open global search |
| Cmd+Z | Undo |
| Cmd+Shift+Z | Redo |
| Cmd+Enter | Submit current form |
| Cmd+N | New item (context-aware) |
| Escape | Close modal/search |

**Additional Items (Overlooked):**

- Keyboard shortcut help modal (Cmd+/)
- Customizable shortcuts in settings
- Shortcut conflict detection

---

## Phase 4: Auto-Scheduler Integration

### 4.1 Goals in Scheduling

**Changes to:**

- `ISchedulingService` - Add goal-aware methods
- `ITaskOptimizerService` - Consider goal deadlines in scoring
- `SchedulingService.cs` - Integrate goal progress tracking

**New Methods:**

```csharp
// ISchedulingService additions
Task<IEnumerable<GoalProgressReport>> GetGoalProgressReportsAsync();
Task<IEnumerable<TodoTask>> GetGoalSubtasksForSchedulingAsync(Guid goalId);

// New model
public class GoalProgressReport
{
    public Goal Goal { get; set; }
    public double ProgressPercent { get; set; }
    public int DaysRemaining { get; set; }
    public double RequiredDailyProgress { get; set; }
    public bool IsOnTrack { get; set; }
    public string? Recommendation { get; set; }
    public List<TodoTask> OverdueTasks { get; set; }
    public List<TodoTask> UpcomingTasks { get; set; }
}
```

**Additional Items (Overlooked):**

- Goal deadline warnings in calendar view
- Goal progress dashboard widget
- "Focus on goal" mode (prioritize goal tasks in scheduling)
- Goal velocity tracking (tasks completed per week toward goal)

---

## Phase 5: Settings & Configuration

### 5.1 LLM Settings Page

**Add to Settings:**

- LLM Provider (Ollama, Azure OpenAI, OpenAI)
- Endpoint URL
- API Key (for cloud providers)
- Model selection dropdown
- Connection test button
- Enable/disable AI features toggle

### 5.2 Graceful Degradation

**Implementation:**

- Check `ILlmService.IsAvailableAsync()` before any AI operation
- Show informative message when AI unavailable
- All AI-dependent buttons show disabled state with tooltip
- Settings link in disabled state message

---

## Implementation Order

### Sprint 1: Core Infrastructure

1. LLM Service (interfaces, basic implementation, settings)
2. Command Pattern (interfaces, history, basic commands)
3. Keyboard shortcuts system

### Sprint 2: Goals Feature

4. Goal models and service
2. Goals UI (list, form, cards)
3. Goal entry methods (manual, bulk)

### Sprint 3: AI Integration

7. AI-assisted goal entry
2. Goal decomposition with LLM
3. Goal augmentation for existing goals

### Sprint 4: Search & Polish

10. Global search service and UI
2. UX improvements (autofocus, shortcuts)
3. Auto-scheduler goal integration

### Sprint 5: Dashboard & Analytics

13. Goal progress dashboard
2. Goal tracking in calendar view
3. Final polish and testing

---

## Database Changes

**New IndexedDB Store:**

- `goals` - Store for Goal entities

**Update StoreNames.cs:**

```csharp
public const string Goals = "goals";
```

**Update IndexedDbService.cs:**

- Add goals store initialization
- Add indexes for goalStatus, targetDate, category

---

## Navigation Changes

**Add to NavMenu.razor:**

```html
<NavLink class="nav-link" href="goals">
    <span class="oi oi-target me-2"></span> Goals
</NavLink>
```

---

## Testing Considerations

1. Test all features with Ollama unavailable
2. Test undo/redo with various operation sequences
3. Test search with special characters
4. Test goal decomposition with various input types
5. Test keyboard shortcuts across browsers
6. Test bulk import with malformed data
