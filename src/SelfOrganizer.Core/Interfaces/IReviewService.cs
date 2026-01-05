using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface IReviewService
{
    Task<DailySnapshot?> GetSnapshotForDateAsync(DateOnly date);
    Task<DailySnapshot> CreateOrUpdateSnapshotAsync(DateOnly date);
    Task<IEnumerable<DailySnapshot>> GetSnapshotsForRangeAsync(DateOnly start, DateOnly end);
    Task MarkDailyReviewCompletedAsync(DateOnly date);
    Task<IEnumerable<TodoTask>> GetStaleWaitingForItemsAsync(int daysThreshold = 7);
    Task<IEnumerable<TodoTask>> GetIncompleteScheduledTasksAsync(DateOnly date);
}
