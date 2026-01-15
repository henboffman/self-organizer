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

    // Morning Check-in Fields
    public bool MorningCheckinCompleted { get; set; }
    public DateTime? MorningCheckinTime { get; set; }
    public int? MorningEnergy { get; set; } // 1-5 scale
    public int? MorningMood { get; set; } // 1-5 scale
    public string? MorningIntention { get; set; }
    public List<Guid> TopPriorityTaskIds { get; set; } = new();
    public string? MorningGratitude { get; set; }

    // Evening Check-out Fields (for future use)
    public bool EveningCheckoutCompleted { get; set; }
    public DateTime? EveningCheckoutTime { get; set; }
    public int? EveningAccomplishment { get; set; } // 1-5 scale
    public string? DayReflection { get; set; }
}
