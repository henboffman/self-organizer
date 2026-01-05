namespace SelfOrganizer.Core.Models;

/// <summary>
/// Daily snapshot for review and statistics
/// </summary>
public class DailySnapshot : BaseEntity
{
    public DateOnly Date { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksCreated { get; set; }
    public int CapturesProcessed { get; set; }
    public int TotalMinutesWorked { get; set; }
    public int MeetingMinutes { get; set; }
    public int DeepWorkMinutes { get; set; }
    public string? Notes { get; set; }
    public bool ReviewCompleted { get; set; }
}
