namespace SelfOrganizer.Core.Models;

/// <summary>
/// Defines a rule for automatically linking calendar events to entities (projects, goals, tasks, ideas)
/// based on pattern matching against event title, description, or attendees.
/// </summary>
public class EntityLinkRule : BaseEntity
{
    /// <summary>
    /// Human-readable name for this rule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of matching to perform
    /// </summary>
    public EntityLinkRuleType RuleType { get; set; } = EntityLinkRuleType.StringMatch;

    /// <summary>
    /// The pattern to match (string for StringMatch, regex for Regex, category name for CategoryMatch)
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pattern matching should be case-sensitive
    /// </summary>
    public bool IsCaseSensitive { get; set; } = false;

    /// <summary>
    /// The type of entity this rule links to
    /// </summary>
    public EntityLinkTargetType TargetType { get; set; }

    /// <summary>
    /// The ID of the target entity to link to
    /// </summary>
    public Guid? TargetEntityId { get; set; }

    /// <summary>
    /// Cached name of the target entity (for display purposes)
    /// </summary>
    public string? TargetEntityName { get; set; }

    /// <summary>
    /// Priority for rule evaluation (higher = evaluated first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this rule is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to apply this rule to event titles
    /// </summary>
    public bool ApplyToTitle { get; set; } = true;

    /// <summary>
    /// Whether to apply this rule to event descriptions
    /// </summary>
    public bool ApplyToDescription { get; set; } = false;

    /// <summary>
    /// Whether to apply this rule to attendee email addresses/names
    /// </summary>
    public bool ApplyToAttendees { get; set; } = false;

    /// <summary>
    /// Whether to apply this rule to event tags
    /// </summary>
    public bool ApplyToTags { get; set; } = false;

    /// <summary>
    /// The minimum confidence score (0-1) required for this rule to trigger
    /// </summary>
    public double MinConfidenceScore { get; set; } = 0.5;

    /// <summary>
    /// Notes or description about why this rule was created
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Type of pattern matching for entity link rules
/// </summary>
public enum EntityLinkRuleType
{
    /// <summary>
    /// Simple string contains/equals matching
    /// </summary>
    StringMatch,

    /// <summary>
    /// Regular expression pattern matching
    /// </summary>
    Regex,

    /// <summary>
    /// Match by meeting category (1:1, Planning, etc.)
    /// </summary>
    CategoryMatch,

    /// <summary>
    /// Match by attendee email domain or name pattern
    /// </summary>
    AttendeeMatch
}

/// <summary>
/// Type of entity that a link rule targets
/// </summary>
public enum EntityLinkTargetType
{
    Project,
    Goal,
    Task,
    Idea
}
