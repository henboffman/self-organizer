namespace SelfOrganizer.Core.Models;

public enum ProcessedItemType
{
    Task,
    Project,
    Reference,
    Calendar,
    SomedayMaybe
}

public enum TodoTaskStatus
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

public enum ProjectStatus
{
    Active,
    OnHold,
    SomedayMaybe,
    Completed,
    Deleted
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

/// <summary>
/// Recurrence patterns for recurring tasks
/// </summary>
public enum RecurrencePattern
{
    /// <summary>Every day</summary>
    Daily,
    /// <summary>Every weekday (Mon-Fri)</summary>
    Weekdays,
    /// <summary>Every week on the same day</summary>
    Weekly,
    /// <summary>Every two weeks</summary>
    Biweekly,
    /// <summary>Every month on the same date</summary>
    Monthly,
    /// <summary>Every quarter</summary>
    Quarterly,
    /// <summary>Every year</summary>
    Yearly,
    /// <summary>Custom interval specified in RecurrenceIntervalDays</summary>
    Custom
}

public enum GoalStatus { Active, OnHold, Completed, Archived }

public enum CareerPlanStatus { Draft, Active, OnHold, Completed, Archived }
public enum MilestoneStatus { NotStarted, InProgress, Completed, Skipped }
public enum MilestoneCategory { Role, Certification, Skill, Project, Education, Networking, Leadership, Other }
public enum GoalCategory { Career, Health, Financial, Personal, Learning, Relationships, Creative, Other }
public enum GoalTimeframe { Week, Month, Quarter, Year, MultiYear }

/// <summary>
/// Categories for organizing skills
/// </summary>
public enum SkillCategory
{
    /// <summary>Programming, engineering, technical knowledge</summary>
    Technical,
    /// <summary>Communication, leadership, collaboration</summary>
    SoftSkills,
    /// <summary>Design, writing, artistic abilities</summary>
    Creative,
    /// <summary>Industry expertise, subject matter knowledge</summary>
    DomainKnowledge,
    /// <summary>Specific tools, platforms, software proficiency</summary>
    ToolsSoftware
}

/// <summary>
/// Type of skill relationship
/// </summary>
public enum SkillType
{
    /// <summary>User currently possesses this skill</summary>
    Have,
    /// <summary>User wants to develop this skill</summary>
    Want
}

/// <summary>
/// App mode that determines which contexts and balance dimensions are shown
/// </summary>
public enum AppMode
{
    /// <summary>Professional focus - career-oriented contexts and dimensions</summary>
    Work,
    /// <summary>Personal focus - life-oriented contexts and dimensions</summary>
    Life,
    /// <summary>Hybrid mode - curated mix of both work and life dimensions</summary>
    Balanced
}

/// <summary>
/// Types of ambient focus sounds for concentration
/// </summary>
public enum FocusSoundType
{
    /// <summary>No ambient sound</summary>
    None,
    /// <summary>White noise</summary>
    WhiteNoise,
    /// <summary>Brown noise (deeper, less harsh than white)</summary>
    BrownNoise,
    /// <summary>Pink noise (balanced frequency)</summary>
    PinkNoise,
    /// <summary>Rain sounds</summary>
    Rain,
    /// <summary>Ocean waves</summary>
    Ocean,
    /// <summary>Forest ambience</summary>
    Forest,
    /// <summary>Coffee shop background</summary>
    CoffeeShop,
    /// <summary>Fireplace crackling</summary>
    Fireplace
}

public enum SnapshotGranularity { Monthly, Quarterly, Manual }
public enum SnapshotTrigger { Auto, Manual, WeeklyReview, MilestoneCompletion }
