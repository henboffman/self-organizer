# Self-Organizer Feature Assessment & Roadmap

## Current State Summary

### Strengths
- **Sophisticated GTD Implementation**: Full inbox processing, next actions, waiting-for, someday/maybe, projects, goals
- **Advanced Task Optimization**: 10-dimensional scoring with circadian rhythm modeling, Pareto optimization, critical path analysis
- **Calendar Intelligence**: Smart prep/decompress blocks, meeting insights, conflict detection
- **ADHD Accommodations**: Celebration effects, Pick For Me, time blindness helpers, minimal mode
- **LLM Integration**: Multi-provider support (Ollama, Azure OpenAI, OpenAI, Anthropic) with goal decomposition
- **Rich Data Model**: Hierarchical tasks, dependencies, recurring tasks, multi-entity linking

### Current Gaps
- No external data sync (calendar, tasks from other systems)
- Client-side only (no multi-device sync)
- Limited bulk operations
- No automation/rules engine
- Basic recurring task handling
- No time tracking beyond estimates

---

## Proposed Feature Categories

### 1. DATA JOBS & EXTERNAL SYNC

#### 1.1 Data Jobs Dashboard
**Priority: HIGH** | Enables calendar sync and future integrations

```
/data-jobs page with:
├── Data Sources Overview (cards showing status, last sync, record count)
├── Sync Progress Tracking (real-time phase, %, items processed)
├── Execution History (filterable table with duration, status, errors)
├── Job Details Modal (steps, logs, error details)
└── Quick Actions (Sync All, Sync Individual, Test Connection)
```

**Models needed:**
- `DataSource` enum: MicrosoftCalendar, LifecycleTasks, GoogleCalendar (future)
- `SyncJob`: Id, Source, StartTime, EndTime, Status, RecordsProcessed, Errors
- `SyncConfiguration`: per-source settings, schedule, enabled state
- `SyncProgress`: real-time tracking with events

#### 1.2 Microsoft Graph Calendar Sync
**Priority: HIGH** | User specifically requested

**Features:**
- OAuth2 authentication flow (you have permissions granted)
- Fetch calendar events for configurable date range
- Map to CalendarEvent model (set Source = "MicrosoftGraph")
- Bidirectional sync option (push local manual events to Graph)
- Conflict detection (local vs remote changes)
- Category mapping (Graph categories → MeetingCategory)
- Attendee extraction for stakeholder tracking
- Recurring event handling

**Sync Settings:**
- Calendars to sync (primary, shared, team)
- Date range (past 7 days, future 30 days)
- Sync frequency (manual, every 15/30/60 min)
- Categories to include/exclude
- Auto-create prep/decompress blocks

#### 1.3 Lifecycle Task Import/Export
**Priority: HIGH** | User specifically requested

**Common Interchange Format (JSON):**
```json
{
  "version": "1.0",
  "exportedAt": "2025-01-14T...",
  "source": "lifecycle|self-organizer",
  "tasks": [
    {
      "externalId": "lifecycle-task-123",
      "title": "...",
      "description": "...",
      "status": "Active|Completed|...",
      "priority": 1-3,
      "dueDate": "2025-01-20",
      "category": "...",
      "tags": ["..."],
      "estimatedMinutes": 30,
      "assignee": "user@email.com",
      "project": { "externalId": "...", "name": "..." },
      "metadata": { /* source-specific fields */ }
    }
  ]
}
```

**Import Features:**
- File upload with drag-drop
- Auto-detect format (lifecycle export vs generic)
- Field mapping UI (map lifecycle fields to SelfOrganizer fields)
- Preview with validation warnings
- Duplicate detection (by externalId or title+date)
- Merge strategy selection (skip, update, create duplicate)
- Import history tracking

---

### 2. QUICK ACTIONS & EXTREME USABILITY

#### 2.1 Universal Quick Actions Bar
**Priority: HIGH** | Reduces friction for common operations

**Trigger:** `/` or `⌘K` (already implemented), enhance with:

**Context-Aware Actions:**
- When viewing a task: "Complete", "Reschedule", "Add to Project", "Link to Goal"
- When viewing calendar: "Add Event", "Block Focus Time", "Schedule Task"
- When viewing project: "Add Task", "Set Next Action", "Mark Stalled"

**Smart Commands (type to filter):**
```
"schedule [task] for tomorrow 2pm"
"complete [task]"
"link [task] to [project]"
"block 2 hours focus tomorrow"
"reschedule all overdue to next week"
"show tasks @work high energy"
```

#### 2.2 Bulk Operations
**Priority: MEDIUM** | Power user efficiency

**Selection Mode:**
- Shift+click for range selection
- Cmd+click for multi-select
- "Select All" / "Select None" / "Invert"

**Bulk Actions:**
- Complete selected
- Reschedule selected (date picker)
- Change priority
- Add to project
- Add tag
- Change status
- Delete

**Bulk Display:**
- Selection count badge
- Floating action bar when items selected

#### 2.3 Keyboard Navigation Enhancement
**Priority: MEDIUM** | Speed for keyboard users

**List Navigation:**
- `j/k` - Move down/up in lists
- `Enter` - Open selected item
- `Space` - Toggle selection
- `c` - Complete task
- `e` - Edit inline
- `p` - Change priority (cycles 1→2→3→1)
- `s` - Schedule (opens date picker)
- `#` - Add tag
- `@` - Add context

**Global:**
- `⌘+N` - New task (context-aware)
- `⌘+Shift+N` - New project
- `⌘+E` - Quick edit selected
- `⌘+D` - Duplicate selected

#### 2.4 Quick Capture Enhancements
**Priority: MEDIUM** | Faster capture = better capture

**Smart Parsing:**
- Natural language date: "call mom tomorrow" → DueDate = tomorrow
- Priority detection: "urgent: fix bug" → Priority = 1
- Context detection: "buy milk @errands" → Context = errands
- Time estimate: "30m review PR" → EstimatedMinutes = 30
- Project assignment: "#ProjectName task title"

**Quick Capture Widget:**
- Floating button (bottom-right corner)
- Always accessible regardless of page
- Voice input option (if browser supports)
- Auto-suggest from recent contexts/projects

---

### 3. INTELLIGENT AUTOMATION

#### 3.1 Smart Rules Engine
**Priority: MEDIUM** | Reduces manual work

**Rule Structure:**
```
WHEN [trigger] AND [conditions] THEN [actions]
```

**Triggers:**
- Task created
- Task completed
- Task overdue
- Project stalled (no next action for X days)
- Goal progress changed
- Calendar event created
- Daily/weekly schedule

**Conditions:**
- Has tag X
- In project X
- Has context X
- Priority is X
- Estimated time > X
- Title contains X
- Is recurring

**Actions:**
- Add tag
- Set priority
- Move to project
- Set status
- Create follow-up task
- Send notification
- Link to goal

**Example Rules:**
- "When task completed AND has tag #client THEN create follow-up task 'Send update to client'"
- "When task overdue AND priority = High THEN set tag #urgent"
- "When calendar event created AND title contains 'interview' THEN create prep task 30 min before"

#### 3.2 Auto-Scheduling Improvements
**Priority: MEDIUM** | Smarter time blocking

**Enhancements:**
- Respect calendar free/busy (from Graph sync)
- Honor "no meetings" blocks
- Lunch protection (configurable)
- Commute time awareness
- Meeting buffer enforcement
- Deep work preference by time of day
- Energy-aware distribution

**New Options:**
- "Schedule around meetings" toggle
- "Protect lunch hour" toggle
- "Morning = deep work" preset
- "Batch similar tasks" toggle
- "Leave buffer for interruptions" (% of day)

#### 3.3 Recurring Task Enhancements
**Priority: MEDIUM** | Better habit support

**New Recurrence Options:**
- First/last day of month
- Every N weeks on specific days
- After completion + N days (flexible recurrence)
- Skip weekends option
- Seasonal (quarterly tasks)

**Streak Tracking:**
- Current streak count
- Best streak
- Visual streak indicator
- Streak break notifications

**Habit Mode:**
- Separate habits section
- Daily check-in view
- Habit completion calendar (GitHub-style)
- Habit analytics

---

### 4. ENHANCED VIEWS & VISUALIZATION

#### 4.1 Kanban Board View
**Priority: MEDIUM** | Visual task management

**Columns:**
- Configurable (by status, priority, project, context)
- Drag-drop between columns
- WIP limits per column
- Collapse/expand columns
- Quick add in column

**Default Boards:**
- GTD Flow: Inbox → Next Actions → Waiting → Completed
- Priority: High → Medium → Low
- Custom: User-defined columns

#### 4.2 Timeline/Gantt View
**Priority: LOW** | Project planning visualization

**Features:**
- Tasks on timeline by due date
- Dependency arrows
- Milestone markers (goals)
- Drag to reschedule
- Zoom (day/week/month)
- Critical path highlighting

#### 4.3 Energy Map View
**Priority: LOW** | Visual energy planning

**Features:**
- Heat map of scheduled tasks by energy level
- Overlay with personal energy curve
- Identify mismatches (high energy tasks at low energy times)
- Drag to rebalance

#### 4.4 Goals Dashboard
**Priority: MEDIUM** | Strategic overview

**Features:**
- OKR-style view (goals with key results as tasks)
- Progress bars with trend arrows
- At-risk goals highlighted
- Linked tasks/projects summary
- Time remaining countdown
- Goal hierarchy visualization

---

### 5. REVIEW & REFLECTION

#### 5.1 Enhanced Daily Review
**Priority: MEDIUM** | Better daily planning

**New Sections:**
- "What went well yesterday" (completed tasks)
- "What got stuck" (incomplete scheduled)
- "Energy reflection" (was my energy estimate accurate?)
- "Tomorrow prep" (schedule key tasks)
- "Gratitude prompt" (optional)

**Automations:**
- Auto-reschedule incomplete tasks
- Suggest tomorrow's top 3
- Flag stale waiting-for items
- Meeting prep reminders

#### 5.2 Weekly Review Wizard
**Priority: MEDIUM** | Guided GTD review

**Steps:**
1. **Collect** - Process all inboxes (show capture count)
2. **Process** - Review inbox items (quick process UI)
3. **Organize** - Review projects for next actions
4. **Review Goals** - Progress check, adjust if needed
5. **Plan** - Select focus for next week
6. **Reflect** - What worked, what didn't

**Features:**
- Progress indicator (step X of 6)
- Skip steps option
- Time estimate per step
- Save progress (resume later)

#### 5.3 Monthly/Quarterly Review
**Priority: LOW** | Strategic reflection

**Includes:**
- Goal progress summary
- Project completion rate
- Time allocation analysis
- Category distribution
- Context usage patterns
- Productivity trends
- Recommendations

---

### 6. INTEGRATIONS & ECOSYSTEM

#### 6.1 Notification System
**Priority: MEDIUM** | Timely reminders

**Channels:**
- Browser notifications (with permission)
- Email digest (daily/weekly summary)
- Future: Mobile push (if PWA)

**Notification Types:**
- Task due soon (configurable: 1 day, 1 hour, 15 min)
- Task overdue
- Meeting starting soon
- Review reminder
- Goal at risk
- Streak about to break

**Settings:**
- Per-type enable/disable
- Quiet hours
- Notification grouping

#### 6.2 API/Webhooks (Future)
**Priority: LOW** | Extensibility

**REST API:**
- CRUD for all entities
- Search endpoint
- Bulk operations
- Authentication (API keys)

**Webhooks:**
- Task created/completed/updated
- Project completed
- Goal progress changed
- Custom events

#### 6.3 Import Sources
**Priority: MEDIUM** | Bring your data

**Supported Formats:**
- Todoist export
- Things 3 export
- OmniFocus export
- Notion export
- Asana export
- Generic CSV with field mapping

---

### 7. LIFE/MIND MANAGEMENT FEATURES

#### 7.1 Energy & Wellbeing Tracking
**Priority: MEDIUM** | Holistic productivity

**Daily Check-in:**
- Morning energy level (1-5)
- Mood indicator
- Sleep quality (optional)
- Notes

**Insights:**
- Energy vs productivity correlation
- Best days/times for deep work
- Mood trends
- Recommendations

#### 7.2 Focus Session Enhancements
**Priority: MEDIUM** | Deep work support

**New Features:**
- Session intentions ("What will I accomplish?")
- Distraction log (quick note when interrupted)
- Session reflection ("How did it go?")
- Focus score (based on time, completion, distractions)
- Focus streaks
- Ambient sounds/music integration (external link)

#### 7.3 Journaling Integration
**Priority: LOW** | Reflection support

**Features:**
- Daily journal prompt
- Link journal entries to tasks/goals
- Mood tracking over time
- Gratitude practice
- Weekly reflection summaries

#### 7.4 Life Areas Dashboard
**Priority: MEDIUM** | Wheel of life visualization

**Areas:**
- Career, Health, Finance, Relationships, Personal Growth, Recreation, Environment, Spirituality

**Features:**
- Visual wheel showing balance
- Goal distribution across areas
- Task time allocation by area
- Imbalance alerts
- Rebalancing suggestions

---

### 8. DATA & ANALYTICS

#### 8.1 Advanced Reports
**Priority: MEDIUM** | Understand your productivity

**New Reports:**
- Time spent per project/goal
- Task completion velocity
- Estimate accuracy (estimated vs actual)
- Context switching frequency
- Meeting load analysis
- Deep work hours trend
- Procrastination patterns (tasks rescheduled multiple times)

#### 8.2 Personal Analytics Dashboard
**Priority: LOW** | Self-knowledge

**Metrics:**
- Productivity score (composite)
- Focus time percentage
- Task throughput
- On-time completion rate
- Average task age
- Project velocity

**Visualizations:**
- Trend charts
- Heat maps
- Comparative analysis (this week vs last)

---

### 9. SETTINGS & CONFIGURATION

#### 9.1 Data Source Configuration UI
**Priority: HIGH** | Required for sync features

**Per-Source Settings:**
```
Microsoft Calendar:
├── Connection Status: ✓ Connected / ✗ Disconnected
├── Account: user@domain.com
├── Last Sync: Jan 14, 2025 3:45 PM
├── Records: 234 events
├── Actions: [Test Connection] [Configure] [Sync Now]
└── Settings:
    ├── Calendars: [✓] Primary [✓] Work [ ] Holidays
    ├── Sync Range: Past [7] days, Future [30] days
    ├── Auto-sync: Every [15] minutes
    └── Categories: [✓] All or [Select specific]

Lifecycle Tasks:
├── Connection Status: Via Import/Export
├── Last Import: Jan 10, 2025
├── Records: 45 tasks imported
└── Actions: [Import] [Export] [View History]
```

#### 9.2 Profiles/Workspaces
**Priority: LOW** | Context switching

**Features:**
- Multiple profiles (Work, Personal, Side Project)
- Per-profile settings
- Quick switch
- Separate data or shared with filters

---

## Implementation Phases

### Phase 1: Data Jobs Foundation (2-3 weeks)
1. Create Data Jobs page with UI framework
2. Implement SyncJob model and history tracking
3. Build progress tracking service with events
4. Create Data Source configuration UI
5. Implement Microsoft Graph OAuth flow
6. Build calendar sync service
7. Add conflict detection

### Phase 2: Lifecycle Integration (1 week)
1. Define common interchange format
2. Build export in common format
3. Build import with field mapping
4. Add import history tracking
5. Duplicate detection

### Phase 3: Quick Actions & Usability (2 weeks)
1. Enhance command palette with context-aware actions
2. Add smart command parsing
3. Implement bulk selection and operations
4. Add keyboard navigation
5. Enhance quick capture with NLP

### Phase 4: Automation & Intelligence (2-3 weeks)
1. Build rules engine framework
2. Create rule builder UI
3. Implement common rule templates
4. Enhance auto-scheduling
5. Improve recurring task handling

### Phase 5: Views & Visualization (2 weeks)
1. Add Kanban board view
2. Enhance goals dashboard
3. Add life areas visualization
4. Improve reports

### Phase 6: Review & Reflection (1-2 weeks)
1. Enhance daily review
2. Build weekly review wizard
3. Add wellbeing tracking
4. Focus session improvements

---

## Quick Wins (Can implement immediately)

1. **Smart capture parsing** - Parse "tomorrow", "@context", "#project" from capture text
2. **Bulk complete** - Select multiple tasks and complete at once
3. **Quick reschedule** - "Tomorrow", "Next Week", "Next Month" buttons
4. **Stale task indicator** - Visual warning for tasks not touched in 7+ days
5. **Project color in task list** - Show project color dot next to task
6. **Goal progress in task card** - Show which goal a task contributes to
7. **Keyboard shortcuts help** - `?` shows shortcuts overlay
8. **Dark mode calendar** - Ensure calendar respects theme
9. **Export all data** - Single button to export everything as JSON backup
10. **Import from clipboard** - Paste task list, auto-create tasks

---

## Success Metrics

- **Capture to Action < 30 seconds** - Time from thought to captured task
- **Daily Review < 5 minutes** - Quick morning planning
- **Weekly Review < 30 minutes** - Comprehensive but efficient
- **Zero friction context switching** - Keyboard-driven navigation
- **100% calendar visibility** - All events synced and visible
- **Seamless lifecycle integration** - Tasks flow between systems

