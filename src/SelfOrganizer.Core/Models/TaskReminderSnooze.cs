namespace SelfOrganizer.Core.Models;

/// <summary>
/// Tracks snoozed task reminders to avoid nagging about stale tasks the user has acknowledged
/// </summary>
public class TaskReminderSnooze : BaseEntity
{
    public Guid TaskId { get; set; }
    public DateTime SnoozedUntil { get; set; }
}
