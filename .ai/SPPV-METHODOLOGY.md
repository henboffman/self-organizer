# Series-Parallel Poset Verification (SPPV)

## A Methodology for AI Agent-Driven Software Development

---

## Table of Contents

1. [Introduction](#introduction)
2. [Core Concepts](#core-concepts)
3. [Why SPPV for Generative AI Agents](#why-sppv-for-generative-ai-agents)
4. [SPPV Work Item Structure](#sppv-work-item-structure)
5. [Composition Operators](#composition-operators)
6. [Verification Gates](#verification-gates)
7. [Definition of Done](#definition-of-done)
8. [Workflow Execution Model](#workflow-execution-model)
9. [Agent Orchestration Patterns](#agent-orchestration-patterns)
10. [Progress Tracking](#progress-tracking)
11. [Error Handling & Recovery](#error-handling--recovery)
12. [Practical Examples](#practical-examples)
13. [SPPV Schema Reference](#sppv-schema-reference)
14. [Best Practices](#best-practices)

---

## Introduction

### What is SPPV?

**Series-Parallel Poset Verification (SPPV)** is a methodology for decomposing, organizing, and executing software development work using generative AI agents. It combines:

- **Poset (Partially Ordered Set)**: A mathematical structure where work items have defined ordering relationships—some must happen before others (dependencies), while some are independent (parallelizable)
- **Series-Parallel Composition**: A restricted class of posets built only from sequential (→) and parallel (||) operations, making them easy to reason about, visualize, and execute
- **Verification**: Explicit gates at each node that validate preconditions, postconditions, and acceptance criteria before allowing work to proceed

### The Core Insight

Traditional project management (Kanban, Scrum) was designed for human teams with implicit knowledge sharing and real-time communication. AI agents operate differently:

- They have **no implicit context**—everything must be explicit
- They can **scale horizontally**—multiple agents can work simultaneously
- They require **precise specifications**—ambiguity leads to incorrect outputs
- They benefit from **formal verification**—automated checks catch errors early

SPPV addresses these characteristics by providing a **machine-parseable, mathematically grounded** structure for defining and executing work.

---

## Core Concepts

### Posets (Partially Ordered Sets)

A poset is a set of elements with a partial order relation (≤) that is:

- **Reflexive**: a ≤ a (every element relates to itself)
- **Antisymmetric**: if a ≤ b and b ≤ a, then a = b
- **Transitive**: if a ≤ b and b ≤ c, then a ≤ c

In SPPV, elements are **work items** and the relation represents **"must complete before"**.

```
Example Poset:
    A
   / \
  B   C
   \ /
    D

Interpretation:
- A must complete before B and C
- B and C are incomparable (can run in parallel)
- Both B and C must complete before D
```

### Series-Parallel Restriction

Not all posets are series-parallel. SP-posets are built recursively using only two operations:

1. **Series Composition (→)**: Sequential execution
2. **Parallel Composition (||)**: Concurrent execution

This restriction is **intentional**—SP-posets have several desirable properties:

- **Polynomial-time schedulability**: Optimal execution order can be computed efficiently
- **No N-shaped dependencies**: Avoids complex cross-dependencies that are hard to reason about
- **Visual clarity**: Can always be drawn without edge crossings
- **Compositional reasoning**: Properties of the whole derive from properties of parts

### Verification Gates

Every work item in SPPV has three verification points:

```
┌─────────────────────────────────────────────┐
│              WORK ITEM                      │
├─────────────────────────────────────────────┤
│  ▼ PRECONDITION GATE                        │
│    - Dependencies satisfied?                │
│    - Required inputs exist?                 │
│    - Environment ready?                     │
│                                             │
│  ══════════════════════════                 │
│  █ EXECUTION                                │
│  ══════════════════════════                 │
│                                             │
│  ▼ POSTCONDITION GATE                       │
│    - Outputs produced?                      │
│    - Tests passing?                         │
│    - No regressions?                        │
│                                             │
│  ▼ ACCEPTANCE GATE                          │
│    - Meets requirements?                    │
│    - Quality standards met?                 │
│    - Ready for dependents?                  │
└─────────────────────────────────────────────┘
```

---

## Why SPPV for Generative AI Agents

### Problem: Traditional Methods Fall Short

| Challenge | Traditional Approach | Problem for AI Agents |
|-----------|---------------------|----------------------|
| Ambiguous requirements | Human judgment fills gaps | AI may hallucinate or make wrong assumptions |
| Implicit dependencies | Team knowledge, standup discussions | AI has no memory between tasks |
| Progress tracking | Human observation, status updates | Need machine-readable state |
| Quality assurance | Code review, manual testing | Need automated verification |
| Parallelization | Limited by team size | AI can scale infinitely if work is decomposed |

### Solution: SPPV Addresses Each Challenge

| Challenge | SPPV Approach |
|-----------|---------------|
| Ambiguous requirements | Formal specification with explicit inputs/outputs |
| Implicit dependencies | Explicit dependency graph (poset structure) |
| Progress tracking | State machine with verified transitions |
| Quality assurance | Automated verification gates |
| Parallelization | Parallel composition identifies concurrent work |

### The AI Agent Sweet Spot

SPPV works well with AI agents because:

1. **Agents excel at well-defined, bounded tasks**: SPPV decomposes work into atomic units
2. **Agents can run in parallel**: SPPV explicitly models parallelism
3. **Agents need clear success criteria**: SPPV requires verification at every step
4. **Agents benefit from context**: SPPV carries forward artifacts between tasks
5. **Agents make mistakes**: SPPV catches errors before they propagate

---

## SPPV Work Item Structure

### Anatomy of an SPPV Work Item

Every work item in SPPV follows this structure:

```yaml
id: string                    # Unique identifier (e.g., "E7.F2.W3")
name: string                  # Human-readable name
type: epic | feature | task   # Granularity level

# CONTEXT
description: string           # What this work item accomplishes
rationale: string            # Why this work item exists
references:                  # Links to requirements, designs, research
  - type: requirement
    id: string
    
# DEPENDENCIES (defines the poset structure)
dependencies:
  - id: string               # Work item ID that must complete first
    artifacts:               # Specific outputs needed from dependency
      - name: string
        path: string
        
# INPUTS (what the agent needs to begin)
inputs:
  - name: string
    type: file | artifact | config | context
    source: string           # Path or reference
    required: boolean
    
# SPECIFICATION (what the agent must produce)
specification:
  objective: string          # Clear statement of goal
  constraints:               # Boundaries and limitations
    - string
  guidance:                  # Implementation hints
    - string
  anti-patterns:             # What to avoid
    - string
    
# OUTPUTS (what the agent must produce)
outputs:
  - name: string
    type: file | artifact | test | documentation
    path: string
    description: string
    
# VERIFICATION
verification:
  preconditions:             # Must be true before starting
    - check: string
      automated: boolean
      command: string        # If automated
  postconditions:            # Must be true after completion
    - check: string
      automated: boolean
      command: string
  acceptance_criteria:       # Definition of done
    - criterion: string
      verification: string   # How to verify
      
# COMPOSITION (how this relates to siblings)
composition:
  type: series | parallel
  group: string              # Composition group ID
  
# METADATA
metadata:
  estimated_complexity: low | medium | high
  skills_required:
    - string
  created_at: datetime
  status: pending | ready | in_progress | verifying | done | blocked | failed
```

### Work Item Hierarchy

SPPV uses three levels of granularity:

```
EPIC (E)
├── High-level capability or feature area
├── Contains multiple Features
├── Example: "E7: Nutrition & Fasting Protocols"
│
└── FEATURE (F)
    ├── Discrete, deliverable functionality
    ├── Contains multiple Tasks
    ├── Example: "F7.2: Fasting Protocols"
    │
    └── TASK (T)
        ├── Atomic unit of work for a single agent
        ├── Completable in one session
        ├── Example: "T7.2.1: Create FMD tracking data model"
```

### Granularity Guidelines

| Level | Scope | Duration | Agent Sessions |
|-------|-------|----------|----------------|
| Epic | Major capability | 2-8 weeks | Many |
| Feature | Shippable increment | 2-10 days | Several |
| Task | Atomic work unit | 1-4 hours | One |

**The key rule**: A Task should be completable by a single AI agent in a single session with all needed context provided upfront.

---

## Composition Operators

### Series Composition (→)

Series composition creates sequential dependencies.

```
Notation: A → B
Meaning: B cannot start until A completes and passes verification

Visual:
┌───┐     ┌───┐
│ A │────▶│ B │
└───┘     └───┘

Properties:
- A's outputs become B's inputs
- B's preconditions include A's completion
- Total duration = duration(A) + duration(B)
```

**Example**:
```yaml
# Task A: Create the data model
- id: T7.2.1
  name: "Create FMD tracking data model"
  outputs:
    - name: FmdSessionEntity
      path: src/Features/Fasting/Data/FmdSessionEntity.cs

# Task B: Create the service (depends on model)
- id: T7.2.2
  name: "Create FMD tracking service"
  dependencies:
    - id: T7.2.1
      artifacts:
        - name: FmdSessionEntity
          path: src/Features/Fasting/Data/FmdSessionEntity.cs
```

### Parallel Composition (||)

Parallel composition allows concurrent execution.

```
Notation: A || B
Meaning: A and B can execute simultaneously; no dependency between them

Visual:
        ┌───┐
    ┌──▶│ A │──┐
    │   └───┘  │
┌───┴───┐   ┌──▼──┐
│ Start │   │ Join│
└───┬───┘   └──▲──┘
    │   ┌───┐  │
    └──▶│ B │──┘
        └───┘

Properties:
- A and B share no dependencies on each other
- Both can be assigned to different agents simultaneously
- Total duration = max(duration(A), duration(B))
```

**Example**:
```yaml
# These tasks can run in parallel
composition_group: "F7.2.parallel_1"

- id: T7.2.3
  name: "Create FMD calendar component"
  composition:
    type: parallel
    group: "F7.2.parallel_1"

- id: T7.2.4
  name: "Create FMD meal suggestions component"
  composition:
    type: parallel
    group: "F7.2.parallel_1"

# Both must complete before this task
- id: T7.2.5
  name: "Create FMD dashboard page"
  dependencies:
    - id: T7.2.3
    - id: T7.2.4
```

### Compound Compositions

Series and parallel can be nested arbitrarily:

```
Expression: (A → B) || (C → D) → E

Visual:
        ┌───┐     ┌───┐
    ┌──▶│ A │────▶│ B │──┐
    │   └───┘     └───┘  │
┌───┴───┐             ┌──▼──┐     ┌───┐
│ Start │             │ Join│────▶│ E │
└───┬───┘             └──▲──┘     └───┘
    │   ┌───┐     ┌───┐  │
    └──▶│ C │────▶│ D │──┘
        └───┘     └───┘

Meaning:
- A must complete before B
- C must complete before D
- (A→B) can run in parallel with (C→D)
- E waits for both branches to complete
```

### Composition Rules

1. **No cycles**: The dependency graph must be acyclic (DAG)
2. **No N-patterns**: Avoid structures that aren't series-parallel
3. **Explicit joins**: When parallel paths converge, create an explicit join task
4. **Artifact flow**: Outputs must flow forward through the graph

**Invalid N-Pattern (to avoid)**:
```
    A       B
    |\     /|
    | \   / |
    |  \ /  |
    |   X   |    ← This cross-dependency makes it non-SP
    |  / \  |
    | /   \ |
    |/     \|
    C       D

If you need this pattern, decompose into:
A → C, A → temp, temp || B → D
```

---

## Verification Gates

### Gate Types

#### 1. Precondition Gate (Entry)

Validates that all prerequisites are met before an agent begins work.

```yaml
preconditions:
  # Dependency verification
  - check: "All dependencies completed"
    automated: true
    command: "sppv verify-deps {task_id}"
    
  # Artifact existence
  - check: "Required input files exist"
    automated: true
    command: |
      test -f src/Features/Fasting/Data/FmdSessionEntity.cs
      
  # Environment readiness
  - check: "Database migrations applied"
    automated: true
    command: "dotnet ef database update --dry-run"
    
  # Build state
  - check: "Solution compiles without errors"
    automated: true
    command: "dotnet build --no-restore"
```

#### 2. Postcondition Gate (Output)

Validates that the agent produced the expected outputs.

```yaml
postconditions:
  # Output existence
  - check: "Service file created"
    automated: true
    command: "test -f src/Features/Fasting/Services/FmdTrackingService.cs"
    
  # Compilation
  - check: "New code compiles"
    automated: true
    command: "dotnet build"
    
  # Tests pass
  - check: "Unit tests pass"
    automated: true
    command: "dotnet test --filter 'FullyQualifiedName~Fasting'"
    
  # No regressions
  - check: "All existing tests still pass"
    automated: true
    command: "dotnet test"
    
  # Static analysis
  - check: "No new warnings"
    automated: true
    command: "dotnet build /warnaserror"
```

#### 3. Acceptance Gate (Quality)

Validates that the work meets requirements and quality standards.

```yaml
acceptance_criteria:
  # Functional requirements
  - criterion: "FMD sessions can be created with 5-day duration"
    verification: "Integration test CreateFmdSession_FiveDays_Succeeds"
    
  # API contract
  - criterion: "Endpoint returns correct schema"
    verification: "OpenAPI schema validation"
    
  # Performance
  - criterion: "Endpoint responds in <500ms"
    verification: "Performance test with k6"
    
  # Documentation
  - criterion: "README.md updated with feature documentation"
    verification: "Manual review or LLM-based doc check"
    
  # Code quality
  - criterion: "Code follows project conventions"
    verification: "dotnet format --verify-no-changes"
```

### Gate Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    TASK EXECUTION FLOW                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────┐                                                │
│  │ PENDING │ ─── Task defined but not ready                 │
│  └────┬────┘                                                │
│       │                                                     │
│       ▼ Dependencies complete?                              │
│  ┌─────────┐                                                │
│  │  READY  │ ─── Task can be picked up by agent             │
│  └────┬────┘                                                │
│       │                                                     │
│       ▼ Run precondition checks                             │
│  ╔═══════════════════════════════════════════════════════╗  │
│  ║ PRECONDITION GATE                                     ║  │
│  ║  □ Dependencies verified                              ║  │
│  ║  □ Inputs available                                   ║  │
│  ║  □ Environment ready                                  ║  │
│  ╚═══════════════════════════════════════════════════════╝  │
│       │                                                     │
│       ▼ All checks pass?                                    │
│       │                                                     │
│  ┌────┴─────┐ No ──▶ ┌─────────┐                           │
│  │          │        │ BLOCKED │ ─── Cannot proceed         │
│  └────┬─────┘        └─────────┘                           │
│       │ Yes                                                 │
│       ▼                                                     │
│  ┌─────────────┐                                            │
│  │ IN_PROGRESS │ ─── Agent executing                        │
│  └──────┬──────┘                                            │
│         │                                                   │
│         ▼ Agent completes                                   │
│  ╔═══════════════════════════════════════════════════════╗  │
│  ║ POSTCONDITION GATE                                    ║  │
│  ║  □ Outputs created                                    ║  │
│  ║  □ Code compiles                                      ║  │
│  ║  □ Tests pass                                         ║  │
│  ╚═══════════════════════════════════════════════════════╝  │
│         │                                                   │
│         ▼ All checks pass?                                  │
│         │                                                   │
│  ┌──────┴──────┐ No ──▶ ┌────────┐                         │
│  │             │        │ FAILED │ ─── Retry or escalate    │
│  └──────┬──────┘        └────────┘                         │
│         │ Yes                                               │
│         ▼                                                   │
│  ┌───────────┐                                              │
│  │ VERIFYING │ ─── Running acceptance checks                │
│  └─────┬─────┘                                              │
│        │                                                    │
│  ╔═══════════════════════════════════════════════════════╗  │
│  ║ ACCEPTANCE GATE                                       ║  │
│  ║  □ Requirements met                                   ║  │
│  ║  □ Quality standards                                  ║  │
│  ║  □ Ready for dependents                               ║  │
│  ╚═══════════════════════════════════════════════════════╝  │
│        │                                                    │
│        ▼ All criteria met?                                  │
│        │                                                    │
│  ┌─────┴─────┐ No ──▶ ┌────────┐                           │
│  │           │        │ FAILED │ ─── Rework required        │
│  └─────┬─────┘        └────────┘                           │
│        │ Yes                                                │
│        ▼                                                    │
│  ┌──────────┐                                               │
│  │   DONE   │ ─── Dependents unblocked                      │
│  └──────────┘                                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Definition of Done

### Task-Level DoD

A Task is DONE when:

```yaml
definition_of_done:
  task:
    required:
      - All specified outputs exist at declared paths
      - Code compiles without errors
      - All new code has corresponding tests
      - All tests pass (new and existing)
      - No increase in technical debt (static analysis)
      - Code follows project style conventions
      - Task-specific acceptance criteria verified
    recommended:
      - Documentation updated if applicable
      - No TODO comments left unresolved
      - Edge cases considered and handled
```

### Feature-Level DoD

A Feature is DONE when:

```yaml
definition_of_done:
  feature:
    required:
      - All child Tasks are DONE
      - Feature README.md is complete
      - Integration tests pass
      - API documentation generated (if applicable)
      - Feature can be demonstrated end-to-end
      - No P0 or P1 bugs in feature scope
    recommended:
      - Performance benchmarks established
      - Monitoring/logging in place
      - Feature flag for gradual rollout (if applicable)
```

### Epic-Level DoD

An Epic is DONE when:

```yaml
definition_of_done:
  epic:
    required:
      - All child Features are DONE
      - Epic documentation complete
      - Cross-feature integration verified
      - User acceptance criteria met
      - Security review passed (if applicable)
    recommended:
      - Load testing completed
      - Rollback procedure documented
      - Stakeholder sign-off
```

### Verification Matrix

| Check | Task | Feature | Epic | Automated? |
|-------|------|---------|------|------------|
| Outputs exist | ✓ | ✓ | ✓ | Yes |
| Compiles | ✓ | ✓ | ✓ | Yes |
| Unit tests pass | ✓ | ✓ | ✓ | Yes |
| Integration tests pass | | ✓ | ✓ | Yes |
| E2E tests pass | | | ✓ | Yes |
| Static analysis clean | ✓ | ✓ | ✓ | Yes |
| Documentation updated | ○ | ✓ | ✓ | Partial |
| Code review | ○ | ✓ | ✓ | Partial (LLM) |
| Security scan | | ○ | ✓ | Yes |
| Performance benchmarks | | ○ | ✓ | Yes |

✓ = Required, ○ = Recommended

---

## Workflow Execution Model

### Scheduler Algorithm

The SPPV scheduler determines which tasks are ready to execute:

```python
def get_ready_tasks(poset):
    ready = []
    for task in poset.tasks:
        if task.status == 'PENDING':
            # Check if all dependencies are DONE
            deps_satisfied = all(
                dep.status == 'DONE' 
                for dep in task.dependencies
            )
            if deps_satisfied:
                task.status = 'READY'
                ready.append(task)
    return ready

def execute_parallel_batch(ready_tasks, agent_pool):
    # Assign tasks to available agents
    assignments = []
    for task in ready_tasks:
        agent = agent_pool.get_available()
        if agent:
            assignments.append((agent, task))
    
    # Execute in parallel
    results = parallel_execute(assignments)
    
    # Process results and update statuses
    for task, result in results:
        if result.success:
            run_postcondition_gate(task)
        else:
            task.status = 'FAILED'
            handle_failure(task, result)
```

### Execution Visualization

```
Timeline:
─────────────────────────────────────────────────────────▶ time

Agents:
Agent 1: ████ T1 ████│░░░░░░░░░│████ T4 ████│████ T7 ████
Agent 2: │████ T2 ████████████│████ T5 ████│░░░░░░░░░░░░
Agent 3: │████ T3 ████│░░░░░░░│████ T6 ████│░░░░░░░░░░░░

Legend:
████ = Executing
│    = Waiting (verification or dependency)
░░░░ = Idle

Dependency Graph:
    T1 ─────┐
            ├──▶ T4 ──▶ T7
    T2 ─────┤
            │
    T3 ─────┴──▶ T5
                 │
                 ▼
                T6
```

### Checkpointing

SPPV supports checkpointing for long-running workflows:

```yaml
checkpoint:
  # State persisted after each task completion
  persistence:
    - task_statuses
    - artifact_registry
    - verification_results
    - agent_context_snapshots
    
  # Recovery from checkpoint
  recovery:
    - Load last checkpoint
    - Verify artifact integrity
    - Resume from first non-DONE task
    - Re-run failed tasks
```

---

## Agent Orchestration Patterns

### Pattern 1: Single Agent, Sequential

For simple workflows or when parallelism isn't needed:

```
┌─────────┐     ┌─────────┐     ┌─────────┐
│ Agent 1 │────▶│ Agent 1 │────▶│ Agent 1 │
│  Task A │     │  Task B │     │  Task C │
└─────────┘     └─────────┘     └─────────┘
```

**Use when**: 
- Tasks have tight coupling
- Shared context is important
- Debugging/tracing is priority

### Pattern 2: Multiple Agents, Parallel

For independent work streams:

```
┌─────────┐
│ Agent 1 │────▶ Stream 1: T1 → T2 → T3
└─────────┘

┌─────────┐
│ Agent 2 │────▶ Stream 2: T4 → T5
└─────────┘

┌─────────┐
│ Agent 3 │────▶ Stream 3: T6 → T7 → T8
└─────────┘
```

**Use when**:
- Independent feature development
- Maximum throughput needed
- Tasks are well-isolated

### Pattern 3: Worker Pool

Dynamic assignment from a ready queue:

```
            ┌───────────────────────┐
            │     Ready Queue       │
            │  [T1, T2, T3, T4, T5] │
            └───────────┬───────────┘
                        │
        ┌───────────────┼───────────────┐
        ▼               ▼               ▼
   ┌─────────┐     ┌─────────┐     ┌─────────┐
   │ Agent 1 │     │ Agent 2 │     │ Agent 3 │
   │  (T1)   │     │  (T2)   │     │  (T3)   │
   └─────────┘     └─────────┘     └─────────┘
```

**Use when**:
- Variable task durations
- Elastic scaling needed
- Optimal resource utilization

### Pattern 4: Specialist Agents

Different agents for different task types:

```
                    ┌─────────────────┐
                    │   Dispatcher    │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  Backend Agent  │ │ Frontend Agent  │ │  Testing Agent  │
│                 │ │                 │ │                 │
│ - Data models   │ │ - Components    │ │ - Unit tests    │
│ - Services      │ │ - Pages         │ │ - Integration   │
│ - APIs          │ │ - Styling       │ │ - E2E           │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

**Use when**:
- Tasks require specialized prompts/context
- Different tools/capabilities needed
- Quality benefits from specialization

### Pattern 5: Hierarchical Orchestration

Agents managing other agents:

```
                    ┌─────────────────┐
                    │ Orchestrator    │
                    │ Agent           │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Feature Lead    │ │ Feature Lead    │ │ Feature Lead    │
│ Agent (Epic 1)  │ │ Agent (Epic 2)  │ │ Agent (Epic 3)  │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘
         │                   │                   │
    ┌────┴────┐         ┌────┴────┐         ┌────┴────┐
    ▼         ▼         ▼         ▼         ▼         ▼
 Worker    Worker    Worker    Worker    Worker    Worker
```

**Use when**:
- Large, complex projects
- Need for coordination and planning
- Adaptive replanning required

---

## Progress Tracking

### Status States

```yaml
statuses:
  PENDING:
    description: "Defined but dependencies not met"
    allowed_transitions: [READY, BLOCKED]
    
  READY:
    description: "Dependencies met, can be executed"
    allowed_transitions: [IN_PROGRESS, BLOCKED]
    
  IN_PROGRESS:
    description: "Agent actively working"
    allowed_transitions: [VERIFYING, FAILED]
    
  VERIFYING:
    description: "Post-execution verification running"
    allowed_transitions: [DONE, FAILED]
    
  DONE:
    description: "Complete and verified"
    allowed_transitions: []  # Terminal state
    
  BLOCKED:
    description: "Cannot proceed due to external issue"
    allowed_transitions: [READY, PENDING]
    
  FAILED:
    description: "Execution or verification failed"
    allowed_transitions: [READY, BLOCKED]  # After fix
```

### Progress Metrics

```yaml
metrics:
  completion:
    - total_tasks: count(*)
    - done_tasks: count(status='DONE')
    - completion_percentage: done_tasks / total_tasks
    
  velocity:
    - tasks_per_day: count(completed_today)
    - rolling_average: avg(tasks_per_day, last_7_days)
    - projected_completion: remaining_tasks / rolling_average
    
  parallelism:
    - max_parallel: max_width(poset)
    - actual_parallel: avg(concurrent_tasks)
    - utilization: actual_parallel / available_agents
    
  quality:
    - first_pass_rate: count(passed_first_try) / count(attempted)
    - rework_rate: count(failed_then_fixed) / count(completed)
    - verification_time: avg(verification_duration)
```

### Progress Visualization

```
Epic: E7 - Nutrition & Fasting Protocols
Progress: ████████████░░░░░░░░ 60% (12/20 tasks)

Features:
  F7.1 Dietary Profile     ████████████████████ 100% ✓
  F7.2 Fasting Protocols   ████████████░░░░░░░░  60%
  F7.3 Meal Logging        ████████░░░░░░░░░░░░  40%
  F7.4 Recommendations     ░░░░░░░░░░░░░░░░░░░░   0%

Current Execution:
  ┌─────────────────────────────────────────────┐
  │ Agent 1: T7.2.4 - FMD calendar component    │ ████████░░ 80%
  │ Agent 2: T7.3.2 - Food search integration   │ ██████░░░░ 60%
  │ Agent 3: [idle - waiting for T7.2.4]        │
  └─────────────────────────────────────────────┘

Blockers:
  ⚠ T7.3.5 - Blocked: Waiting for Nutritionix API key

Timeline:
  Started: 2025-01-15
  Today:   2025-01-20 (Day 5)
  ETA:     2025-01-28 (8 days remaining)
```

---

## Error Handling & Recovery

### Failure Categories

```yaml
failure_types:
  PRECONDITION_FAILURE:
    description: "Entry gate failed"
    causes:
      - Missing dependency artifacts
      - Environment not ready
      - Configuration error
    recovery:
      - Identify missing precondition
      - Fix upstream issue or environment
      - Re-queue task
      
  EXECUTION_FAILURE:
    description: "Agent failed during execution"
    causes:
      - Agent error/hallucination
      - Timeout
      - External service failure
    recovery:
      - Analyze agent output
      - Adjust specification if ambiguous
      - Retry with refined prompt
      - Escalate if repeated failure
      
  POSTCONDITION_FAILURE:
    description: "Output verification failed"
    causes:
      - Missing outputs
      - Compilation errors
      - Test failures
    recovery:
      - Provide failure details to agent
      - Re-execute with error context
      - Reduce task scope if too complex
      
  ACCEPTANCE_FAILURE:
    description: "Quality gate failed"
    causes:
      - Requirements not met
      - Performance issues
      - Integration problems
    recovery:
      - Review acceptance criteria
      - Create fix-up task
      - May need task decomposition
```

### Retry Strategy

```yaml
retry_policy:
  max_retries: 3
  
  backoff:
    type: exponential
    initial_delay: 30s
    max_delay: 10m
    
  context_enrichment:
    retry_1:
      - Include previous error message
      - Highlight specific failure point
    retry_2:
      - Include more examples
      - Simplify specification
    retry_3:
      - Decompose into smaller tasks
      - Human review trigger
      
  escalation:
    after_max_retries:
      - Mark task BLOCKED
      - Notify human operator
      - Log detailed diagnostics
```

### Rollback Procedures

```yaml
rollback:
  triggers:
    - Critical verification failure
    - Cascade failures (>3 dependent tasks fail)
    - Manual intervention request
    
  procedure:
    - Halt all in-progress tasks in affected branch
    - Identify last known good state (checkpoint)
    - Revert artifacts to checkpoint state
    - Reset task statuses to checkpoint state
    - Analyze failure cause
    - Create remediation plan
    - Resume from checkpoint
```

---

## Practical Examples

### Example 1: Simple Feature (Linear)

```yaml
feature:
  id: F7.1
  name: "Dietary Profile Setup"
  
tasks:
  - id: T7.1.1
    name: "Create DietaryProfile entity"
    outputs:
      - path: src/Features/Nutrition/Data/DietaryProfileEntity.cs
    
  - id: T7.1.2
    name: "Create DietaryProfile service"
    dependencies: [T7.1.1]
    outputs:
      - path: src/Features/Nutrition/Services/DietaryProfileService.cs
      
  - id: T7.1.3
    name: "Create profile API endpoints"
    dependencies: [T7.1.2]
    outputs:
      - path: src/Features/Nutrition/Endpoints/GetDietaryProfile.cs
      - path: src/Features/Nutrition/Endpoints/UpdateDietaryProfile.cs
      
  - id: T7.1.4
    name: "Create profile UI component"
    dependencies: [T7.1.3]
    outputs:
      - path: src/Features/Nutrition/Components/DietaryProfilePage.razor

composition: T7.1.1 → T7.1.2 → T7.1.3 → T7.1.4
```

### Example 2: Feature with Parallelism

```yaml
feature:
  id: F7.2
  name: "Fasting Protocols"
  
tasks:
  # Foundation (sequential)
  - id: T7.2.1
    name: "Create fasting data models"
    
  - id: T7.2.2
    name: "Create fasting service"
    dependencies: [T7.2.1]
    
  # UI Components (parallel)
  - id: T7.2.3
    name: "Create fasting timer component"
    dependencies: [T7.2.2]
    composition: { type: parallel, group: "ui-components" }
    
  - id: T7.2.4
    name: "Create FMD calendar component"
    dependencies: [T7.2.2]
    composition: { type: parallel, group: "ui-components" }
    
  - id: T7.2.5
    name: "Create fasting history chart"
    dependencies: [T7.2.2]
    composition: { type: parallel, group: "ui-components" }
    
  # Integration (sequential after parallel)
  - id: T7.2.6
    name: "Create fasting dashboard page"
    dependencies: [T7.2.3, T7.2.4, T7.2.5]
    
  - id: T7.2.7
    name: "Create fasting feature tests"
    dependencies: [T7.2.6]

composition: T7.2.1 → T7.2.2 → (T7.2.3 || T7.2.4 || T7.2.5) → T7.2.6 → T7.2.7

# Visual:
#                    ┌─ T7.2.3 ─┐
# T7.2.1 → T7.2.2 ──┼─ T7.2.4 ─┼── T7.2.6 → T7.2.7
#                    └─ T7.2.5 ─┘
```

### Example 3: Full Task Specification

```yaml
task:
  id: T7.2.2
  name: "Create fasting tracking service"
  type: task
  
  description: |
    Implement the core fasting tracking service that manages fasting sessions,
    calculates fasting metrics, and integrates with the user's eating window
    preferences.
    
  rationale: |
    The fasting service is the business logic layer between the data models
    and the API/UI layers. It encapsulates all fasting-related calculations
    and state management.
    
  references:
    - type: requirement
      id: F7.2
      section: "Fasting Protocols"
    - type: research
      id: "Longo-FMD-Protocol"
      url: "https://valterlongo.com/fasting-mimicking-program-and-longevity/"
      
  dependencies:
    - id: T7.2.1
      artifacts:
        - name: FastingSessionEntity
          path: src/Features/Fasting/Data/FastingSessionEntity.cs
        - name: FastingType enum
          path: src/Features/Fasting/Models/FastingType.cs
          
  inputs:
    - name: Data models
      type: artifact
      source: T7.2.1
      required: true
    - name: User preferences service
      type: context
      source: src/Features/UserProfile/Services/UserPreferencesService.cs
      required: true
    - name: CLAUDE.md
      type: context
      source: CLAUDE.md
      required: true
      
  specification:
    objective: |
      Create an IFastingService interface and FastingService implementation
      that provides:
      1. Start/stop fasting session management
      2. Current fasting state queries
      3. Fasting metrics calculations (duration, streak, weekly totals)
      4. FMD protocol management (5-day cycles)
      5. Integration with user eating window preferences
      
    constraints:
      - Must follow vertical slice architecture (all code in Features/Fasting/)
      - Must use ICurrentUserService to scope queries to current user
      - Must not expose Entity types to API layer (use DTOs)
      - All public methods must be async
      - Must include XML documentation
      
    guidance:
      - Reference existing services for patterns (e.g., HealthTrackingService)
      - Use Result<T> pattern for operations that can fail
      - Consider edge cases: overlapping sessions, timezone handling
      - FMD sessions have specific states: Day1, Day2-5, Completed, Abandoned
      
    anti_patterns:
      - Do not put business logic in the Entity classes
      - Do not use static methods or singletons
      - Do not hardcode magic numbers (extract to constants)
      - Do not skip validation
      
  outputs:
    - name: IFastingService interface
      type: file
      path: src/Features/Fasting/Services/IFastingService.cs
      description: Interface defining fasting service contract
      
    - name: FastingService implementation
      type: file
      path: src/Features/Fasting/Services/FastingService.cs
      description: Implementation of fasting service
      
    - name: Fasting DTOs
      type: file
      path: src/Features/Fasting/Models/FastingDtos.cs
      description: DTOs for fasting service responses
      
    - name: Service registration
      type: modification
      path: src/Features/Fasting/FastingRegistration.cs
      description: Add service to DI container
      
  verification:
    preconditions:
      - check: "FastingSessionEntity exists"
        automated: true
        command: "test -f src/Features/Fasting/Data/FastingSessionEntity.cs"
        
      - check: "Solution compiles"
        automated: true
        command: "dotnet build"
        
    postconditions:
      - check: "Interface file created"
        automated: true
        command: "test -f src/Features/Fasting/Services/IFastingService.cs"
        
      - check: "Implementation file created"
        automated: true
        command: "test -f src/Features/Fasting/Services/FastingService.cs"
        
      - check: "Solution still compiles"
        automated: true
        command: "dotnet build"
        
      - check: "No new warnings"
        automated: true
        command: "dotnet build 2>&1 | grep -c 'warning' | test $(cat) -eq 0"
        
    acceptance_criteria:
      - criterion: "Can start a TRF fasting session"
        verification: "Unit test: StartFastingSession_TRF_CreatesSession"
        
      - criterion: "Can query current fasting state"
        verification: "Unit test: GetCurrentState_ActiveSession_ReturnsCorrectState"
        
      - criterion: "Calculates fasting duration correctly"
        verification: "Unit test: GetFastingMetrics_CompletedSession_CalculatesDuration"
        
      - criterion: "Enforces eating window from preferences"
        verification: "Unit test: StartSession_OutsideEatingWindow_Succeeds"
        
      - criterion: "FMD protocol tracks 5-day cycle"
        verification: "Unit test: StartFmd_TracksDay1Through5"
        
      - criterion: "Service registered in DI"
        verification: "Integration test: ServiceProvider_CanResolve_IFastingService"
        
  metadata:
    estimated_complexity: medium
    skills_required:
      - C# / .NET
      - Entity Framework Core
      - Dependency Injection
      - Unit Testing
    created_at: 2025-01-20T10:00:00Z
    status: READY
```

---

## SPPV Schema Reference

### JSON Schema (Abbreviated)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "SPPV Task",
  "type": "object",
  "required": ["id", "name", "type", "specification", "outputs", "verification"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[ET]\\d+(\\.\\d+)*$"
    },
    "name": {
      "type": "string",
      "maxLength": 100
    },
    "type": {
      "enum": ["epic", "feature", "task"]
    },
    "description": { "type": "string" },
    "rationale": { "type": "string" },
    "references": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "type": { "enum": ["requirement", "research", "design", "external"] },
          "id": { "type": "string" },
          "url": { "type": "string", "format": "uri" }
        }
      }
    },
    "dependencies": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["id"],
        "properties": {
          "id": { "type": "string" },
          "artifacts": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "path": { "type": "string" }
              }
            }
          }
        }
      }
    },
    "inputs": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "type", "source"],
        "properties": {
          "name": { "type": "string" },
          "type": { "enum": ["file", "artifact", "config", "context"] },
          "source": { "type": "string" },
          "required": { "type": "boolean", "default": true }
        }
      }
    },
    "specification": {
      "type": "object",
      "required": ["objective"],
      "properties": {
        "objective": { "type": "string" },
        "constraints": { "type": "array", "items": { "type": "string" } },
        "guidance": { "type": "array", "items": { "type": "string" } },
        "anti_patterns": { "type": "array", "items": { "type": "string" } }
      }
    },
    "outputs": {
      "type": "array",
      "minItems": 1,
      "items": {
        "type": "object",
        "required": ["name", "type", "path"],
        "properties": {
          "name": { "type": "string" },
          "type": { "enum": ["file", "artifact", "test", "documentation", "modification"] },
          "path": { "type": "string" },
          "description": { "type": "string" }
        }
      }
    },
    "verification": {
      "type": "object",
      "properties": {
        "preconditions": { "$ref": "#/definitions/checks" },
        "postconditions": { "$ref": "#/definitions/checks" },
        "acceptance_criteria": {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["criterion", "verification"],
            "properties": {
              "criterion": { "type": "string" },
              "verification": { "type": "string" }
            }
          }
        }
      }
    },
    "composition": {
      "type": "object",
      "properties": {
        "type": { "enum": ["series", "parallel"] },
        "group": { "type": "string" }
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "estimated_complexity": { "enum": ["low", "medium", "high"] },
        "skills_required": { "type": "array", "items": { "type": "string" } },
        "created_at": { "type": "string", "format": "date-time" },
        "status": { 
          "enum": ["pending", "ready", "in_progress", "verifying", "done", "blocked", "failed"]
        }
      }
    }
  },
  "definitions": {
    "checks": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["check"],
        "properties": {
          "check": { "type": "string" },
          "automated": { "type": "boolean", "default": false },
          "command": { "type": "string" }
        }
      }
    }
  }
}
```

---

## Best Practices

### 1. Task Decomposition

**DO:**
- Break tasks into atomic units completable in one agent session
- Ensure each task has clear, verifiable outputs
- Make dependencies explicit with specific artifacts

**DON'T:**
- Create tasks that require human judgment mid-execution
- Bundle unrelated functionality in one task
- Create implicit dependencies (always declare them)

### 2. Specification Writing

**DO:**
- Write objectives as if explaining to a new team member
- Include examples of expected output
- List anti-patterns to prevent common mistakes
- Reference existing code patterns

**DON'T:**
- Assume the agent knows project conventions
- Leave ambiguous requirements
- Skip constraints and edge cases

### 3. Verification Design

**DO:**
- Automate as many checks as possible
- Make acceptance criteria objectively measurable
- Include both positive and negative test cases

**DON'T:**
- Use vague criteria like "code looks good"
- Skip postcondition checks to save time
- Ignore failed verifications

### 4. Parallelization

**DO:**
- Identify independent work streams early
- Design artifacts to minimize coupling
- Create explicit join points

**DON'T:**
- Force parallelism where it doesn't fit naturally
- Create hidden dependencies through shared state
- Skip the join task consolidation step

### 5. Error Recovery

**DO:**
- Include error context in retry prompts
- Decompose tasks that fail repeatedly
- Set up monitoring for failure patterns

**DON'T:**
- Retry infinitely without analysis
- Ignore patterns of similar failures
- Modify task specs without versioning

### 6. Progress Tracking

**DO:**
- Update status immediately on transitions
- Track metrics over time for process improvement
- Visualize the critical path

**DON'T:**
- Let stale statuses accumulate
- Ignore velocity trends
- Forget to celebrate completions!

---

## Conclusion

SPPV provides a rigorous framework for decomposing software development work into machine-executable units while maintaining the flexibility for human oversight and intervention. By combining the mathematical foundations of series-parallel posets with explicit verification gates, SPPV enables:

1. **Parallel execution** where the dependency graph allows
2. **Quality assurance** through automated verification at every step
3. **Progress visibility** through well-defined status states
4. **Error recovery** through checkpointing and structured retries
5. **Human-AI collaboration** through clear contracts and explicit context

The methodology is particularly well-suited for generative AI agents because it addresses their core needs: unambiguous specifications, explicit dependencies, verifiable outputs, and complete context for each task.

---

## Appendix: Quick Reference Card

```
┌─────────────────────────────────────────────────────────────┐
│                    SPPV QUICK REFERENCE                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  COMPOSITION:                                               │
│    A → B     Series (A before B)                            │
│    A || B    Parallel (A and B concurrent)                  │
│    (A → B) || C    Compound (AB-chain parallel with C)      │
│                                                             │
│  STATUS FLOW:                                               │
│    PENDING → READY → IN_PROGRESS → VERIFYING → DONE         │
│                ↓           ↓            ↓                   │
│             BLOCKED      FAILED       FAILED                │
│                                                             │
│  VERIFICATION GATES:                                        │
│    1. Precondition  - Can we start?                         │
│    2. Postcondition - Did we produce outputs?               │
│    3. Acceptance    - Does it meet requirements?            │
│                                                             │
│  TASK STRUCTURE:                                            │
│    id, name, type                                           │
│    dependencies[]                                           │
│    inputs[]                                                 │
│    specification { objective, constraints, guidance }       │
│    outputs[]                                                │
│    verification { pre, post, acceptance }                   │
│                                                             │
│  HIERARCHY:                                                 │
│    Epic (weeks) → Feature (days) → Task (hours)             │
│                                                             │
│  GOLDEN RULES:                                              │
│    ✓ One task = One agent session                           │
│    ✓ All dependencies explicit                              │
│    ✓ All outputs verifiable                                 │
│    ✓ No implicit context                                    │
│    ✓ Automate verification                                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

*Document Version: 1.0.0*  
*Last Updated: January 2025*
