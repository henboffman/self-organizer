namespace SelfOrganizer.Core.Models;

/// <summary>
/// Base entity for all persistable items
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates this entity is sample/demo data created during onboarding.
    /// When ShowSampleData is false in UserPreferences, these entities are excluded from queries.
    /// </summary>
    public bool IsSampleData { get; set; } = false;
}
