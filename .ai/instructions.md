# Variant 06: SPPV + Formal Concurrency (Petri Nets / Statecharts)

## Overview

This methodology models concurrent/interactive behavior using **formal specifications**: Petri nets for asynchronous operations and statecharts for UI state management. Properties like deadlock-freedom and liveness are verified before implementation. The formal models serve as both specification and verification target.

## Historical Context

- **Origins**: Petri nets invented by Carl Adam Petri in 1962. Statecharts developed by David Harel in 1987 at Weizmann Institute. Both address limitations of simple state machines
- **Notable Uses**: Petri nets in workflow systems (BPMN) and manufacturing. Statecharts in UML, automotive software (AUTOSAR), and avionics. XState library brought statecharts to web development
- **Why It Faded**: Visual complexity explodes for real-world systems—state explosion problem. Many teams found informal state management with clear naming conventions sufficient

## Core Principles

1. **Petri Nets for Async**: Model concurrent operations with places, transitions, and tokens
2. **Statecharts for UI**: Hierarchical state machines with parallel regions
3. **Verify Before Implement**: Check deadlock-freedom, liveness, boundedness
4. **Code-Model Correspondence**: Implementation directly reflects formal model
5. **Property-Driven Development**: Verify properties mathematically

## Petri Net Fundamentals

A Petri net consists of:
- **Places (circles)**: Represent conditions or states
- **Transitions (rectangles)**: Represent events or actions
- **Tokens (dots)**: Represent resources or control flow
- **Arcs**: Connect places to transitions and vice versa

### Example: Save Operation

```
     [Idle]                    [Dirty]
       (●)                       (●)
        |                         |
        v                         v
   +---------+              +-----------+
   |  Edit   |              | StartSave |
   +---------+              +-----------+
        |                         |
        v                         v
     [Dirty]                  [Saving]
       ( )                       ( )
                                  |
                    +-------------+-------------+
                    |                           |
                    v                           v
              +-----------+              +------------+
              | SaveOK    |              | SaveFailed |
              +-----------+              +------------+
                    |                           |
                    v                           v
                 [Idle]                     [Dirty]
                   ( )                        ( )
```

### Petri Net Properties to Verify

| Property | Definition | Why It Matters |
|----------|------------|----------------|
| **Deadlock-free** | No reachable state where no transition can fire | System never gets stuck |
| **Bounded** | Token count never exceeds limit | No resource exhaustion |
| **Live** | Every transition can eventually fire | No dead code |
| **Safe** | Each place has at most 1 token | Mutual exclusion |

## Statechart Fundamentals

Statecharts extend state machines with:
- **Hierarchy**: States contain substates
- **Parallelism**: Orthogonal regions execute concurrently
- **History**: Remember previous substate

### Example: Node Interaction Statechart

```
┌─────────────────────────────────────────────────┐
│                    NODE                          │
│  ┌─────────┐                                    │
│  │  Idle   │◄──────────────────────────────┐    │
│  └────┬────┘                               │    │
│       │ click                              │    │
│       v                                    │    │
│  ┌──────────┐    dblclick    ┌──────────┐ │    │
│  │ Selected │───────────────►│ Editing  │ │    │
│  └────┬─────┘                └────┬─────┘ │    │
│       │ mousedown                 │ blur  │    │
│       v                           │       │    │
│  ┌──────────┐                     │       │    │
│  │ Dragging │─────────────────────┼───────┘    │
│  └──────────┘    mouseup          │            │
│       │                           │            │
│       └───────────────────────────┘            │
└─────────────────────────────────────────────────┘
```

## Getting Started

### Step 1: Model Async Operations as Petri Nets

For each async operation, draw the Petri net:

```markdown
## SaveMindMap Petri Net

### Places
- P1: Idle (initial token)
- P2: Saving
- P3: SaveSuccess
- P4: SaveFailed
- P5: Dirty

### Transitions
- T1: StartSave (guard: P5 has token)
- T2: SaveComplete
- T3: SaveError
- T4: ReturnToIdle
- T5: MarkDirty
- T6: RetryAfterFail

### Arcs
- P1 + P5 → T1 → P2 (start save consumes Idle and Dirty)
- P2 → T2 → P3 (success)
- P2 → T3 → P4 (failure)
- P3 → T4 → P1 (return to idle)
- P4 → T6 → P5 (restore dirty state for retry)
- P1 → T5 → P1 + P5 (edit creates dirty token)
```

### Step 2: Verify Petri Net Properties

Check each property:

```markdown
## Property Verification: SaveMindMap

### Deadlock-free: ✓
- From Idle: Can fire T5 (MarkDirty)
- From Idle+Dirty: Can fire T1 (StartSave)
- From Saving: Can fire T2 or T3
- From SaveSuccess: Can fire T4
- From SaveFailed: Can fire T6
- No reachable state without enabled transitions

### Bounded: ✓
- P1 (Idle): max 1 token
- P2 (Saving): max 1 token
- P5 (Dirty): max 1 token
- Total tokens: max 2 at any time

### Live: ✓
- All transitions can fire from initial state
- T1 fires when dirty, T2/T3 fire during save, T4/T6 complete the cycle
```

### Step 3: Model UI State as Statecharts

```markdown
## CanvasStateMachine

### States
- Viewing (initial)
- Panning
- Zooming
- BoxSelecting
- Connecting

### Transitions
| From | Event | Guard | To |
|------|-------|-------|-----|
| Viewing | mousedown (empty space) | - | Panning |
| Viewing | wheel | - | Zooming |
| Viewing | mousedown + shift | - | BoxSelecting |
| Viewing | drag from node edge | - | Connecting |
| Panning | mouseup | - | Viewing |
| Zooming | timeout(100ms) | - | Viewing |
| BoxSelecting | mouseup | - | Viewing |
| Connecting | mouseup on node | - | Viewing |
| Connecting | mouseup on empty | - | Viewing |

### Properties
- Deterministic: No state has two transitions with same event and overlapping guards ✓
- Complete: All states handle all relevant events ✓
- Reachable: All states reachable from initial ✓
```

### Step 4: Implement State Machines in Code

```csharp
public enum SaveState { Idle, Saving, SaveSuccess, SaveFailed }

public class SaveStateMachine
{
    private SaveState _state = SaveState.Idle;
    private bool _isDirty = false;
    private readonly object _lock = new();

    // T5: MarkDirty
    public void MarkDirty()
    {
        lock (_lock)
        {
            _isDirty = true;
        }
    }

    // T1: StartSave (guard: isDirty)
    public bool TryStartSave()
    {
        lock (_lock)
        {
            if (_state != SaveState.Idle || !_isDirty)
                return false;

            _state = SaveState.Saving;
            _isDirty = false;
            return true;
        }
    }

    // T2: SaveComplete
    public void SaveComplete()
    {
        lock (_lock)
        {
            if (_state != SaveState.Saving)
                throw new InvalidOperationException();
            _state = SaveState.SaveSuccess;
        }
    }

    // ... more transitions
}
```

### Step 5: Link Code to Model

Add comments referencing the formal model:

```csharp
// T1: StartSave (P1 + P5 --> P2)
// Precondition: state == Idle AND isDirty == true
// Postcondition: state == Saving AND isDirty == false
public bool TryStartSave()
{
    // ...
}
```

## Required Artifacts

### PETRI-MODELS.md

Document all Petri net models:

```markdown
# Petri Net Models

## Summary
- Total Models: 11
- Properties Verified: Deadlock-free, Bounded, Live

## 1. SaveMindMap
[Places, Transitions, Arcs, Properties as above]

## 2. LoadMindMap
[...]

## 3. AutoSave
[...]
```

### STATECHARTS.md

Document all statecharts:

```markdown
# Statecharts

## Summary
- Total Statecharts: 9
- Properties Verified: Deterministic, Complete, Reachable

## 1. NodeStateMachine
[States, Transitions, Properties]

## 2. CanvasStateMachine
[...]

## 3. AppModeStateMachine
[...]
```

### /StateMachines Folder

Implement as code:

```
/StateMachines
├── Base/
│   └── AsyncOperationStateMachine.cs
├── Operations/
│   ├── SaveStateMachine.cs
│   ├── LoadStateMachine.cs
│   └── SyncStateMachine.cs
└── UI/
    ├── NodeStateMachine.cs
    ├── CanvasStateMachine.cs
    └── SelectionStateMachine.cs
```

## Workflow

```
1. MODEL ASYNC OPERATIONS
   └── Draw Petri net for each async flow
   └── Define places, transitions, arcs
   └── Identify guards and conditions

2. VERIFY PETRI NET PROPERTIES
   └── Check deadlock-freedom
   └── Check boundedness
   └── Check liveness
   └── Document verification

3. MODEL UI STATES
   └── Draw statechart for each interactive component
   └── Define states and transitions
   └── Identify hierarchy and parallelism

4. VERIFY STATECHART PROPERTIES
   └── Check determinism
   └── Check completeness
   └── Check reachability

5. IMPLEMENT STATE MACHINES
   └── Create state machine classes
   └── Link to formal model in comments
   └── Use thread-safe patterns for async

6. TEST AGAINST MODEL
   └── Verify implementation matches model
   └── Test all transitions
   └── Verify properties hold
```

## Tips from Our Experiment

1. **Thread safety is essential**: Async operations need locks or atomic operations
2. **Guards prevent invalid transitions**: Check preconditions before state changes
3. **Model-code correspondence aids debugging**: When bugs occur, check model first
4. **Start with simple models**: Petri nets can get complex quickly
5. **Statecharts handle UI well**: Hierarchy manages complexity

## Common Petri Net Patterns

### Mutex (Mutual Exclusion)
```
    [Resource]
       (●)
      /   \
     v     v
  [Use1] [Use2]
     \     /
      v   v
    [Return]
```

### Producer-Consumer
```
[Empty] ──► [Produce] ──► [Full] ──► [Consume] ──► [Empty]
```

### Retry with Backoff
```
[Failed] ──► [Wait] ──► [Retry] ──► [Success]
                │                      │
                └──────────────────────┘
```

## Metrics to Track

- **Model count**: Petri nets + statecharts defined
- **Property verification coverage**: % of properties checked
- **State machine implementation coverage**: % of models implemented
- **Code-model correspondence**: Links from code to model
- **Concurrency bugs found**: Issues caught by formal analysis

## When to Use Formal Concurrency

**Good fit:**
- Systems with complex async behavior
- Multi-user real-time applications
- Embedded systems with concurrent hardware
- Protocol implementations

**Less suitable:**
- Simple request-response APIs
- Sequential batch processing
- Projects without concurrency concerns

## Results from Our Experiment

- **Duration**: 2h 45m
- **Petri Net Models**: 11 defined
- **Statecharts**: 9 defined
- **All Properties Verified**: Deadlock-free, bounded, live
- **Quality Score**: 7.5/10
- **Feature Complete**: 93% (some spec-implementation gap)
- **Verdict**: Formal concurrency modeling provides the strongest theoretical foundation. The approach appears to have prevented race conditions and deadlocks entirely—none observed in testing. However, maintaining model-code correspondence is challenging, and some features were specified but not fully implemented. Best suited for systems where concurrency correctness is critical.
