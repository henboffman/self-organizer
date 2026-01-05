# Self-Organizer

A GTD-inspired personal task and schedule management application built with Blazor WebAssembly. Features advanced AI-powered task optimization, intelligent scheduling, and ADHD-friendly accommodations.

## Features

### Core GTD Workflow
- **Capture**: Quick capture with Cmd+Enter shortcut, voice input support
- **Inbox Processing**: Clarify and organize captured items into actionable tasks
- **Next Actions**: Context-based task lists (@home, @work, @errands, etc.)
- **Projects**: Multi-step outcomes with subtask support
- **Waiting For**: Track delegated items and their status
- **Someday/Maybe**: Park ideas for future consideration
- **Reference**: Store reference materials and notes

### Calendar & Scheduling
- **Timeline View**: Hour-by-hour visualization with overlap detection
- **Multi-Day Views**: 1-day, 3-day, and 5-day calendar views
- **Smart Blocks**: Automatic prep time and wind-down blocks around meetings
- **Follow-up Tasks**: One-click task creation from meetings requiring follow-up
- **Auto-Scheduling**: AI-powered task scheduling into available time blocks

### Intelligence Features
- **Advanced Task Optimizer**: Multi-objective optimization using 10 scoring dimensions
- **Entity Extraction**: Detects acronyms, proper nouns, and key concepts in task descriptions
- **Critical Path Analysis**: Identifies bottleneck tasks in dependency chains
- **Task Clustering**: K-medoids clustering for optimal batching
- **Energy Matching**: Aligns task difficulty with circadian rhythms

### ADHD Accommodations
- **Pick For Me**: Decision fatigue reduction with smart task selection
- **Focus Timer**: Pomodoro-style timer with customizable intervals
- **Time Blindness Helpers**: Visual time estimation and warnings
- **Celebration Effects**: Positive reinforcement on task completion
- **Hyperfocus Alerts**: Reminders to take breaks during extended focus
- **Minimal Mode**: Reduced visual clutter option

## Technology Stack

- **Frontend**: Blazor WebAssembly (.NET 9)
- **Storage**: IndexedDB (client-side, offline-first)
- **Styling**: Bootstrap 5 with custom CSS variables for theming
- **No External Dependencies**: All intelligence features use pure C# (no ML libraries)

## Architecture

```
src/
├── SelfOrganizer.Core/          # Domain models and interfaces
│   ├── Models/                  # Entity definitions
│   └── Interfaces/              # Service contracts
│
└── SelfOrganizer.App/           # Blazor WebAssembly application
    ├── Components/              # Reusable UI components
    ├── Pages/                   # Route-based pages
    ├── Services/
    │   ├── Data/               # IndexedDB repositories
    │   ├── Domain/             # Business logic services
    │   └── Intelligence/       # AI/optimization services
    └── wwwroot/                # Static assets
```

## Task Optimization Algorithm

The optimizer uses a sophisticated multi-objective optimization approach to prioritize and schedule tasks. This section documents the mathematical models used.

### Score Dimensions

Each task is scored across 10 independent dimensions:

| Dimension | Description | Range |
|-----------|-------------|-------|
| Urgency | Time pressure from deadlines | 0-1.5 |
| Importance | Value and strategic priority | 0-1 |
| Effort | Inverse of task duration (quick wins) | 0-1 |
| Context Fit | Match with available contexts | 0-1 |
| Energy Alignment | Match with current energy level | 0-1 |
| Momentum | Similarity to recently completed work | 0-1 |
| Dependency | Unblocks other tasks / critical path | 0-1 |
| Staleness | Penalty for old, neglected tasks | 0-1 |
| Opportunity Cost | Value relative to alternatives | 0-1 |
| Batching Affinity | Groups well with recent work | 0-1 |

### Temporal Decay Functions

#### Sigmoid Decay
Used for deadline urgency with a sharp transition near the deadline:

```
sigmoid(x) = 1 / (1 + e^(steepness × (x - midpoint)))
```

- `midpoint`: Days until due date where urgency = 0.5
- `steepness`: Controls how sharp the transition is

#### Exponential Decay
Used for gradual urgency increase over longer periods:

```
exponential(x) = 0.5^(x / halfLife)
```

- `halfLife`: Days until urgency drops to 50%

#### Hyperbolic Growth
Used for overdue tasks to model escalating urgency that approaches an asymptote:

```
hyperbolic(x) = max × x / (scale + x)
```

- `max`: Maximum additional urgency (caps at 0.5)
- `scale`: Controls growth rate

### Energy Curve Modeling

The optimizer models human energy using circadian and ultradian rhythms:

```
energy = 2.5 + max(morningPeak, afternoonPeak) - lunchDip + ultradianModulation
```

Where:
- **Morning Peak**: Gaussian centered on user's morning energy peak hour
- **Afternoon Peak**: Gaussian centered on user's afternoon energy peak hour
- **Lunch Dip**: Gaussian centered at 13:30 (post-lunch decrease)
- **Ultradian Modulation**: Sinusoidal ~90 minute cycles

```
morningEnergy = 5.0 × e^(-(hour - morningPeak)² / (2 × 2.0²))
afternoonEnergy = 4.0 × e^(-(hour - afternoonPeak)² / (2 × 2.5²))
lunchDip = 1.5 × e^(-(hour - 13.5)² / (2 × 1.0²))   [if 12.5 ≤ hour ≤ 14.5]
ultradian = 0.3 × sin(2π × (minutes mod 90) / 90)
```

### Dependency Graph Analysis

Uses topological sorting with longest-path calculation to identify critical path:

1. Build directed acyclic graph from `BlockedByTaskIds`
2. Calculate in-degrees for all tasks
3. Process tasks in topological order, tracking longest path to each
4. Trace back from endpoint to identify critical path tasks

Critical path tasks receive a 20% score boost.

### Pareto Optimization

Identifies non-dominated tasks across primary dimensions (urgency, importance, effort, context fit, energy alignment):

**Dominance**: Task A dominates Task B if A is better or equal in all dimensions and strictly better in at least one.

**Pareto Frontier**: Set of tasks not dominated by any other task.

Pareto-optimal tasks receive a 15% score boost.

### K-Medoids Clustering

Groups similar tasks for batching using the PAM (Partitioning Around Medoids) algorithm:

1. **Similarity Matrix**: Jaccard coefficient for set fields (contexts, tags), exact match for categories/projects
2. **Medoid Selection**: Greedy initialization maximizing inter-medoid distance
3. **Assignment**: Each task assigned to nearest medoid
4. **Cohesion**: Average pairwise similarity within cluster

### Adaptive Weights

Weights dynamically adjust based on context:

| Condition | Adjustment |
|-----------|------------|
| High time pressure (≥3 urgent tasks) | Urgency ×1.5, Effort ×1.3, Batching ×0.7 |
| Low energy (≤2) | Energy Alignment ×1.5, Effort ×1.2 |
| Large backlog (>50 tasks) | Importance ×1.2, Urgency ×1.1, Staleness ×0.8 |
| Morning deep work window | Importance ×1.3, Effort ×0.8, Momentum ×1.2 |
| End of day (≥16:00) | Effort ×1.4, Urgency ×1.2 |

### Final Score Computation

Uses weighted geometric mean in log-space for numerical stability:

```
logScore = Σ(weight_i × log(max(0.001, score_i)))
finalScore = e^(logScore / Σweight_i) × 100
```

This ensures multiplicative interaction between dimensions (a zero in any critical dimension significantly impacts the final score).

## User Preferences

### Schedule Optimization Weights (0-100)

| Setting | Default | Description |
|---------|---------|-------------|
| Due Date Urgency | 70 | Weight for approaching deadlines |
| Energy Matching | 50 | Match task energy to time of day |
| Context Grouping | 50 | Group tasks by @context |
| Similar Work Grouping | 50 | Group by category/project |
| Deep Work Preference | 60 | Schedule deep work in morning |
| Stakeholder Grouping | 30 | Group tasks by who they're for |
| Tag Similarity | 40 | Group tasks with similar tags |
| Blocked Task Penalty | 100 | Deprioritize blocked tasks (100 = exclude) |

### Work Schedule Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Work Day Start | 9:00 AM | Start of schedulable time |
| Work Day End | 5:00 PM | End of schedulable time |
| Work Days | Mon-Fri | Days available for scheduling |
| Default Task Duration | 30 min | When task has no estimate |
| Minimum Usable Block | 15 min | Smallest schedulable gap |
| Deep Work Minimum | 60 min | Minimum block for deep work |
| Morning Energy Peak | 10 AM | Hour of peak morning energy |
| Afternoon Energy Peak | 3 PM | Hour of peak afternoon energy |

## Getting Started

### Prerequisites

- .NET 9 SDK
- Modern web browser with IndexedDB support

### Running Locally

```bash
cd src/SelfOrganizer.App
dotnet run
```

Navigate to `https://localhost:5001` in your browser.

### Building for Production

```bash
dotnet publish -c Release
```

Output will be in `src/SelfOrganizer.App/bin/Release/net9.0/publish/wwwroot/`

## Data Storage

All data is stored locally in the browser's IndexedDB. No server required.

### Stores

- `captures`: Quick capture items pending processing
- `tasks`: All task/action items
- `projects`: Project definitions and metadata
- `events`: Calendar events
- `timeblocks`: Scheduled time blocks
- `contacts`: Contact/stakeholder information
- `references`: Reference materials
- `contexts`: Context definitions (@home, @work, etc.)
- `categories`: Category definitions with default prep/decompress times
- `preferences`: User preferences (single record)
- `dailysnapshots`: Daily productivity snapshots for reporting

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Cmd/Ctrl + Enter | Quick capture (from any page) |
| Escape | Close modal/cancel action |

## License

MIT License - See LICENSE file for details.
