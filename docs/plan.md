I'll create a comprehensive implementation plan for your GTD-inspired todo app with smart scheduling capabilities.

```markdown
# GTD Smart Todo Application - Implementation Plan

## Project Overview

A Blazor WebAssembly application implementing GTD (Getting Things Done) methodology with intelligent task scheduling, calendar integration preparation, and automated time block management. All data persists locally using IndexedDB.

---

## Technical Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 / Blazor WebAssembly |
| UI Framework | Blazor + CSS (or MudBlazor/Radzen for components) |
| Local Storage | IndexedDB via Blazor.IndexedDB or DnetIndexedDb |
| State Management | Fluxor or built-in cascading parameters |
| Calendar Integration | Mock service now, Microsoft Graph later |

---

## Data Models

### Core Entities

```csharp
// Base entity for all items
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

// Quick capture item (inbox)
public class CaptureItem : BaseEntity
{
    public string RawText { get; set; }
    public bool IsProcessed { get; set; } = false;
    public Guid? ProcessedIntoId { get; set; } // Links to resulting Task/Project
    public ProcessedItemType? ProcessedIntoType { get; set; }
}

public enum ProcessedItemType { Task, Project, Reference, Calendar, SomedayMaybe }

// Main task/action item
public class TodoTask : BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public Guid? ProjectId { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.NextAction;
    public List<string> Contexts { get; set; } = new(); // @home, @work, @phone, etc.
    public string Category { get; set; } // Maps to category dictionary
    public int? EnergyLevel { get; set; } // 1-5, required energy
    public int EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? WaitingForContactId { get; set; }
    public string WaitingForNote { get; set; }
    public DateTime? WaitingForSince { get; set; }
    public int Priority { get; set; } = 2; // 1=High, 2=Normal, 3=Low
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<Guid> LinkedMeetingIds { get; set; } = new();
    public bool RequiresDeepWork { get; set; }
    public string Notes { get; set; }
    public List<string> Tags { get; set; } = new();
}

public enum TaskStatus
{
    Inbox,
    NextAction,
    Active,
    WaitingFor,
    Scheduled,
    SomedayMaybe,
    Reference,
    Completed,
    Deleted
}

// Project (collection of tasks toward outcome)
public class Project : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DesiredOutcome { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string Category { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Priority { get; set; } = 2;
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; }
    // Navigation: Tasks loaded separately by ProjectId
}

public enum ProjectStatus { Active, OnHold, SomedayMaybe, Completed, Deleted }

// Calendar event (mock for now, real from Graph later)
public class CalendarEvent : BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; }
    public bool IsAllDay { get; set; }
    public string ExternalId { get; set; } // For Graph sync later
    public string Source { get; set; } = "Manual"; // Manual, MicrosoftGraph
    public MeetingCategory? AutoCategory { get; set; }
    public MeetingCategory? OverrideCategory { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? DecompressTimeMinutes { get; set; }
    public List<Guid> LinkedTaskIds { get; set; } = new();
    public List<string> Attendees { get; set; } = new();
    public bool RequiresPrep { get; set; }
    public bool RequiresFollowUp { get; set; }
}

public enum MeetingCategory
{
    OneOnOne,
    TeamMeeting,
    ClientMeeting,
    Interview,
    Presentation,
    Workshop,
    BrainStorming,
    StatusUpdate,
    Planning,
    Review,
    Training,
    Social,
    Focus,
    Break,
    Other
}

// Time block (scheduled work period)
public class TimeBlock : BaseEntity
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeBlockType Type { get; set; }
    public Guid? LinkedEventId { get; set; }
    public List<Guid> AssignedTaskIds { get; set; } = new();
    public string Notes { get; set; }
    public bool IsAutoGenerated { get; set; }
    public string Category { get; set; } // Inherits from meeting or task category
}

public enum TimeBlockType
{
    MeetingPrep,
    Meeting,
    MeetingDecompress,
    DeepWork,
    ShallowWork,
    AdminTime,
    Break,
    Buffer,
    Available
}

// Contact (for waiting-for tracking)
public class Contact : BaseEntity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
}

// Reference item
public class ReferenceItem : BaseEntity
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Url { get; set; }
    public List<Guid> LinkedProjectIds { get; set; } = new();
    public List<Guid> LinkedTaskIds { get; set; } = new();
}

// Context definition
public class Context : BaseEntity
{
    public string Name { get; set; } // @home, @work, @phone, @computer, @errands
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

// Category with dictionary terms for auto-categorization
public class CategoryDefinition : BaseEntity
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
    public List<string> MatchTerms { get; set; } = new(); // Keywords to auto-detect
    public int DefaultPrepMinutes { get; set; }
    public int DefaultDecompressMinutes { get; set; }
    public int DefaultEnergyRequired { get; set; }
    public bool TypicallyRequiresFollowUp { get; set; }
}

// User preferences
public class UserPreferences : BaseEntity
{
    public TimeSpan WorkDayStart { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan WorkDayEnd { get; set; } = TimeSpan.FromHours(17);
    public List<DayOfWeek> WorkDays { get; set; } = new() { 
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, 
        DayOfWeek.Thursday, DayOfWeek.Friday 
    };
    public int DefaultTaskDurationMinutes { get; set; } = 30;
    public int MinimumUsableBlockMinutes { get; set; } = 15;
    public int DeepWorkMinimumMinutes { get; set; } = 60;
    public int DefaultBreakMinutes { get; set; } = 10;
    public int MaxConsecutiveMeetingMinutes { get; set; } = 180;
    public int BufferBetweenMeetingsMinutes { get; set; } = 5;
    public int MorningEnergyPeak { get; set; } = 10; // Hour of day
    public int AfternoonEnergyPeak { get; set; } = 15;
    public bool AutoScheduleEnabled { get; set; } = true;
    public int DailyReviewReminderHour { get; set; } = 17;
    public int WeeklyReviewDay { get; set; } = 5; // Friday
}

// Daily snapshot for review
public class DailySnapshot : BaseEntity
{
    public DateOnly Date { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksCreated { get; set; }
    public int CapturesProcessed { get; set; }
    public int TotalMinutesWorked { get; set; }
    public int MeetingMinutes { get; set; }
    public int DeepWorkMinutes { get; set; }
    public string Notes { get; set; }
    public bool ReviewCompleted { get; set; }
}
```

---

## Application Structure

```
/src
├── GtdTodo.App/                    # Blazor WebAssembly project
│   ├── wwwroot/
│   │   ├── css/
│   │   ├── js/
│   │   │   └── indexeddb-interop.js
│   │   └── index.html
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   ├── NavMenu.razor
│   │   │   └── Breadcrumb.razor
│   │   ├── Shared/
│   │   │   ├── SortableTable.razor
│   │   │   ├── DrillDownLink.razor
│   │   │   ├── TagEditor.razor
│   │   │   ├── ContextBadge.razor
│   │   │   ├── CategoryBadge.razor
│   │   │   ├── PriorityIndicator.razor
│   │   │   ├── TimeDisplay.razor
│   │   │   ├── LinkedItemsList.razor
│   │   │   ├── QuickActions.razor
│   │   │   ├── SearchBox.razor
│   │   │   ├── FilterPanel.razor
│   │   │   ├── Modal.razor
│   │   │   └── ConfirmDialog.razor
│   │   ├── Capture/
│   │   │   ├── QuickCapturePage.razor      # Minimal distraction-free input
│   │   │   └── CaptureInput.razor
│   │   ├── Inbox/
│   │   │   ├── InboxPage.razor
│   │   │   ├── InboxItem.razor
│   │   │   └── ProcessItemModal.razor
│   │   ├── Tasks/
│   │   │   ├── TaskListPage.razor
│   │   │   ├── TaskDetailPage.razor
│   │   │   ├── TaskCard.razor
│   │   │   ├── TaskEditModal.razor
│   │   │   ├── NextActionsView.razor
│   │   │   ├── WaitingForView.razor
│   │   │   ├── ScheduledView.razor
│   │   │   └── SomedayMaybeView.razor
│   │   ├── Projects/
│   │   │   ├── ProjectListPage.razor
│   │   │   ├── ProjectDetailPage.razor
│   │   │   ├── ProjectCard.razor
│   │   │   └── ProjectEditModal.razor
│   │   ├── Calendar/
│   │   │   ├── CalendarPage.razor
│   │   │   ├── DayView.razor
│   │   │   ├── WeekView.razor
│   │   │   ├── EventCard.razor
│   │   │   ├── EventDetailPage.razor
│   │   │   ├── EventEditModal.razor
│   │   │   └── TimeBlockDisplay.razor
│   │   ├── Schedule/
│   │   │   ├── DailyPlanPage.razor
│   │   │   ├── TimeBlockEditor.razor
│   │   │   ├── AutoSchedulePanel.razor
│   │   │   └── ScheduleConflictView.razor
│   │   ├── Review/
│   │   │   ├── DailyReviewPage.razor
│   │   │   ├── WeeklyReviewPage.razor
│   │   │   ├── UnprocessedCaptures.razor
│   │   │   ├── StaleItemsView.razor
│   │   │   └── ReviewChecklist.razor
│   │   ├── Reference/
│   │   │   ├── ReferenceListPage.razor
│   │   │   └── ReferenceDetailPage.razor
│   │   ├── Reports/
│   │   │   ├── DashboardPage.razor
│   │   │   ├── ProductivityStats.razor
│   │   │   ├── CategoryBreakdown.razor
│   │   │   └── TimeAnalysis.razor
│   │   └── Settings/
│   │       ├── SettingsPage.razor
│   │       ├── PreferencesSection.razor
│   │       ├── ContextManager.razor
│   │       ├── CategoryManager.razor
│   │       ├── DataExportImport.razor
│   │       └── MockDataGenerator.razor
│   ├── Services/
│   │   ├── Data/
│   │   │   ├── IIndexedDbService.cs
│   │   │   ├── IndexedDbService.cs
│   │   │   ├── IRepository.cs
│   │   │   └── IndexedDbRepository.cs
│   │   ├── Domain/
│   │   │   ├── ICaptureService.cs
│   │   │   ├── CaptureService.cs
│   │   │   ├── ITaskService.cs
│   │   │   ├── TaskService.cs
│   │   │   ├── IProjectService.cs
│   │   │   ├── ProjectService.cs
│   │   │   ├── ICalendarService.cs
│   │   │   ├── CalendarService.cs
│   │   │   ├── ISchedulingService.cs
│   │   │   ├── SchedulingService.cs
│   │   │   ├── IReviewService.cs
│   │   │   └── ReviewService.cs
│   │   ├── Intelligence/
│   │   │   ├── ICategoryMatcherService.cs
│   │   │   ├── CategoryMatcherService.cs
│   │   │   ├── ITimeBlockGeneratorService.cs
│   │   │   ├── TimeBlockGeneratorService.cs
│   │   │   ├── IContextSwitchOptimizerService.cs
│   │   │   ├── ContextSwitchOptimizerService.cs
│   │   │   ├── ITaskSchedulerService.cs
│   │   │   └── TaskSchedulerService.cs
│   │   ├── Calendar/
│   │   │   ├── ICalendarProvider.cs
│   │   │   ├── MockCalendarProvider.cs
│   │   │   └── MicrosoftGraphCalendarProvider.cs  # Stub for future
│   │   └── Infrastructure/
│   │       ├── INavigationService.cs
│   │       ├── NavigationService.cs
│   │       ├── INotificationService.cs
│   │       └── NotificationService.cs
│   ├── State/
│   │   ├── AppState.cs
│   │   ├── AppStateActions.cs
│   │   └── AppStateReducers.cs
│   └── Program.cs
├── GtdTodo.Core/                    # Shared models and interfaces
│   ├── Models/
│   ├── Interfaces/
│   ├── Extensions/
│   └── Constants/
└── GtdTodo.Tests/
    ├── Unit/
    └── Integration/
```

---

## Feature Specifications

### 1. Quick Capture Page

**Purpose:** Distraction-free thought capture without seeing existing tasks.

**Requirements:**

- Minimal UI: Single text input, submit button, keyboard shortcut (Ctrl+Enter)
- No visible task list, no navigation distractions
- Optional: Small unobtrusive counter showing "X items captured today"
- Auto-focus on input on page load
- Success feedback: Brief animation/toast, then clear input
- Support multi-line input for detailed captures
- Optional voice input button (browser speech recognition API)
- Keyboard shortcut from anywhere in app to jump to capture (Ctrl+Shift+C)

**UI Layout:**

```
┌────────────────────────────────────────────────────┐
│                                                    │
│                                                    │
│         ┌────────────────────────────────┐         │
│         │  What's on your mind?          │         │
│         │                                │         │
│         │  [Multi-line text area]        │         │
│         │                                │         │
│         └────────────────────────────────┘         │
│                                                    │
│              [Capture] (Ctrl+Enter)                │
│                                                    │
│              3 items captured today                │
│                                                    │
│         [Back to App] (small, bottom)              │
└────────────────────────────────────────────────────┘
```

---

### 2. Inbox Processing

**Purpose:** Review and process captured items into actionable tasks, projects, or reference.

**Requirements:**

- List all unprocessed CaptureItems
- For each item, GTD-style processing:
  1. "Is it actionable?" → Yes/No
  2. If No → Trash, Reference, or Someday/Maybe
  3. If Yes → "Can it be done in 2 minutes?"
     - If Yes → Do it now, mark complete
     - If No → "Is it a single action or multiple steps?"
       - Single → Create Task
       - Multiple → Create Project with first next action
  4. "What's the next action?" → Define task
  5. "When/Where?" → Add context, due date, scheduled date
- Batch actions: Select multiple, process similarly
- Show original capture timestamp
- Link to resulting task/project after processing

**Processing Modal Flow:**

```
┌─────────────────────────────────────────────────────────┐
│ Process Capture                                    [X]  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Original: "Call dentist about appointment"              │
│ Captured: Today at 2:34 PM                              │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│ Is this actionable?                                     │
│ [Yes, it's actionable]    [No, not actionable]          │
│                                                         │
│ (If "No" selected):                                     │
│ [Move to Reference]  [Someday/Maybe]  [Delete]          │
│                                                         │
│ (If "Yes" selected):                                    │
│ Can you do this in under 2 minutes?                     │
│ [Yes - Do it now]    [No - needs more time]             │
│                                                         │
│ (If "No - needs more time"):                            │
│ Is this a single action or multi-step project?          │
│ [Single Action]      [Multi-step Project]               │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│ Create Task:                                            │
│ Title: [Call dentist about appointment          ]       │
│ Project: [None / Select project...              ▼]      │
│ Context: [@phone] [@work] [@home] [@errands]            │
│ Category: [Healthcare                           ▼]      │
│ Est. time: [15] minutes                                 │
│ Energy: [Low ●○○○○]                                     │
│ Due date: [None / Pick date]                            │
│ Schedule for: [None / Pick date & time]                 │
│                                                         │
│              [Create Task]    [Cancel]                  │
└─────────────────────────────────────────────────────────┘
```

---

### 3. Task Management

**Views Required:**

#### 3.1 Next Actions

- All tasks with Status = NextAction
- Group by Context or Category (toggle)
- Filter by: Context, Category, Energy Level, Time Available, Project
- Sort by: Priority, Due Date, Created Date, Estimated Time

#### 3.2 Waiting For

- Tasks with Status = WaitingFor
- Show: Contact name, waiting since date, days waiting
- Sort by: Days waiting, Contact, Due date

#### 3.3 Scheduled

- Tasks with ScheduledDate set
- Calendar-style view option
- List view grouped by date

#### 3.4 Someday/Maybe

- Tasks with Status = SomedayMaybe
- Periodic review reminder
- Quick action to activate

**Task Detail Page Features:**

- All task fields editable inline or via modal
- Activity history/log
- Linked items section (clickable drill-through):
  - Parent Project (if any)
  - Related Tasks
  - Related Calendar Events
  - Reference Items
- Quick actions: Complete, Delete, Move to Someday, Schedule
- Time tracking: Start/Stop timer
- Notes section with rich text

**Task Table Columns (sortable):**

| Column | Sortable | Filterable |
|--------|----------|------------|
| Title | Yes | Yes (search) |
| Project | Yes | Yes |
| Context | Yes | Yes (multi) |
| Category | Yes | Yes (multi) |
| Priority | Yes | Yes |
| Due Date | Yes | Yes (range) |
| Scheduled | Yes | Yes (range) |
| Est. Time | Yes | Yes (range) |
| Energy | Yes | Yes |
| Status | Yes | Yes |
| Created | Yes | Yes (range) |
| Tags | No | Yes (multi) |

---

### 4. Project Management

**Project List Features:**

- Filter by: Status, Category, Priority, Has Next Action
- Sort by: Name, Priority, Due Date, Progress, Created
- Visual indicator if project has no next action defined (stalled)

**Project Detail Page:**

- Project metadata (all fields editable)
- Task list for project (sortable table)
- Progress indicator: X of Y tasks complete
- Linked items section
- Button: "Add Task to Project"
- Warning banner if no active next action

---

### 5. Calendar & Events

**Calendar Views:**

- Day view: Hour-by-hour schedule with events and time blocks
- Week view: Overview of all days
- Agenda view: List of upcoming events

**Event Management:**

- Manual event creation (for mock phase)
- Auto-categorization based on title/description matching
- Linked tasks (prep work, follow-ups)
- Prep time and decompress time settings per event

**Event Detail:**

- All event fields
- Linked tasks (drill-through)
- Auto-suggested prep tasks based on category
- Button: "Create Follow-up Task"

---

### 6. Smart Scheduling Engine

**Core Algorithm: Time Block Generator**

```
Input: 
  - Calendar events for date range
  - User preferences
  - Unscheduled tasks

Process:
  1. Load all calendar events for the day
  2. For each event:
     a. Create Meeting time block
     b. Create Prep block before (based on category defaults or override)
     c. Create Decompress block after (based on category defaults)
  3. Identify remaining available time slots
  4. For each available slot:
     a. Categorize by duration (deep work viable? shallow only?)
     b. Consider time of day (morning = high energy, post-lunch = low)
  5. Score tasks for each slot:
     - Duration fit
     - Energy match
     - Context continuity (minimize switching)
     - Category alignment with adjacent meetings
     - Priority weighting
     - Due date urgency
  6. Assign tasks to slots optimizing total score
  7. Insert buffer/break blocks as needed

Output:
  - List of TimeBlock objects for the day
```

**Context Switch Minimization Logic:**

```
ContextSwitchScore calculation:
  - Same category as previous block: +10
  - Same context as previous block: +5
  - Related to adjacent meeting: +8
  - Requires deep focus after shallow work: -5
  - Switching from creative to administrative: -3
  - Mental load differential > 2: -4

Optimization approach:
  - Group similar tasks together
  - Place high-energy tasks at energy peaks
  - Schedule prep tasks immediately before related meetings
  - Schedule follow-up tasks soon after meetings
  - Batch similar contexts (all @phone calls together)
```

**Time Block Types and Defaults:**

| Block Type | Default Duration | Color | Description |
|------------|------------------|-------|-------------|
| MeetingPrep | Per category (5-30 min) | Orange | Prep before meetings |
| Meeting | Event duration | Blue | Calendar events |
| MeetingDecompress | Per category (5-15 min) | Light Orange | Process after meeting |
| DeepWork | 60-120 min | Green | Focus blocks for complex tasks |
| ShallowWork | 15-45 min | Teal | Admin, email, quick tasks |
| Break | 10-15 min | Gray | Rest periods |
| Buffer | 5 min | Light Gray | Transition time |
| Available | Variable | White | Unassigned time |

---

### 7. Category Dictionary & Auto-Matching

**Category Matcher Service:**

```csharp
public class CategoryMatcherService : ICategoryMatcherService
{
    public MeetingCategory? MatchCategory(string title, string description)
    {
        // Check against all CategoryDefinitions
        // Return best match based on MatchTerms
        // Return null if no confident match (user will manually set)
    }
    
    public List<CategoryMatch> GetPossibleMatches(string text)
    {
        // Return ranked list of possible categories with confidence scores
    }
}
```

**Default Category Definitions:**

| Category | Match Terms | Prep Time | Decompress | Energy |
|----------|-------------|-----------|------------|--------|
| OneOnOne | "1:1", "1-on-1", "one on one", "check-in" | 5 min | 5 min | Medium |
| TeamMeeting | "team", "standup", "sync", "all-hands" | 5 min | 5 min | Medium |
| ClientMeeting | "client", "customer", "external", company names | 15 min | 10 min | High |
| Interview | "interview", "candidate", "hiring" | 20 min | 15 min | High |
| Presentation | "present", "demo", "review", "showcase" | 30 min | 15 min | High |
| Workshop | "workshop", "working session", "brainstorm" | 15 min | 15 min | High |
| Planning | "planning", "roadmap", "strategy", "quarterly" | 10 min | 10 min | High |
| Training | "training", "learning", "onboarding" | 10 min | 10 min | Medium |
| Focus | "focus time", "no meetings", "blocked" | 0 min | 0 min | Variable |

---

### 8. Daily Review Page

**Purpose:** End-of-day processing and review workflow.

**Sections:**

1. **Unprocessed Captures**
   - Count and list of inbox items
   - Link to process each

2. **Today's Completed Tasks**
   - List with actual time vs estimated
   - Option to add notes

3. **Incomplete Scheduled Tasks**
   - Tasks that were scheduled for today but not done
   - Options: Mark complete, Reschedule, Move to Next Actions

4. **Tomorrow's Preview**
   - Calendar events
   - Currently scheduled tasks
   - Auto-generate schedule button

5. **Stale Items Alert**
   - Waiting-for items > 7 days
   - Tasks with overdue dates
   - Projects with no next action

6. **Quick Stats**
   - Tasks completed today
   - Time in meetings vs focused work
   - Captures processed

**Daily Review Checklist (GTD-inspired):**

```
□ Process all inbox items to zero
□ Review today's completed tasks
□ Reschedule any incomplete items
□ Check waiting-for items (follow up if needed)
□ Review tomorrow's calendar
□ Identify tomorrow's most important tasks (MIT)
□ Clear your head: anything else to capture?
```

---

### 9. Weekly Review Page

**Expanded review following GTD Weekly Review:**

1. **Get Clear**
   - Process inbox to zero
   - Process notes/captures
   - Review previous week's calendar
   - Review upcoming week's calendar

2. **Get Current**
   - Review Next Actions list
   - Review Projects list (each should have next action)
   - Review Waiting For list
   - Review Someday/Maybe list

3. **Get Creative**
   - New projects to add?
   - New ideas captured
   - Trigger list review option

4. **Statistics**
   - Week over week comparison
   - Category time breakdown
   - Productivity trends

---

### 10. Dashboard / Home Page

**Widgets:**

1. **Today at a Glance**
   - Current/next time block
   - Events remaining
   - Tasks scheduled

2. **Quick Capture** (inline)
   - Small input for immediate capture

3. **Next Actions** (top 5)
   - Prioritized list
   - Drill-through to task

4. **Stale Items Alert**
   - Overdue count
   - Old waiting-for count

5. **This Week**
   - Mini calendar view
   - Event/task density

6. **Productivity Pulse**
   - Tasks completed (week)
   - Focus time achieved
   - Inbox zero streak

---

### 11. Sortable Table Component

**Generic sortable table component for reuse:**

```razor
<SortableTable TItem="TodoTask"
               Items="@tasks"
               OnRowClick="@HandleRowClick"
               EnableDrillThrough="true">
    <Columns>
        <Column Field="@(t => t.Title)" Title="Title" Sortable="true" />
        <Column Field="@(t => t.Project)" Title="Project" Sortable="true">
            <Template Context="task">
                <DrillDownLink Type="Project" Id="@task.ProjectId">
                    @task.ProjectName
                </DrillDownLink>
            </Template>
        </Column>
        <Column Field="@(t => t.Priority)" Title="Priority" Sortable="true">
            <Template Context="task">
                <PriorityIndicator Value="@task.Priority" />
            </Template>
        </Column>
        <!-- More columns -->
    </Columns>
</SortableTable>
```

**Features:**

- Click column header to sort (asc/desc toggle)
- Visual sort indicator (arrow)
- Multi-column sort with Shift+Click
- Sticky header option
- Row click handler for drill-through
- Cell-level custom templates
- Pagination support
- Export to CSV/JSON

---

### 12. Drill-Through & Linking

**DrillDownLink Component:**

```razor
<DrillDownLink Type="Task" Id="@taskId">View Task</DrillDownLink>
<DrillDownLink Type="Project" Id="@projectId">@projectName</DrillDownLink>
<DrillDownLink Type="Event" Id="@eventId">Related Meeting</DrillDownLink>
```

**Behavior:**

- Renders as clickable link with appropriate icon
- Navigates to detail page for entity type
- Supports: Task, Project, Event, Reference, Contact
- Optional tooltip preview on hover

**LinkedItemsList Component:**

```razor
<LinkedItemsList>
    <LinkedTasks Items="@task.LinkedTaskIds" />
    <LinkedProjects Items="@task.ProjectId" />
    <LinkedEvents Items="@task.LinkedMeetingIds" />
</LinkedItemsList>
```

---

### 13. Data Persistence (IndexedDB)

**Database Schema:**

```javascript
// indexeddb-schema.js
const DB_NAME = "GtdTodoDb";
const DB_VERSION = 1;

const stores = {
  captures: { keyPath: "id", indexes: ["createdAt", "isProcessed"] },
  tasks: { keyPath: "id", indexes: ["status", "projectId", "dueDate", "scheduledDate", "category"] },
  projects: { keyPath: "id", indexes: ["status", "category"] },
  events: { keyPath: "id", indexes: ["startTime", "endTime"] },
  timeBlocks: { keyPath: "id", indexes: ["startTime", "date"] },
  contacts: { keyPath: "id", indexes: ["name"] },
  references: { keyPath: "id", indexes: ["category"] },
  contexts: { keyPath: "id", indexes: ["name"] },
  categories: { keyPath: "id", indexes: ["name"] },
  preferences: { keyPath: "id" },
  dailySnapshots: { keyPath: "id", indexes: ["date"] }
};
```

**Service Interface:**

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
```

---

### 14. Settings & Configuration

**Settings Page Sections:**

1. **Work Schedule**
   - Work day start/end times
   - Work days selection
   - Time zone

2. **Scheduling Preferences**
   - Default task duration
   - Minimum useful block size
   - Deep work minimum duration
   - Break preferences
   - Buffer between meetings

3. **Energy Settings**
   - Morning energy peak hour
   - Afternoon energy peak hour
   - Post-lunch dip handling

4. **Review Settings**
   - Daily review reminder time
   - Weekly review day
   - Stale threshold days

5. **Context Management**
   - Add/edit/delete contexts
   - Set default contexts
   - Icon and color customization

6. **Category Management**
   - Add/edit/delete categories
   - Configure match terms
   - Set default prep/decompress times

7. **Data Management**
   - Export all data (JSON)
   - Import data
   - Clear all data
   - Generate mock data (for testing)

---

### 15. Mock Data Generator

**For development and testing:**

```csharp
public class MockDataGenerator
{
    public async Task GenerateSampleData()
    {
        // Generate:
        // - 20 sample tasks across various states
        // - 5 sample projects
        // - 10 calendar events for next 2 weeks
        // - 5 inbox items
        // - Sample contacts
        // - Sample reference items
        // - Default categories and contexts
    }
    
    public async Task GenerateCalendarForDate(DateOnly date, int eventCount)
    {
        // Generate realistic meeting schedule for a day
    }
}
```

---

## Navigation Structure

```
├── Dashboard (Home)
├── Capture (Quick Capture Page)
├── Inbox
│   └── Process Item Modal
├── Tasks
│   ├── Next Actions
│   ├── Waiting For
│   ├── Scheduled
│   ├── Someday/Maybe
│   └── Task Detail → /tasks/{id}
├── Projects
│   ├── Active
│   ├── On Hold
│   └── Project Detail → /projects/{id}
├── Calendar
│   ├── Day View
│   ├── Week View
│   └── Event Detail → /events/{id}
├── Schedule
│   ├── Today's Plan
│   └── Auto-Schedule
├── Review
│   ├── Daily Review
│   └── Weekly Review
├── Reference
│   └── Reference Detail → /reference/{id}
├── Reports
│   └── Dashboard/Stats
└── Settings
    ├── Preferences
    ├── Contexts
    ├── Categories
    └── Data
```

---

## URL Routing

| Route | Component | Description |
|-------|-----------|-------------|
| `/` | DashboardPage | Home dashboard |
| `/capture` | QuickCapturePage | Distraction-free capture |
| `/inbox` | InboxPage | Process captured items |
| `/tasks` | TaskListPage | All tasks with filters |
| `/tasks/next` | NextActionsView | Next actions only |
| `/tasks/waiting` | WaitingForView | Waiting for items |
| `/tasks/scheduled` | ScheduledView | Scheduled tasks |
| `/tasks/someday` | SomedayMaybeView | Someday/maybe |
| `/tasks/{id}` | TaskDetailPage | Single task detail |
| `/projects` | ProjectListPage | All projects |
| `/projects/{id}` | ProjectDetailPage | Single project detail |
| `/calendar` | CalendarPage | Calendar views |
| `/calendar/day/{date?}` | DayView | Day view |
| `/calendar/week/{date?}` | WeekView | Week view |
| `/events/{id}` | EventDetailPage | Event detail |
| `/schedule` | DailyPlanPage | Today's schedule |
| `/review/daily` | DailyReviewPage | Daily review |
| `/review/weekly` | WeeklyReviewPage | Weekly review |
| `/reference` | ReferenceListPage | Reference items |
| `/reference/{id}` | ReferenceDetailPage | Reference detail |
| `/reports` | DashboardPage | Statistics |
| `/settings` | SettingsPage | All settings |

---

## Implementation Phases

### Phase 1: Foundation (Core Setup)

1. Create Blazor WebAssembly project with .NET 10
2. Set up IndexedDB integration
3. Implement base repository pattern
4. Create all data models
5. Implement basic navigation/layout
6. Create SortableTable component
7. Create DrillDownLink component

### Phase 2: Capture & Basic Tasks

1. Implement QuickCapturePage
2. Implement CaptureService
3. Implement InboxPage with processing flow
4. Implement basic TaskListPage
5. Implement TaskDetailPage
6. Implement task CRUD operations

### Phase 3: GTD Views

1. Implement NextActionsView
2. Implement WaitingForView
3. Implement ScheduledView
4. Implement SomedayMaybeView
5. Add filtering and sorting to all views
6. Implement context and category management

### Phase 4: Projects

1. Implement ProjectListPage
2. Implement ProjectDetailPage
3. Link tasks to projects
4. Add "no next action" warnings
5. Project progress tracking

### Phase 5: Calendar & Events

1. Implement CalendarPage (day/week views)
2. Implement EventDetailPage
3. Manual event creation
4. Category auto-matching service
5. Event-task linking

### Phase 6: Smart Scheduling

1. Implement TimeBlockGeneratorService
2. Implement ContextSwitchOptimizerService
3. Implement TaskSchedulerService
4. Build DailyPlanPage with time blocks
5. Auto-schedule functionality
6. Drag-and-drop time block adjustment

### Phase 7: Reviews

1. Implement DailyReviewPage
2. Implement WeeklyReviewPage
3. Daily snapshot tracking
4. Statistics and reporting
5. Review checklists

### Phase 8: Polish & Enhancement

1. Dashboard widgets
2. Search across all entities
3. Keyboard shortcuts
4. Data export/import
5. Mock data generator
6. Performance optimization
7. PWA support (offline)

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+C` | Open quick capture |
| `Ctrl+Shift+N` | New task |
| `Ctrl+Shift+P` | New project |
| `Ctrl+/` | Focus search |
| `G then H` | Go to home |
| `G then I` | Go to inbox |
| `G then T` | Go to tasks |
| `G then P` | Go to projects |
| `G then C` | Go to calendar |
| `G then R` | Go to daily review |
| `Esc` | Close modal/cancel |

---

## Testing Requirements

### Unit Tests

- All service methods
- Category matching logic
- Time block generation algorithm
- Context switch scoring

### Integration Tests

- IndexedDB operations
- Full CRUD flows
- Navigation flows

### E2E Tests (optional)

- Capture to task flow
- Daily review workflow
- Auto-scheduling

---

## Future Enhancements (Post-MVP)

1. **Microsoft Graph Integration**
   - Replace MockCalendarProvider with MicrosoftGraphCalendarProvider
   - OAuth flow
   - Real-time sync

2. **Natural Language Input**
   - Parse "Call mom tomorrow at 3pm" into task with due date

3. **Recurring Tasks**
   - Daily, weekly, monthly patterns

4. **Collaboration**
   - Share projects
   - Delegate tasks

5. **Mobile Optimization**
   - Responsive design
   - Touch-friendly interactions

6. **Notifications**
   - Browser notifications for reminders
   - Daily review reminder

7. **Integrations**
   - Todoist import
   - Things 3 import
   - Notion sync

---

## Success Criteria

- [ ] All captures can be processed through GTD workflow
- [ ] Tasks are sortable and filterable across all relevant fields
- [ ] Every data item is drill-through clickable to its detail page
- [ ] Smart scheduling generates reasonable time blocks
- [ ] Category auto-matching works with configurable dictionary
- [ ] Daily and weekly reviews guide user through GTD process
- [ ] All data persists in IndexedDB across sessions
- [ ] Application works offline after initial load

```

This plan is ready for handoff to an implementation agent. Want me to elaborate on any specific section or create starter code for any component?
