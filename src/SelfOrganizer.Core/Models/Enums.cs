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
public enum GoalCategory { Career, Health, Financial, Personal, Learning, Relationships, Creative, Other }
public enum GoalTimeframe { Week, Month, Quarter, Year, MultiYear }
