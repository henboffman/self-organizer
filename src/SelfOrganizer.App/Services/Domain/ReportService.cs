using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class ReportService : IReportService
{
    private readonly IRepository<TodoTask> _taskRepository;
    private readonly IRepository<CalendarEvent> _eventRepository;
    private readonly IRepository<TimeBlock> _timeBlockRepository;
    private readonly IRepository<CaptureItem> _captureRepository;
    private readonly IRepository<DailySnapshot> _snapshotRepository;

    public ReportService(
        IRepository<TodoTask> taskRepository,
        IRepository<CalendarEvent> eventRepository,
        IRepository<TimeBlock> timeBlockRepository,
        IRepository<CaptureItem> captureRepository,
        IRepository<DailySnapshot> snapshotRepository)
    {
        _taskRepository = taskRepository;
        _eventRepository = eventRepository;
        _timeBlockRepository = timeBlockRepository;
        _captureRepository = captureRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<Dictionary<DateOnly, int>> GetTasksCompletedPerWeekAsync(int weeks = 8)
    {
        var result = new Dictionary<DateOnly, int>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get start of current week (Monday)
        var daysFromMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var currentWeekStart = today.AddDays(-daysFromMonday);

        // Get all completed tasks
        var completedTasks = await _taskRepository.QueryAsync(t =>
            t.Status == TodoTaskStatus.Completed && t.CompletedAt.HasValue);

        // Group by week
        for (int i = 0; i < weeks; i++)
        {
            var weekStart = currentWeekStart.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);

            var count = completedTasks.Count(t =>
                t.CompletedAt.HasValue &&
                DateOnly.FromDateTime(t.CompletedAt.Value) >= weekStart &&
                DateOnly.FromDateTime(t.CompletedAt.Value) < weekEnd);

            result[weekStart] = count;
        }

        return result.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task<(int MeetingMinutes, int DeepWorkMinutes)> GetMeetingVsDeepWorkTimeAsync(int days = 30)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        // Get meetings from calendar events
        var events = await _eventRepository.QueryAsync(e =>
            e.StartTime >= startDate && e.StartTime <= endDate);

        var meetingMinutes = events.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes);

        // Get deep work from time blocks
        var timeBlocks = await _timeBlockRepository.QueryAsync(tb =>
            tb.StartTime >= startDate && tb.StartTime <= endDate);

        var deepWorkMinutes = timeBlocks
            .Where(tb => tb.Type == TimeBlockType.DeepWork)
            .Sum(tb => tb.DurationMinutes);

        // Also check daily snapshots if available
        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        var snapshots = await _snapshotRepository.QueryAsync(s =>
            s.Date >= startDateOnly && s.Date <= endDateOnly);

        if (snapshots.Any())
        {
            // Prefer snapshot data if available as it's more accurate
            var snapshotMeetingMinutes = snapshots.Sum(s => s.MeetingMinutes);
            var snapshotDeepWorkMinutes = snapshots.Sum(s => s.DeepWorkMinutes);

            if (snapshotMeetingMinutes > 0 || snapshotDeepWorkMinutes > 0)
            {
                return (snapshotMeetingMinutes, snapshotDeepWorkMinutes);
            }
        }

        return (meetingMinutes, deepWorkMinutes);
    }

    public async Task<Dictionary<string, int>> GetCategoryTimeBreakdownAsync(int days = 30)
    {
        var result = new Dictionary<string, int>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        // Get completed tasks with categories
        var completedTasks = await _taskRepository.QueryAsync(t =>
            t.Status == TodoTaskStatus.Completed &&
            t.CompletedAt.HasValue &&
            t.CompletedAt.Value >= startDate &&
            t.CompletedAt.Value <= endDate);

        foreach (var task in completedTasks)
        {
            var category = task.Category ?? "Uncategorized";
            var minutes = task.ActualMinutes ?? task.EstimatedMinutes;

            if (result.ContainsKey(category))
            {
                result[category] += minutes;
            }
            else
            {
                result[category] = minutes;
            }
        }

        // Also consider time blocks by category
        var timeBlocks = await _timeBlockRepository.QueryAsync(tb =>
            tb.StartTime >= startDate && tb.StartTime <= endDate && !string.IsNullOrEmpty(tb.Category));

        foreach (var block in timeBlocks)
        {
            var category = block.Category ?? "Other";
            if (result.ContainsKey(category))
            {
                result[category] += block.DurationMinutes;
            }
            else
            {
                result[category] = block.DurationMinutes;
            }
        }

        // Add meeting categories from calendar events
        var events = await _eventRepository.QueryAsync(e =>
            e.StartTime >= startDate && e.StartTime <= endDate);

        foreach (var evt in events)
        {
            var category = "Meeting: " + evt.EffectiveCategory.ToString();
            var minutes = (int)(evt.EndTime - evt.StartTime).TotalMinutes;

            if (result.ContainsKey(category))
            {
                result[category] += minutes;
            }
            else
            {
                result[category] = minutes;
            }
        }

        return result.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task<IEnumerable<DailyProductivityMetrics>> GetProductivityTrendsAsync(int days = 30)
    {
        var result = new List<DailyProductivityMetrics>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get all data
        var allTasks = await _taskRepository.GetAllAsync();
        var allCaptures = await _captureRepository.GetAllAsync();
        var allTimeBlocks = await _timeBlockRepository.GetAllAsync();
        var allEvents = await _eventRepository.GetAllAsync();
        var allSnapshots = await _snapshotRepository.GetAllAsync();

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            var nextDateTime = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

            // Check if we have a snapshot for this date
            var snapshot = allSnapshots.FirstOrDefault(s => s.Date == date);

            if (snapshot != null)
            {
                result.Add(new DailyProductivityMetrics
                {
                    Date = date,
                    TasksCompleted = snapshot.TasksCompleted,
                    TasksCreated = snapshot.TasksCreated,
                    TotalMinutesWorked = snapshot.TotalMinutesWorked,
                    MeetingMinutes = snapshot.MeetingMinutes,
                    DeepWorkMinutes = snapshot.DeepWorkMinutes,
                    CapturesProcessed = snapshot.CapturesProcessed,
                    InboxCleared = !allCaptures.Any(c =>
                        !c.IsProcessed &&
                        c.CreatedAt < nextDateTime)
                });
            }
            else
            {
                // Calculate from raw data
                var tasksCompletedOnDate = allTasks.Count(t =>
                    t.Status == TodoTaskStatus.Completed &&
                    t.CompletedAt.HasValue &&
                    DateOnly.FromDateTime(t.CompletedAt.Value) == date);

                var tasksCreatedOnDate = allTasks.Count(t =>
                    DateOnly.FromDateTime(t.CreatedAt) == date);

                var capturesProcessedOnDate = allCaptures.Count(c =>
                    c.IsProcessed &&
                    DateOnly.FromDateTime(c.ModifiedAt) == date);

                var timeBlocksOnDate = allTimeBlocks.Where(tb =>
                    DateOnly.FromDateTime(tb.StartTime) == date);

                var meetingMinutes = allEvents
                    .Where(e => DateOnly.FromDateTime(e.StartTime) == date)
                    .Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes);

                var deepWorkMinutes = timeBlocksOnDate
                    .Where(tb => tb.Type == TimeBlockType.DeepWork)
                    .Sum(tb => tb.DurationMinutes);

                var totalMinutesWorked = timeBlocksOnDate.Sum(tb => tb.DurationMinutes);

                // Check if inbox was cleared by end of day
                var unprocessedAtEndOfDay = allCaptures.Any(c =>
                    !c.IsProcessed &&
                    c.CreatedAt < nextDateTime &&
                    (!c.ModifiedAt.Date.Equals(c.CreatedAt.Date) || c.CreatedAt >= nextDateTime));

                result.Add(new DailyProductivityMetrics
                {
                    Date = date,
                    TasksCompleted = tasksCompletedOnDate,
                    TasksCreated = tasksCreatedOnDate,
                    TotalMinutesWorked = totalMinutesWorked,
                    MeetingMinutes = meetingMinutes,
                    DeepWorkMinutes = deepWorkMinutes,
                    CapturesProcessed = capturesProcessedOnDate,
                    InboxCleared = !unprocessedAtEndOfDay
                });
            }
        }

        return result.OrderBy(m => m.Date);
    }

    public async Task<int> GetInboxZeroStreakAsync()
    {
        var trends = await GetProductivityTrendsAsync(90); // Check last 90 days
        var trendsList = trends.OrderByDescending(t => t.Date).ToList();

        int streak = 0;
        foreach (var day in trendsList)
        {
            // For today, check current inbox status
            if (day.Date == DateOnly.FromDateTime(DateTime.UtcNow))
            {
                var unprocessedCaptures = await _captureRepository.QueryAsync(c => !c.IsProcessed);
                if (!unprocessedCaptures.Any())
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }
            else if (day.InboxCleared)
            {
                streak++;
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    public async Task<ProductivitySummary> GetProductivitySummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Calculate week boundaries
        var daysFromMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var currentWeekStart = today.AddDays(-daysFromMonday);
        var lastWeekStart = currentWeekStart.AddDays(-7);
        var lastWeekEnd = currentWeekStart.AddDays(-1);

        // Get tasks
        var completedTasks = await _taskRepository.QueryAsync(t =>
            t.Status == TodoTaskStatus.Completed && t.CompletedAt.HasValue);

        var completedTasksList = completedTasks.ToList();

        // Tasks completed this week
        var thisWeekTasks = completedTasksList.Count(t =>
            DateOnly.FromDateTime(t.CompletedAt!.Value) >= currentWeekStart);

        // Tasks completed last week
        var lastWeekTasks = completedTasksList.Count(t =>
            DateOnly.FromDateTime(t.CompletedAt!.Value) >= lastWeekStart &&
            DateOnly.FromDateTime(t.CompletedAt!.Value) <= lastWeekEnd);

        // Total tasks in last 30 days
        var thirtyDaysAgo = today.AddDays(-30);
        var last30DaysTasks = completedTasksList.Count(t =>
            DateOnly.FromDateTime(t.CompletedAt!.Value) >= thirtyDaysAgo);

        // Meeting vs deep work time
        var (meetingMinutes, deepWorkMinutes) = await GetMeetingVsDeepWorkTimeAsync(30);

        // Inbox zero streak
        var inboxZeroStreak = await GetInboxZeroStreakAsync();

        // Calculate longest inbox zero streak
        var trends = await GetProductivityTrendsAsync(90);
        var longestStreak = CalculateLongestStreak(trends);

        // Calculate productivity scores (simple weighted score)
        var thisWeekScore = CalculateProductivityScore(thisWeekTasks, deepWorkMinutes / 7, meetingMinutes / 7);
        var lastWeekScore = CalculateProductivityScore(lastWeekTasks, deepWorkMinutes / 7, meetingMinutes / 7);

        return new ProductivitySummary
        {
            TotalTasksCompleted = completedTasksList.Count,
            TasksCompletedThisWeek = thisWeekTasks,
            TasksCompletedLastWeek = lastWeekTasks,
            AverageTasksPerDay = last30DaysTasks / 30.0,
            TotalMeetingMinutes = meetingMinutes,
            TotalDeepWorkMinutes = deepWorkMinutes,
            MeetingToDeepWorkRatio = deepWorkMinutes > 0 ? (double)meetingMinutes / deepWorkMinutes : 0,
            InboxZeroStreak = inboxZeroStreak,
            LongestInboxZeroStreak = Math.Max(longestStreak, inboxZeroStreak),
            CurrentWeekProductivityScore = thisWeekScore,
            PreviousWeekProductivityScore = lastWeekScore
        };
    }

    private int CalculateLongestStreak(IEnumerable<DailyProductivityMetrics> trends)
    {
        var trendsList = trends.OrderBy(t => t.Date).ToList();
        int longestStreak = 0;
        int currentStreak = 0;

        foreach (var day in trendsList)
        {
            if (day.InboxCleared)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return longestStreak;
    }

    private int CalculateProductivityScore(int tasksCompleted, int deepWorkMinutesPerDay, int meetingMinutesPerDay)
    {
        // Simple productivity score: tasks * 10 + deep work bonus - meeting penalty
        var taskScore = tasksCompleted * 10;
        var deepWorkBonus = Math.Min(deepWorkMinutesPerDay / 6, 30); // Max 30 points for 3+ hours of deep work
        var meetingPenalty = Math.Max(0, (meetingMinutesPerDay - 120) / 10); // Penalty for > 2 hours meetings

        return Math.Max(0, Math.Min(100, taskScore + deepWorkBonus - meetingPenalty));
    }
}
