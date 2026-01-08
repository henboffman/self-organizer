# Organizational Goals System - Implementation Plan

## Executive Summary

This document outlines a comprehensive plan for extending the Self-Organizer application to support organizational-level goal tracking. The system will allow organizations to define strategic goals and track progress by aggregating data from individual team members using the Self-Organizer tool, while maintaining the privacy and autonomy central to the "self-organizer" philosophy.

---

## 1. Architecture Options

### Option A: Extension of Current App (Recommended)

**Description**: Add organizational features as an optional module within the existing Self-Organizer application, with a separate "Organization Hub" web service for aggregation.

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Self-Organizer (Existing)                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Individual Mode (Default)                                    │  │
│  │  - Personal goals, tasks, calendar                            │  │
│  │  - 100% local IndexedDB storage                               │  │
│  │  - No network required                                        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                              │                                      │
│                    [Optional Org Link]                              │
│                              │                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Organization Module (Opt-in)                                 │  │
│  │  - Link personal goals to org goals                           │  │
│  │  - Selective progress sharing                                 │  │
│  │  - Sync service to Organization Hub                           │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                    [Encrypted Sync]
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      Organization Hub (New)                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────────────┐  │
│  │  Org Mgmt API   │  │  Goal Hierarchy │  │  Aggregation       │  │
│  │  - Teams        │  │  - Org Goals    │  │  - Progress calc   │  │
│  │  - Members      │  │  - Team Goals   │  │  - Roll-up logic   │  │
│  │  - Roles        │  │  - Key Results  │  │  - Dashboards      │  │
│  └─────────────────┘  └─────────────────┘  └────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

**Pros**:
- Preserves the "self-organizer" philosophy
- Users can use the app without any organization features
- Single codebase to maintain
- Gradual adoption path for organizations
- Individual data remains private by default

**Cons**:
- Adds complexity to the existing app
- Requires careful separation of concerns
- Organization Hub requires server infrastructure

### Option B: Separate Companion App

**Description**: Build a completely separate "Team Organizer" or "Org Goals" application that consumes data from Self-Organizer instances.

```
┌────────────────────────┐     ┌────────────────────────┐
│  Self-Organizer #1     │     │  Self-Organizer #2     │
│  (Alice)               │     │  (Bob)                 │
│  ┌──────────────────┐  │     │  ┌──────────────────┐  │
│  │ Export API       │──┼──┐  │  │ Export API       │──┼──┐
│  │ - Goals progress │  │  │  │  │ - Goals progress │  │  │
│  │ - Task metrics   │  │  │  │  │ - Task metrics   │  │  │
│  └──────────────────┘  │  │  │  └──────────────────┘  │  │
└────────────────────────┘  │  └────────────────────────┘  │
                            │                              │
                            ▼                              ▼
                 ┌─────────────────────────────────────────┐
                 │          Organization Hub               │
                 │  - Import progress data                 │
                 │  - Define org goals                     │
                 │  - Map user goals to org goals          │
                 │  - Dashboard & reporting                │
                 └─────────────────────────────────────────┘
```

**Pros**:
- Complete separation of concerns
- Self-Organizer stays focused on personal productivity
- Easier to sell/deploy to organizations separately
- Independent release cycles

**Cons**:
- Two applications to maintain
- Users need to install and configure two tools
- More complex data synchronization
- Potential for feature drift between apps

### Option C: Plugin/Extension Architecture

**Description**: Design Self-Organizer with a plugin system where organizational features are loaded as an extension.

**Pros**:
- Maximum flexibility
- Community-contributed extensions possible
- Core app stays lightweight

**Cons**:
- Significant architectural investment
- Plugin API design is complex
- Testing becomes more challenging
- May be over-engineered for current needs

### Recommendation

**Option A (Extension with Organization Hub)** is recommended because:
1. Maintains the philosophy of self-organization while enabling opt-in collaboration
2. Single install for end users
3. Clear boundary between personal and shared data
4. Organization Hub can be self-hosted or cloud-hosted based on org needs

---

## 2. Data Model Changes

### 2.1 New Entities for Organization Module

#### Organization

```csharp
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    // Fiscal/planning periods
    public int FiscalYearStartMonth { get; set; } = 1; // January
    public string? Mission { get; set; }
    public string? Vision { get; set; }

    // Settings
    public bool AllowMemberGoalCreation { get; set; } = true;
    public bool RequireGoalApproval { get; set; } = false;
    public GoalScoringMethod ScoringMethod { get; set; } = GoalScoringMethod.Percentage;

    // Metadata
    public string? ExternalId { get; set; } // For SSO/directory integration
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
}

public enum OrganizationStatus { Active, Inactive, Archived }
public enum GoalScoringMethod { Percentage, OKRStyle, Binary, Custom }
```

#### Team

```csharp
public class Team : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentTeamId { get; set; } // For nested team hierarchies
    public TeamType Type { get; set; } = TeamType.Department;
    public List<Guid> MemberIds { get; set; } = new();
    public Guid? TeamLeadUserId { get; set; }
    public TeamStatus Status { get; set; } = TeamStatus.Active;
}

public enum TeamType { Department, Squad, Chapter, WorkingGroup, ProjectTeam, Custom }
public enum TeamStatus { Active, Inactive, Archived }
```

#### OrganizationMember

```csharp
public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; } // Links to local user or external identity
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public List<Guid> TeamIds { get; set; } = new();
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateTime? JoinedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }

    // Privacy settings - what this member shares
    public PrivacySettings SharingPreferences { get; set; } = new();
}

public enum OrganizationRole
{
    Owner,      // Full admin
    Admin,      // Manage goals, teams, members
    Manager,    // Manage team goals, view team progress
    Member,     // Contribute to goals, view own progress
    Viewer      // Read-only access to dashboards
}

public enum MemberStatus { Pending, Active, Inactive, Removed }
```

#### PrivacySettings

```csharp
public class PrivacySettings
{
    public bool ShareGoalProgress { get; set; } = true;
    public bool ShareTaskCompletionCounts { get; set; } = true;
    public bool ShareTimeAllocation { get; set; } = false;
    public bool ShareDetailedTasks { get; set; } = false; // Task titles
    public bool ShareCategories { get; set; } = true;
    public List<Guid> GoalIdsToShare { get; set; } = new(); // Whitelist
    public List<Guid> GoalIdsToHide { get; set; } = new();  // Blacklist
}
```

#### OrganizationGoal

```csharp
public class OrganizationGoal : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? TeamId { get; set; } // null = org-wide goal
    public Guid? ParentGoalId { get; set; } // For goal hierarchy

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SuccessCriteria { get; set; }

    // OKR-style structure
    public GoalType GoalType { get; set; } = GoalType.Objective;
    public List<KeyResult> KeyResults { get; set; } = new();

    // Timing
    public GoalPeriod Period { get; set; } = GoalPeriod.Quarter;
    public int PeriodNumber { get; set; } // Q1, Q2, etc. or Year
    public int PeriodYear { get; set; }
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }

    // Progress
    public decimal TargetValue { get; set; } = 100;
    public decimal CurrentValue { get; set; } = 0;
    public decimal ProgressPercent => TargetValue > 0 ? (CurrentValue / TargetValue) * 100 : 0;
    public ProgressCalculationMethod CalculationMethod { get; set; } = ProgressCalculationMethod.Average;

    // Ownership
    public Guid? OwnerUserId { get; set; }
    public List<Guid> ContributorUserIds { get; set; } = new();

    // Status
    public OrganizationGoalStatus Status { get; set; } = OrganizationGoalStatus.Draft;
    public int Priority { get; set; } = 2;
    public List<string> Tags { get; set; } = new();

    // Linkage to individual goals
    public List<GoalLink> LinkedIndividualGoals { get; set; } = new();
}

public class KeyResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string? Unit { get; set; } // "users", "%", "$", etc.
    public KeyResultType Type { get; set; } = KeyResultType.Percentage;
}

public class GoalLink
{
    public Guid IndividualGoalId { get; set; }
    public Guid UserId { get; set; }
    public decimal ContributionWeight { get; set; } = 1.0m; // For weighted roll-ups
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}

public enum GoalType { Objective, KeyResult, Initiative, Milestone }
public enum GoalPeriod { Week, Month, Quarter, Half, Year, Custom }
public enum ProgressCalculationMethod { Average, WeightedAverage, Minimum, Maximum, Sum }
public enum OrganizationGoalStatus { Draft, Active, OnTrack, AtRisk, Behind, Completed, Cancelled }
public enum KeyResultType { Percentage, Number, Currency, Binary }
```

### 2.2 Extensions to Existing Models

#### Goal.cs (Personal Goal - Add org linking)

```csharp
// Add to existing Goal class
public class Goal : BaseEntity
{
    // ... existing properties ...

    // Organization linking (opt-in)
    public Guid? LinkedOrganizationGoalId { get; set; }
    public Guid? ContributingToTeamId { get; set; }
    public bool IsSharedWithOrganization { get; set; } = false;
    public DateTime? LastSyncedToOrg { get; set; }
}
```

#### UserPreferences.cs (Add org settings)

```csharp
// Add to existing UserPreferences class
public class UserPreferences : BaseEntity
{
    // ... existing properties ...

    // Organization Settings
    public Guid? LinkedOrganizationId { get; set; }
    public string? OrganizationHubUrl { get; set; }
    public string? OrganizationAuthToken { get; set; } // Encrypted
    public bool AutoSyncWithOrganization { get; set; } = false;
    public int OrgSyncIntervalMinutes { get; set; } = 60;
    public PrivacySettings OrganizationPrivacySettings { get; set; } = new();
}
```

### 2.3 Aggregated Progress Models

#### GoalProgressSnapshot

```csharp
public class GoalProgressSnapshot : BaseEntity
{
    public Guid OrganizationGoalId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public decimal ProgressPercent { get; set; }
    public int ContributorCount { get; set; }
    public int LinkedGoalCount { get; set; }
    public Dictionary<Guid, decimal> ContributorProgress { get; set; } = new(); // UserId -> Progress
    public Dictionary<string, decimal> BreakdownByTeam { get; set; } = new();
    public string? Notes { get; set; }
}
```

---

## 3. Goal Roll-Up Architecture

### 3.1 Roll-Up Hierarchy

```
Organization Goal (Company-wide OKR)
    │
    ├── Team Goal (Department OKR)
    │       │
    │       ├── Individual Goal (Personal, linked) ──► Progress %
    │       ├── Individual Goal (Personal, linked) ──► Progress %
    │       └── Individual Goal (Personal, linked) ──► Progress %
    │
    └── Team Goal (Another Department)
            │
            └── ... more individual goals
```

### 3.2 Progress Calculation Methods

#### Method 1: Simple Average

```
OrgGoalProgress = Average(LinkedIndividualGoals.Progress)
```

Best for: Goals where all contributors have equal impact

#### Method 2: Weighted Average

```
OrgGoalProgress = Sum(IndividualProgress * ContributionWeight) / Sum(ContributionWeight)
```

Best for: Goals where some contributors have larger scope or impact

#### Method 3: Key Result Aggregation

```
For each KeyResult:
    KR_Progress = Aggregate(related individual metrics)

OrgGoalProgress = Average(KeyResults.Progress)
```

Best for: OKR-style objectives with measurable key results

#### Method 4: Minimum Progress (Weakest Link)

```
OrgGoalProgress = Min(LinkedIndividualGoals.Progress)
```

Best for: Goals that require ALL contributors to complete their part

#### Method 5: Task-Based Roll-Up

```
OrgGoalProgress = (CompletedLinkedTasks / TotalLinkedTasks) * 100
```

Best for: Goals where individual tasks directly contribute to org goals

### 3.3 Roll-Up Service Design

```csharp
public interface IGoalRollUpService
{
    Task<decimal> CalculateOrganizationGoalProgress(Guid orgGoalId);
    Task<decimal> CalculateTeamGoalProgress(Guid teamGoalId);
    Task<GoalProgressBreakdown> GetProgressBreakdown(Guid orgGoalId);
    Task<List<GoalProgressSnapshot>> GetProgressHistory(Guid orgGoalId, DateTime from, DateTime to);
    Task RecalculateAllOrgGoals(Guid organizationId);
}

public class GoalProgressBreakdown
{
    public decimal OverallProgress { get; set; }
    public List<ContributorProgress> Contributors { get; set; } = new();
    public List<TeamProgress> Teams { get; set; } = new();
    public List<KeyResultProgress> KeyResults { get; set; } = new();
    public TrendAnalysis Trend { get; set; } = new();
}

public class ContributorProgress
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal Progress { get; set; }
    public decimal ContributionWeight { get; set; }
    public DateTime LastUpdated { get; set; }
    public int LinkedGoalCount { get; set; }
    public int CompletedTaskCount { get; set; }
}

public class TrendAnalysis
{
    public decimal ProgressChange7Days { get; set; }
    public decimal ProgressChange30Days { get; set; }
    public decimal ProjectedCompletionPercent { get; set; }
    public DateTime? ProjectedCompletionDate { get; set; }
    public TrendDirection Direction { get; set; }
    public RiskLevel Risk { get; set; }
}

public enum TrendDirection { Accelerating, OnTrack, Slowing, Stalled, Declining }
public enum RiskLevel { Low, Medium, High, Critical }
```

---

## 4. Privacy and Permission Considerations

### 4.1 Core Privacy Principles

1. **Opt-In by Default**: No data is shared until user explicitly joins an organization and configures sharing
2. **Granular Control**: Users control exactly what they share (goals, progress, tasks, time allocation)
3. **Selective Linking**: Users choose which personal goals link to org goals
4. **Aggregated by Default**: Managers see rollup numbers, not individual task details
5. **Transparency**: Users can see exactly what data is being shared
6. **Revocability**: Users can unlink or stop sharing at any time

### 4.2 Permission Matrix

| Action | Owner | Admin | Manager | Member | Viewer |
|--------|-------|-------|---------|--------|--------|
| Create/Edit Org Goals | Yes | Yes | Team Only | No | No |
| View All Org Goals | Yes | Yes | Yes | Yes | Yes |
| View Goal Progress | Yes | Yes | Team + Own | Own | Dashboard |
| View Individual Contributions | Yes | Yes | Team Only | Own Only | No |
| View Task Details | Yes | If Shared | If Shared | Own Only | No |
| Manage Teams | Yes | Yes | Own Team | No | No |
| Invite Members | Yes | Yes | Own Team | No | No |
| Configure Org Settings | Yes | Yes | No | No | No |
| Export Data | Yes | Yes | Team Only | Own Only | No |
| Delete Organization | Yes | No | No | No | No |

### 4.3 Data Visibility Layers

```
Layer 1: Dashboard (Everyone)
├── Overall org goal progress %
├── Number of contributors
├── Trend indicators (on track, at risk, etc.)
└── Key results summary

Layer 2: Team View (Managers)
├── Team members list
├── Individual progress % per goal
├── Task completion counts
└── Time allocation by category (if shared)

Layer 3: Detail View (Admins + If Shared)
├── Individual task titles
├── Estimated vs actual time
├── Detailed notes
└── Full activity history
```

### 4.4 Privacy Configuration UI

```
┌─────────────────────────────────────────────────────────────┐
│ Organization Sharing Settings                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ What I share with my organization:                          │
│                                                             │
│ ☑ Goal progress percentages                                 │
│ ☑ Task completion counts (not titles)                       │
│ ☐ Task titles and descriptions                              │
│ ☐ Time allocation breakdown                                 │
│ ☑ Category distribution                                     │
│                                                             │
│ ─────────────────────────────────────────────────────────── │
│                                                             │
│ Goals I'm sharing:                                          │
│                                                             │
│ ☑ Q1 2024: Complete certification program                   │
│   └── Linked to: [Org Goal: Team Skill Development]         │
│                                                             │
│ ☑ Q1 2024: Reduce deployment time by 50%                    │
│   └── Linked to: [Org Goal: Engineering Excellence]         │
│                                                             │
│ ☐ Personal: Learn Spanish (Not shared)                      │
│                                                             │
│ [+ Link another goal to organization]                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.5 Audit Trail

All data sharing should be logged:

```csharp
public class SharingAuditLog : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? OrganizationId { get; set; }
    public SharingAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty; // Goal, Task, etc.
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public enum SharingAction
{
    LinkedGoal, UnlinkedGoal,
    EnabledSharing, DisabledSharing,
    SyncedProgress,
    ExportedData, ViewedByManager
}
```

---

## 5. Technical Approach for Multi-User Data Aggregation

### 5.1 Synchronization Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    Self-Organizer Client                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                     Sync Service                            │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │  │
│  │  │ Change       │  │ Conflict     │  │ Encryption       │  │  │
│  │  │ Detection    │  │ Resolution   │  │ Service          │  │  │
│  │  └──────────────┘  └──────────────┘  └──────────────────┘  │  │
│  │                          │                                  │  │
│  │           ┌──────────────┴──────────────┐                  │  │
│  │           │     Outbound Queue          │                  │  │
│  │           │  (Progress updates only)    │                  │  │
│  │           └──────────────┬──────────────┘                  │  │
│  └────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬────────────────────────────────────┘
                              │ HTTPS/WSS
                              │ JWT Auth
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Organization Hub API                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ Auth Service    │  │ Sync Endpoint   │  │ Aggregation     │  │
│  │ (OAuth2/OIDC)   │  │ (REST/WebSocket)│  │ Service         │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┴─────────────────────────────────┐ │
│  │                    PostgreSQL/SQLite                        │ │
│  │  Organizations │ Teams │ Members │ OrgGoals │ Snapshots     │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### 5.2 Sync Protocol

#### Phase 1: Progress-Only Sync (Recommended Start)

Only sync aggregated progress data, not individual items:

```csharp
public class ProgressSyncPayload
{
    public Guid UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<GoalProgressUpdate> GoalUpdates { get; set; } = new();
    public SummaryMetrics Summary { get; set; } = new();
    public string Signature { get; set; } = string.Empty; // HMAC for integrity
}

public class GoalProgressUpdate
{
    public Guid PersonalGoalId { get; set; }
    public Guid? LinkedOrgGoalId { get; set; }
    public decimal ProgressPercent { get; set; }
    public int CompletedTaskCount { get; set; }
    public int TotalTaskCount { get; set; }
    public DateTime LastActivity { get; set; }
}

public class SummaryMetrics
{
    public int TotalTasksCompleted { get; set; }
    public int TotalGoalsActive { get; set; }
    public Dictionary<string, int> TasksByCategory { get; set; } = new();
    public int FocusMinutesThisPeriod { get; set; }
}
```

#### Phase 2: Event-Based Updates

Real-time updates via WebSocket for live dashboards:

```csharp
public class ProgressEvent
{
    public string EventType { get; set; } = string.Empty; // "goal_progress", "task_completed"
    public Guid UserId { get; set; }
    public Guid? OrgGoalId { get; set; }
    public decimal NewProgress { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 5.3 Conflict Resolution

Since personal data is source of truth, conflicts are minimal:

1. **Progress Updates**: Last-write-wins with timestamp
2. **Goal Links**: User's client is authoritative
3. **Org Goal Changes**: Hub is authoritative, client pulls updates

### 5.4 Offline Support

```csharp
public class SyncQueue
{
    public List<QueuedSyncItem> PendingItems { get; set; } = new();
    public DateTime LastSuccessfulSync { get; set; }
    public int RetryCount { get; set; }
}

public class QueuedSyncItem
{
    public Guid Id { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public SyncItemStatus Status { get; set; }
}
```

### 5.5 Hub Database Schema

```sql
-- Organizations
CREATE TABLE organizations (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    fiscal_year_start_month INT DEFAULT 1,
    scoring_method VARCHAR(50) DEFAULT 'Percentage',
    status VARCHAR(50) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Teams
CREATE TABLE teams (
    id UUID PRIMARY KEY,
    organization_id UUID REFERENCES organizations(id),
    name VARCHAR(255) NOT NULL,
    parent_team_id UUID REFERENCES teams(id),
    team_lead_user_id UUID,
    status VARCHAR(50) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Members
CREATE TABLE organization_members (
    id UUID PRIMARY KEY,
    organization_id UUID REFERENCES organizations(id),
    external_user_id VARCHAR(255), -- From SSO/identity provider
    email VARCHAR(255) NOT NULL,
    display_name VARCHAR(255),
    role VARCHAR(50) DEFAULT 'Member',
    status VARCHAR(50) DEFAULT 'Active',
    sharing_preferences JSONB DEFAULT '{}',
    last_sync_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Team Memberships
CREATE TABLE team_members (
    team_id UUID REFERENCES teams(id),
    member_id UUID REFERENCES organization_members(id),
    PRIMARY KEY (team_id, member_id)
);

-- Organization Goals
CREATE TABLE organization_goals (
    id UUID PRIMARY KEY,
    organization_id UUID REFERENCES organizations(id),
    team_id UUID REFERENCES teams(id),
    parent_goal_id UUID REFERENCES organization_goals(id),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    goal_type VARCHAR(50) DEFAULT 'Objective',
    period VARCHAR(50) DEFAULT 'Quarter',
    period_number INT,
    period_year INT,
    target_value DECIMAL(10,2) DEFAULT 100,
    current_value DECIMAL(10,2) DEFAULT 0,
    calculation_method VARCHAR(50) DEFAULT 'Average',
    owner_user_id UUID,
    status VARCHAR(50) DEFAULT 'Active',
    priority INT DEFAULT 2,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Key Results
CREATE TABLE key_results (
    id UUID PRIMARY KEY,
    goal_id UUID REFERENCES organization_goals(id),
    title VARCHAR(500) NOT NULL,
    target_value DECIMAL(10,2),
    current_value DECIMAL(10,2) DEFAULT 0,
    unit VARCHAR(50),
    kr_type VARCHAR(50) DEFAULT 'Percentage'
);

-- Individual Goal Links
CREATE TABLE goal_links (
    id UUID PRIMARY KEY,
    organization_goal_id UUID REFERENCES organization_goals(id),
    member_id UUID REFERENCES organization_members(id),
    individual_goal_id UUID NOT NULL, -- From user's local app
    contribution_weight DECIMAL(5,2) DEFAULT 1.0,
    current_progress DECIMAL(5,2) DEFAULT 0,
    last_updated TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Progress Snapshots (for historical tracking)
CREATE TABLE goal_progress_snapshots (
    id UUID PRIMARY KEY,
    organization_goal_id UUID REFERENCES organization_goals(id),
    snapshot_date DATE NOT NULL,
    progress_percent DECIMAL(5,2),
    contributor_count INT,
    breakdown_json JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Audit Log
CREATE TABLE sharing_audit_log (
    id UUID PRIMARY KEY,
    member_id UUID REFERENCES organization_members(id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100),
    entity_id UUID,
    details JSONB,
    ip_address VARCHAR(45),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes
CREATE INDEX idx_goals_org ON organization_goals(organization_id);
CREATE INDEX idx_goals_team ON organization_goals(team_id);
CREATE INDEX idx_goals_period ON organization_goals(period_year, period_number);
CREATE INDEX idx_members_org ON organization_members(organization_id);
CREATE INDEX idx_snapshots_goal ON goal_progress_snapshots(organization_goal_id, snapshot_date);
```

---

## 6. API Design

### 6.1 Organization Hub REST API

#### Authentication

```
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

#### Organizations

```
GET    /api/organizations                    # List user's organizations
POST   /api/organizations                    # Create organization
GET    /api/organizations/{id}               # Get organization details
PUT    /api/organizations/{id}               # Update organization
DELETE /api/organizations/{id}               # Delete organization (owner only)
GET    /api/organizations/{id}/dashboard     # Dashboard summary
```

#### Teams

```
GET    /api/organizations/{orgId}/teams                 # List teams
POST   /api/organizations/{orgId}/teams                 # Create team
GET    /api/organizations/{orgId}/teams/{teamId}        # Get team
PUT    /api/organizations/{orgId}/teams/{teamId}        # Update team
DELETE /api/organizations/{orgId}/teams/{teamId}        # Delete team
GET    /api/organizations/{orgId}/teams/{teamId}/members
POST   /api/organizations/{orgId}/teams/{teamId}/members
DELETE /api/organizations/{orgId}/teams/{teamId}/members/{memberId}
```

#### Members

```
GET    /api/organizations/{orgId}/members              # List members
POST   /api/organizations/{orgId}/members/invite       # Invite member
GET    /api/organizations/{orgId}/members/{id}         # Get member
PUT    /api/organizations/{orgId}/members/{id}         # Update member role
DELETE /api/organizations/{orgId}/members/{id}         # Remove member
```

#### Organization Goals

```
GET    /api/organizations/{orgId}/goals                # List goals with filters
POST   /api/organizations/{orgId}/goals                # Create goal
GET    /api/organizations/{orgId}/goals/{id}           # Get goal with progress
PUT    /api/organizations/{orgId}/goals/{id}           # Update goal
DELETE /api/organizations/{orgId}/goals/{id}           # Delete goal
GET    /api/organizations/{orgId}/goals/{id}/progress  # Detailed progress breakdown
GET    /api/organizations/{orgId}/goals/{id}/history   # Progress history
GET    /api/organizations/{orgId}/goals/{id}/contributors # List contributors
```

#### Sync Endpoints (for Self-Organizer clients)

```
POST   /api/sync/progress                    # Push progress update
GET    /api/sync/goals                       # Pull org goals to link to
POST   /api/sync/link                        # Link personal goal to org goal
DELETE /api/sync/link/{linkId}               # Unlink goal
GET    /api/sync/status                      # Get sync status
```

#### WebSocket Endpoint

```
WSS    /ws/progress                          # Real-time progress updates
```

### 6.2 API Request/Response Examples

#### Create Organization Goal

```http
POST /api/organizations/550e8400-e29b-41d4-a716-446655440000/goals
Content-Type: application/json
Authorization: Bearer <token>

{
  "title": "Increase customer satisfaction score to 4.5",
  "description": "Improve our NPS and CSAT scores through better support and product quality",
  "goalType": "Objective",
  "period": "Quarter",
  "periodNumber": 1,
  "periodYear": 2024,
  "teamId": null,
  "targetValue": 4.5,
  "calculationMethod": "Average",
  "priority": 1,
  "keyResults": [
    {
      "title": "Reduce average response time to under 4 hours",
      "targetValue": 4,
      "unit": "hours",
      "type": "Number"
    },
    {
      "title": "Achieve 95% first-contact resolution rate",
      "targetValue": 95,
      "unit": "%",
      "type": "Percentage"
    }
  ]
}
```

#### Sync Progress Update

```http
POST /api/sync/progress
Content-Type: application/json
Authorization: Bearer <token>

{
  "userId": "user-uuid",
  "timestamp": "2024-01-15T10:30:00Z",
  "goalUpdates": [
    {
      "personalGoalId": "personal-goal-uuid",
      "linkedOrgGoalId": "org-goal-uuid",
      "progressPercent": 45.5,
      "completedTaskCount": 12,
      "totalTaskCount": 25,
      "lastActivity": "2024-01-15T09:15:00Z"
    }
  ],
  "summary": {
    "totalTasksCompleted": 47,
    "totalGoalsActive": 3,
    "tasksByCategory": {
      "Development": 20,
      "Documentation": 15,
      "Meetings": 12
    },
    "focusMinutesThisPeriod": 1840
  },
  "signature": "hmac-sha256-signature"
}
```

#### Get Goal Progress Breakdown

```http
GET /api/organizations/{orgId}/goals/{goalId}/progress

Response:
{
  "overallProgress": 62.5,
  "status": "OnTrack",
  "lastUpdated": "2024-01-15T10:30:00Z",
  "contributors": [
    {
      "userId": "user-1-uuid",
      "displayName": "Alice Smith",
      "progress": 75.0,
      "contributionWeight": 1.0,
      "lastUpdated": "2024-01-15T10:30:00Z",
      "linkedGoalCount": 2,
      "completedTaskCount": 15
    },
    {
      "userId": "user-2-uuid",
      "displayName": "Bob Jones",
      "progress": 50.0,
      "contributionWeight": 1.0,
      "lastUpdated": "2024-01-14T16:45:00Z",
      "linkedGoalCount": 1,
      "completedTaskCount": 8
    }
  ],
  "teams": [
    {
      "teamId": "team-uuid",
      "teamName": "Engineering",
      "progress": 62.5,
      "contributorCount": 2
    }
  ],
  "keyResults": [
    {
      "id": "kr-1-uuid",
      "title": "Reduce response time",
      "targetValue": 4,
      "currentValue": 5.2,
      "unit": "hours",
      "progress": 70.0
    }
  ],
  "trend": {
    "progressChange7Days": 8.5,
    "progressChange30Days": 25.0,
    "projectedCompletionPercent": 98.0,
    "projectedCompletionDate": "2024-03-28",
    "direction": "OnTrack",
    "risk": "Low"
  }
}
```

---

## 7. Implementation Phases

### Phase 1: Foundation (4-6 weeks)

**Goal**: Establish the basic infrastructure for organizational features

#### 1.1 Data Model Implementation
- [ ] Add new entity models (Organization, Team, OrganizationMember, OrganizationGoal)
- [ ] Extend Goal model with org linking fields
- [ ] Extend UserPreferences with org settings
- [ ] Create migration scripts for IndexedDB schema

#### 1.2 Organization Hub Setup
- [ ] Create new ASP.NET Core Web API project
- [ ] Set up PostgreSQL database with schema
- [ ] Implement basic CRUD operations for all entities
- [ ] Set up JWT authentication

#### 1.3 Basic Organization Management UI
- [ ] Organization settings page in Self-Organizer
- [ ] "Link to Organization" modal
- [ ] Organization connection status indicator

**Deliverables**:
- Data models finalized and implemented
- Hub API running with basic endpoints
- Users can connect to an organization

---

### Phase 2: Goal Linking & Manual Progress (3-4 weeks)

**Goal**: Enable users to link personal goals to org goals and manually update progress

#### 2.1 Goal Linking
- [ ] UI to browse available org goals
- [ ] Link personal goal to org goal
- [ ] Unlink functionality
- [ ] Visual indicator of linked goals

#### 2.2 Progress Sync (Manual)
- [ ] "Sync Now" button
- [ ] Progress update API implementation
- [ ] Conflict handling
- [ ] Sync status display

#### 2.3 Basic Dashboard
- [ ] Organization goals list view
- [ ] Simple progress bars
- [ ] Contributor count display

**Deliverables**:
- Users can link personal goals to org goals
- Progress syncs on demand
- Basic org dashboard available

---

### Phase 3: Automated Sync & Privacy Controls (3-4 weeks)

**Goal**: Automatic background sync with full privacy controls

#### 3.1 Background Sync Service
- [ ] Implement sync queue
- [ ] Background sync worker
- [ ] Retry logic with exponential backoff
- [ ] Offline queue management

#### 3.2 Privacy Settings UI
- [ ] Granular sharing preferences
- [ ] Per-goal sharing toggles
- [ ] "What am I sharing?" summary view
- [ ] Audit log viewer

#### 3.3 Role-Based Access
- [ ] Implement permission checks on Hub API
- [ ] Role management UI for admins
- [ ] Team assignment UI

**Deliverables**:
- Automatic background sync working
- Full privacy controls in place
- Role-based access implemented

---

### Phase 4: Roll-Up Engine & Dashboards (4-5 weeks)

**Goal**: Sophisticated progress aggregation and visualization

#### 4.1 Roll-Up Service
- [ ] Implement all calculation methods
- [ ] Weighted averages
- [ ] Key result aggregation
- [ ] Historical snapshot generation

#### 4.2 Advanced Dashboards
- [ ] Organization-wide dashboard
- [ ] Team dashboards
- [ ] Progress charts and trends
- [ ] At-risk goal alerts

#### 4.3 Reporting
- [ ] Export progress reports (PDF/CSV)
- [ ] Scheduled report emails
- [ ] Custom date range selection

**Deliverables**:
- Full roll-up calculations working
- Rich dashboards with visualizations
- Export/reporting functionality

---

### Phase 5: Real-Time & Notifications (2-3 weeks)

**Goal**: Live updates and proactive notifications

#### 5.1 WebSocket Integration
- [ ] Real-time progress updates
- [ ] Live dashboard updates
- [ ] Connection management

#### 5.2 Notifications
- [ ] Goal milestone notifications
- [ ] At-risk alerts
- [ ] Weekly digest emails
- [ ] In-app notification center

**Deliverables**:
- Real-time dashboard updates
- Notification system operational

---

### Phase 6: Advanced Features & Polish (3-4 weeks)

**Goal**: Enterprise features and production readiness

#### 6.1 SSO Integration
- [ ] OAuth2/OIDC integration
- [ ] SAML support
- [ ] Directory sync (Azure AD, Okta)

#### 6.2 Enterprise Features
- [ ] Multi-organization support
- [ ] Custom goal periods
- [ ] API rate limiting
- [ ] Audit compliance reports

#### 6.3 Polish
- [ ] Performance optimization
- [ ] Error handling improvements
- [ ] Documentation
- [ ] Admin guide

**Deliverables**:
- Production-ready system
- Enterprise authentication
- Complete documentation

---

## 8. Technical Considerations

### 8.1 Security

- All API communication over HTTPS
- JWT tokens with short expiry (15 min) and refresh tokens
- HMAC signing for sync payloads
- Encryption at rest for sensitive data
- Rate limiting on all endpoints
- Input validation and sanitization

### 8.2 Scalability

- Horizontal scaling of Hub API
- Read replicas for dashboard queries
- Caching layer (Redis) for frequently accessed data
- Async processing for heavy calculations

### 8.3 Reliability

- Circuit breaker pattern for external calls
- Graceful degradation when Hub is unavailable
- Idempotent sync operations
- Comprehensive logging and monitoring

### 8.4 Testing Strategy

- Unit tests for roll-up calculations
- Integration tests for sync protocol
- Load testing for Hub API
- E2E tests for critical flows

---

## 9. Open Questions

1. **Hosting Model**: Should the Hub be cloud-hosted (SaaS) or self-hostable? Both?

2. **Identity**: Build own identity system or require external IdP (Azure AD, Okta)?

3. **Pricing Model**: If SaaS, how to price? Per user? Per organization? Feature tiers?

4. **Data Residency**: Requirements for data location (EU, US, etc.)?

5. **Mobile Support**: Org features needed on mobile immediately or later?

6. **Integration Priority**: Which external systems (Jira, Asana, etc.) should integrate first?

---

## 10. Success Metrics

- **Adoption**: % of users who link to an organization
- **Engagement**: Weekly active users viewing org dashboards
- **Sync Health**: % successful syncs, average latency
- **Goal Achievement**: Correlation between org goal usage and goal completion rates
- **Privacy Satisfaction**: User surveys on privacy controls

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| Hub | The Organization Hub server that aggregates data |
| Org Goal | Organization-level goal (e.g., company OKR) |
| Personal Goal | Individual user's goal in Self-Organizer |
| Roll-Up | Process of aggregating individual progress into org progress |
| Sync | Process of sending progress updates from client to Hub |
| Contributor | User whose personal goals are linked to an org goal |
| Key Result | Measurable outcome tied to an Objective (OKR methodology) |

---

## Appendix B: Related Documents

- `/docs/plan.md` - Overall Self-Organizer implementation plan
- `/docs/ideas/ideas-5.md` - Original idea description
- `/src/SelfOrganizer.Core/Models/Goal.cs` - Current Goal model

---

*Document created: January 2024*
*Last updated: January 2024*
*Author: Planning Agent*
