using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class ReviewService : IReviewService
{
    private readonly IRepository<DailySnapshot> _repository;
    private readonly ITaskService _taskService;
    private readonly ICaptureService _captureService;
    private readonly ISchedulingService _schedulingService;

    public ReviewService(
        IRepository<DailySnapshot> repository,
        ITaskService taskService,
        ICaptureService captureService,
        ISchedulingService schedulingService)
    {
        _repository = repository;
        _taskService = taskService;
        _captureService = captureService;
        _schedulingService = schedulingService;
    }

    public async Task<DailySnapshot?> GetSnapshotForDateAsync(DateOnly date)
    {
        var snapshots = await _repository.QueryAsync(s => s.Date == date);
        return snapshots.FirstOrDefault();
    }

    public async Task<DailySnapshot> CreateOrUpdateSnapshotAsync(DateOnly date)
    {
        var existing = await GetSnapshotForDateAsync(date);
        var snapshot = existing ?? new DailySnapshot { Date = date };

        // Load all data in parallel for better performance
        var completedTodayTask = _taskService.GetCompletedTodayCountAsync();
        var allTasksTask = _taskService.GetAllAsync();
        var unprocessedTask = _captureService.GetUnprocessedAsync();
        var todayCapturesTask = _captureService.GetTodayCaptureCountAsync();
        var timeBlocksTask = _schedulingService.GetTimeBlocksForDateAsync(date);

        await Task.WhenAll(completedTodayTask, allTasksTask, unprocessedTask, todayCapturesTask, timeBlocksTask);

        // Calculate statistics from parallel-loaded data
        snapshot.TasksCompleted = await completedTodayTask;

        var allTasks = await allTasksTask;
        snapshot.TasksCreated = allTasks.Count(t => t.CreatedAt.Date == date.ToDateTime(TimeOnly.MinValue).Date);

        var unprocessed = await unprocessedTask;
        var todayCaptures = await todayCapturesTask;
        snapshot.CapturesProcessed = todayCaptures - unprocessed.Count();

        // Calculate time from completed time blocks
        var timeBlocks = await timeBlocksTask;
        snapshot.MeetingMinutes = timeBlocks
            .Where(b => b.Type == TimeBlockType.Meeting)
            .Sum(b => b.DurationMinutes);
        snapshot.DeepWorkMinutes = timeBlocks
            .Where(b => b.Type == TimeBlockType.DeepWork)
            .Sum(b => b.DurationMinutes);
        snapshot.TotalMinutesWorked = timeBlocks
            .Where(b => b.Type != TimeBlockType.Break && b.Type != TimeBlockType.Buffer && b.Type != TimeBlockType.Available)
            .Sum(b => b.DurationMinutes);

        if (existing != null)
        {
            return await _repository.UpdateAsync(snapshot);
        }
        return await _repository.AddAsync(snapshot);
    }

    public async Task<IEnumerable<DailySnapshot>> GetSnapshotsForRangeAsync(DateOnly start, DateOnly end)
    {
        return await _repository.QueryAsync(s => s.Date >= start && s.Date <= end);
    }

    public async Task MarkDailyReviewCompletedAsync(DateOnly date)
    {
        var snapshot = await GetSnapshotForDateAsync(date);
        if (snapshot == null)
        {
            snapshot = await CreateOrUpdateSnapshotAsync(date);
        }
        snapshot.ReviewCompleted = true;
        await _repository.UpdateAsync(snapshot);
    }

    public async Task<IEnumerable<TodoTask>> GetStaleWaitingForItemsAsync(int daysThreshold = 7)
    {
        var threshold = DateTime.UtcNow.AddDays(-daysThreshold);
        var waitingFor = await _taskService.GetWaitingForAsync();
        return waitingFor.Where(t => t.WaitingForSince.HasValue && t.WaitingForSince.Value < threshold);
    }

    public async Task<IEnumerable<TodoTask>> GetIncompleteScheduledTasksAsync(DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

        var scheduled = await _taskService.GetScheduledAsync(startOfDay, endOfDay);
        return scheduled.Where(t => t.Status != TodoTaskStatus.Completed);
    }
}
