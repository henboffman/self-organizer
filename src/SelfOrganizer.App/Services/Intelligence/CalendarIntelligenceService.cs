using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.App.Services.Domain;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Provides intelligent calendar analysis and recommendations,
/// including meeting move suggestions, focus time optimization, and project focus analysis.
/// </summary>
public interface ICalendarIntelligenceService
{
    /// <summary>
    /// Suggests if a meeting could be moved to improve project focus or create focus blocks.
    /// </summary>
    Task<MeetingMoveRecommendation?> SuggestMeetingMoveAsync(CalendarEvent evt);

    /// <summary>
    /// Finds the best time slots for focused work on a given day.
    /// </summary>
    Task<IEnumerable<FocusTimeSlot>> FindBestFocusTimeSlotsAsync(DateOnly date, int requiredMinutes);

    /// <summary>
    /// Analyzes project focus patterns for a week.
    /// </summary>
    Task<ProjectFocusAnalysis> AnalyzeProjectFocusAsync(DateOnly weekStart);

    /// <summary>
    /// Detects scheduling conflicts between tasks and calendar events.
    /// </summary>
    Task<IEnumerable<SchedulingConflict>> DetectConflictsAsync(DateOnly date);

    /// <summary>
    /// Gets optimal days for scheduling a task based on energy requirements and calendar.
    /// </summary>
    Task<IEnumerable<OptimalScheduleSlot>> GetOptimalSlotsForTaskAsync(TodoTask task, int daysToCheck = 7);
}

/// <summary>
/// Recommendation to move a meeting for better focus.
/// </summary>
public class MeetingMoveRecommendation
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? SuggestedNewTime { get; set; }
    public DateOnly? SuggestedNewDate { get; set; }
    public double ImpactScore { get; set; } // 0-1, how much this would help
    public bool IsOrganizer { get; set; } // Can the user reschedule?
}

/// <summary>
/// A time slot available for focused work.
/// </summary>
public class FocusTimeSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int DurationMinutes => (int)(End - Start).TotalMinutes;
    public double QualityScore { get; set; } // 0-1, accounts for energy, interruptions
    public string? Notes { get; set; }
}

/// <summary>
/// Analysis of project focus patterns for a week.
/// </summary>
public class ProjectFocusAnalysis
{
    public DateOnly WeekStart { get; set; }
    public List<ProjectDayDistribution> ProjectDistributions { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public int TotalProjectMeetings { get; set; }
    public int ScatteredProjects { get; set; }
    public int FocusedDays { get; set; }
}

/// <summary>
/// Distribution of a project's meetings across days.
/// </summary>
public class ProjectDayDistribution
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Dictionary<DateOnly, int> MeetingsByDay { get; set; } = new();
    public bool IsScattered => MeetingsByDay.Count >= 3 && MeetingsByDay.Values.Max() <= 2;
}

/// <summary>
/// A scheduling conflict between tasks and calendar.
/// </summary>
public class SchedulingConflict
{
    public SchedulingConflictType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? TaskId { get; set; }
    public Guid? EventId { get; set; }
    public string? Resolution { get; set; }
    public int SeverityScore { get; set; } // 1-10
}

/// <summary>
/// Types of scheduling conflicts.
/// </summary>
public enum SchedulingConflictType
{
    TaskDueNoTime,           // Task due but no focus time available
    HighEnergyMeetingHeavy,  // High-energy task on meeting-heavy day
    DeadlineConflict,        // Multiple deadlines same day
    OverScheduled            // Day is over-scheduled
}

/// <summary>
/// An optimal time slot for scheduling a task.
/// </summary>
public class OptimalScheduleSlot
{
    public DateOnly Date { get; set; }
    public TimeOnly? SuggestedStartTime { get; set; }
    public int AvailableMinutes { get; set; }
    public double FitScore { get; set; } // 0-1, how well the slot fits the task
    public string Reason { get; set; } = string.Empty;
}

public class CalendarIntelligenceService : ICalendarIntelligenceService
{
    private readonly ICalendarService _calendarService;
    private readonly IMeetingInsightService _meetingInsightService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    public CalendarIntelligenceService(
        ICalendarService calendarService,
        IMeetingInsightService meetingInsightService,
        IProjectService projectService,
        ITaskService taskService,
        IRepository<UserPreferences> preferencesRepository)
    {
        _calendarService = calendarService;
        _meetingInsightService = meetingInsightService;
        _projectService = projectService;
        _taskService = taskService;
        _preferencesRepository = preferencesRepository;
    }

    public async Task<MeetingMoveRecommendation?> SuggestMeetingMoveAsync(CalendarEvent evt)
    {
        // Only suggest moves for meetings the user organizes
        // For now, check if it's a manual event or has IsOrganizer flag
        var isOrganizer = evt.Source == "Manual";

        if (!isOrganizer)
            return null;

        var eventDate = DateOnly.FromDateTime(evt.StartTime);
        var dayAnalysis = await _meetingInsightService.AnalyzeDayAsync(eventDate);

        // Check if this event is part of a back-to-back chain
        var backToBackInsight = dayAnalysis.Insights
            .FirstOrDefault(i => i.Type == MeetingInsightType.BackToBackMeetings &&
                                 i.RelatedEventIds.Contains(evt.Id));

        if (backToBackInsight != null)
        {
            // Find a day with more breathing room
            var betterDay = await FindLessCongestedDayAsync(eventDate, (int)(evt.EndTime - evt.StartTime).TotalMinutes);
            if (betterDay.HasValue)
            {
                return new MeetingMoveRecommendation
                {
                    EventId = evt.Id,
                    EventTitle = evt.Title,
                    Reason = $"Moving would eliminate back-to-back meetings and create buffer time",
                    SuggestedNewDate = betterDay,
                    ImpactScore = 0.7,
                    IsOrganizer = isOrganizer
                };
            }
        }

        // Check if moving would create project focus
        if (evt.LinkedProjectId.HasValue)
        {
            var projectFocusDay = await FindProjectFocusDayAsync(evt.LinkedProjectId.Value, eventDate, evt.Id);
            if (projectFocusDay.HasValue && projectFocusDay != eventDate)
            {
                var project = await _projectService.GetByIdAsync(evt.LinkedProjectId.Value);
                return new MeetingMoveRecommendation
                {
                    EventId = evt.Id,
                    EventTitle = evt.Title,
                    Reason = $"Moving to {projectFocusDay.Value.DayOfWeek} would consolidate \"{project?.Name}\" meetings for better focus",
                    SuggestedNewDate = projectFocusDay,
                    ImpactScore = 0.8,
                    IsOrganizer = isOrganizer
                };
            }
        }

        return null;
    }

    public async Task<IEnumerable<FocusTimeSlot>> FindBestFocusTimeSlotsAsync(DateOnly date, int requiredMinutes)
    {
        var slots = new List<FocusTimeSlot>();
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault() ?? new UserPreferences();

        var workStart = date.ToDateTime(TimeOnly.FromTimeSpan(prefs.WorkDayStart));
        var workEnd = date.ToDateTime(TimeOnly.FromTimeSpan(prefs.WorkDayEnd));

        var events = (await _calendarService.GetEventsForDateAsync(date))
            .Where(e => !e.IsAllDay)
            .OrderBy(e => e.StartTime)
            .ToList();

        if (!events.Any())
        {
            // Whole day is available - prefer morning for high-energy work
            slots.Add(new FocusTimeSlot
            {
                Start = workStart,
                End = workStart.AddHours(3),
                QualityScore = 0.95,
                Notes = "Prime morning focus time"
            });
            slots.Add(new FocusTimeSlot
            {
                Start = workStart.AddHours(4),
                End = workEnd,
                QualityScore = 0.75,
                Notes = "Extended afternoon block"
            });
            return slots;
        }

        // Find gaps between meetings
        var currentTime = workStart;
        foreach (var evt in events)
        {
            if (evt.StartTime > currentTime)
            {
                var gapMinutes = (int)(evt.StartTime - currentTime).TotalMinutes;
                if (gapMinutes >= requiredMinutes)
                {
                    var qualityScore = CalculateFocusSlotQuality(currentTime, evt.StartTime, prefs);
                    slots.Add(new FocusTimeSlot
                    {
                        Start = currentTime,
                        End = evt.StartTime,
                        QualityScore = qualityScore,
                        Notes = GetFocusSlotNotes(currentTime)
                    });
                }
            }
            if (evt.EndTime > currentTime)
                currentTime = evt.EndTime;
        }

        // Time after last meeting
        if (currentTime < workEnd)
        {
            var gapMinutes = (int)(workEnd - currentTime).TotalMinutes;
            if (gapMinutes >= requiredMinutes)
            {
                var qualityScore = CalculateFocusSlotQuality(currentTime, workEnd, prefs);
                slots.Add(new FocusTimeSlot
                {
                    Start = currentTime,
                    End = workEnd,
                    QualityScore = qualityScore,
                    Notes = GetFocusSlotNotes(currentTime)
                });
            }
        }

        return slots.OrderByDescending(s => s.QualityScore);
    }

    public async Task<ProjectFocusAnalysis> AnalyzeProjectFocusAsync(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        var analysis = new ProjectFocusAnalysis { WeekStart = weekStart };

        var events = await _calendarService.GetEventsForRangeAsync(
            weekStart.ToDateTime(TimeOnly.MinValue),
            weekEnd.ToDateTime(TimeOnly.MaxValue));

        var projectEvents = events.Where(e => e.LinkedProjectId.HasValue).ToList();
        analysis.TotalProjectMeetings = projectEvents.Count;

        if (!projectEvents.Any())
        {
            analysis.Recommendations.Add("Consider linking calendar events to projects for better focus analysis");
            return analysis;
        }

        var projects = (await _projectService.GetAllAsync())
            .Where(p => p.Status != ProjectStatus.Completed)
            .ToDictionary(p => p.Id, p => p);

        // Group by project
        var byProject = projectEvents
            .GroupBy(e => e.LinkedProjectId!.Value)
            .Where(g => projects.ContainsKey(g.Key))
            .ToList();

        foreach (var projectGroup in byProject)
        {
            var dist = new ProjectDayDistribution
            {
                ProjectId = projectGroup.Key,
                ProjectName = projects[projectGroup.Key].Name
            };

            foreach (var evt in projectGroup)
            {
                var evtDate = DateOnly.FromDateTime(evt.StartTime);
                if (!dist.MeetingsByDay.ContainsKey(evtDate))
                    dist.MeetingsByDay[evtDate] = 0;
                dist.MeetingsByDay[evtDate]++;
            }

            analysis.ProjectDistributions.Add(dist);

            if (dist.IsScattered)
            {
                analysis.ScatteredProjects++;
                analysis.Recommendations.Add(
                    $"Consider consolidating \"{dist.ProjectName}\" meetings to fewer days for better context switching");
            }
        }

        // Check for focus days (single project dominates)
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            var dayEvents = projectEvents.Where(e => DateOnly.FromDateTime(e.StartTime) == date).ToList();
            if (dayEvents.Count >= 3)
            {
                var dominantProject = dayEvents
                    .GroupBy(e => e.LinkedProjectId)
                    .OrderByDescending(g => g.Count())
                    .First();

                if (dominantProject.Count() >= dayEvents.Count * 0.7)
                {
                    analysis.FocusedDays++;
                }
            }
        }

        if (analysis.FocusedDays > 0)
        {
            analysis.Recommendations.Add($"Great! You have {analysis.FocusedDays} project-focused day(s) this week");
        }

        return analysis;
    }

    public async Task<IEnumerable<SchedulingConflict>> DetectConflictsAsync(DateOnly date)
    {
        var conflicts = new List<SchedulingConflict>();
        var dayAnalysis = await _meetingInsightService.AnalyzeDayAsync(date);

        // Get tasks due or scheduled for this day
        var tasks = await _taskService.GetAllAsync();
        var dayTasks = tasks
            .Where(t => t.Status != TodoTaskStatus.Completed &&
                       ((t.DueDate.HasValue && DateOnly.FromDateTime(t.DueDate.Value) == date) ||
                        (t.ScheduledDate.HasValue && DateOnly.FromDateTime(t.ScheduledDate.Value) == date)))
            .ToList();

        foreach (var task in dayTasks)
        {
            // Check if task needs more time than available
            if (task.EstimatedMinutes > dayAnalysis.AvailableFocusMinutes)
            {
                conflicts.Add(new SchedulingConflict
                {
                    Type = SchedulingConflictType.TaskDueNoTime,
                    Description = $"\"{task.Title}\" needs {task.EstimatedMinutes} min but only {dayAnalysis.AvailableFocusMinutes} min available",
                    TaskId = task.Id,
                    Resolution = "Consider rescheduling the task or moving some meetings",
                    SeverityScore = task.DueDate.HasValue ? 8 : 5
                });
            }

            // Check energy mismatches
            if (task.EnergyLevel >= 4 && dayAnalysis.TotalMeetings >= 4)
            {
                conflicts.Add(new SchedulingConflict
                {
                    Type = SchedulingConflictType.HighEnergyMeetingHeavy,
                    Description = $"High-energy task \"{task.Title}\" on a day with {dayAnalysis.TotalMeetings} meetings",
                    TaskId = task.Id,
                    Resolution = "Schedule high-energy work on days with fewer meetings",
                    SeverityScore = 6
                });
            }
        }

        // Check for multiple deadlines
        var deadlineTasks = dayTasks.Where(t => t.DueDate.HasValue && t.Priority == 1).ToList();
        if (deadlineTasks.Count >= 3)
        {
            conflicts.Add(new SchedulingConflict
            {
                Type = SchedulingConflictType.DeadlineConflict,
                Description = $"{deadlineTasks.Count} high-priority deadlines on {date:dddd}",
                Resolution = "Review and potentially extend some deadlines",
                SeverityScore = 9
            });
        }

        // Check for over-scheduled day
        var meetingPercent = dayAnalysis.TotalMeetingMinutes > 0
            ? (dayAnalysis.TotalMeetingMinutes * 100) / 480
            : 0;
        var taskMinutes = dayTasks.Sum(t => t.EstimatedMinutes);

        if (meetingPercent >= 70 && taskMinutes > 60)
        {
            conflicts.Add(new SchedulingConflict
            {
                Type = SchedulingConflictType.OverScheduled,
                Description = $"Day is {meetingPercent}% meetings plus {taskMinutes} min of tasks",
                Resolution = "Consider moving tasks or declining some meetings",
                SeverityScore = 7
            });
        }

        return conflicts.OrderByDescending(c => c.SeverityScore);
    }

    public async Task<IEnumerable<OptimalScheduleSlot>> GetOptimalSlotsForTaskAsync(TodoTask task, int daysToCheck = 7)
    {
        var slots = new List<OptimalScheduleSlot>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault() ?? new UserPreferences();

        for (var i = 0; i < daysToCheck; i++)
        {
            var checkDate = today.AddDays(i);

            // Skip weekends by default
            if (checkDate.DayOfWeek == DayOfWeek.Saturday || checkDate.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var dayAnalysis = await _meetingInsightService.AnalyzeDayAsync(checkDate);

            // Check if there's enough time
            if (dayAnalysis.AvailableFocusMinutes < task.EstimatedMinutes)
                continue;

            // Calculate fit score
            var fitScore = CalculateTaskFitScore(task, dayAnalysis, checkDate);

            // Find best focus slot
            var focusSlots = await FindBestFocusTimeSlotsAsync(checkDate, task.EstimatedMinutes);
            var bestSlot = focusSlots.FirstOrDefault();

            slots.Add(new OptimalScheduleSlot
            {
                Date = checkDate,
                SuggestedStartTime = bestSlot != null ? TimeOnly.FromDateTime(bestSlot.Start) : null,
                AvailableMinutes = dayAnalysis.AvailableFocusMinutes,
                FitScore = fitScore,
                Reason = GetSlotReason(fitScore, dayAnalysis, task)
            });
        }

        return slots.OrderByDescending(s => s.FitScore).Take(3);
    }

    #region Private Helpers

    private async Task<DateOnly?> FindLessCongestedDayAsync(DateOnly currentDate, int meetingDuration)
    {
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault() ?? new UserPreferences();

        for (var i = 1; i <= 5; i++)
        {
            var checkDate = currentDate.AddDays(i);

            // Skip weekends
            if (checkDate.DayOfWeek == DayOfWeek.Saturday || checkDate.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var analysis = await _meetingInsightService.AnalyzeDayAsync(checkDate);

            // Less than 50% meeting time and no back-to-back issues
            var meetingPercent = analysis.TotalMeetingMinutes * 100 / 480;
            var noBackToBack = !analysis.Insights.Any(i => i.Type == MeetingInsightType.BackToBackMeetings);

            if (meetingPercent < 50 && noBackToBack && analysis.AvailableFocusMinutes >= meetingDuration + 30)
            {
                return checkDate;
            }
        }

        return null;
    }

    private async Task<DateOnly?> FindProjectFocusDayAsync(Guid projectId, DateOnly currentDate, Guid excludeEventId)
    {
        var weekStart = currentDate.AddDays(-(int)currentDate.DayOfWeek);
        var weekEnd = weekStart.AddDays(6);

        var events = await _calendarService.GetEventsForRangeAsync(
            weekStart.ToDateTime(TimeOnly.MinValue),
            weekEnd.ToDateTime(TimeOnly.MaxValue));

        // Find days that already have this project's meetings
        var projectMeetingDays = events
            .Where(e => e.LinkedProjectId == projectId && e.Id != excludeEventId)
            .GroupBy(e => DateOnly.FromDateTime(e.StartTime))
            .OrderByDescending(g => g.Count())
            .ToList();

        if (projectMeetingDays.Any())
        {
            // Return the day with most project meetings
            return projectMeetingDays.First().Key;
        }

        return null;
    }

    private double CalculateFocusSlotQuality(DateTime start, DateTime end, UserPreferences prefs)
    {
        var duration = (end - start).TotalMinutes;
        var hour = start.Hour;

        // Base score from duration (longer = better, up to a point)
        var durationScore = Math.Min(1.0, duration / 180.0);

        // Time of day score (morning is typically best for focus)
        var timeScore = hour switch
        {
            >= 9 and <= 11 => 1.0,  // Peak morning
            >= 8 and <= 12 => 0.9,  // Morning
            >= 14 and <= 16 => 0.7, // Afternoon recovery
            >= 12 and <= 14 => 0.5, // Post-lunch dip
            _ => 0.6
        };

        return (durationScore * 0.6) + (timeScore * 0.4);
    }

    private static string GetFocusSlotNotes(DateTime start)
    {
        var hour = start.Hour;
        return hour switch
        {
            < 10 => "Prime morning focus time - ideal for deep work",
            < 12 => "Late morning - good for complex tasks",
            < 14 => "Post-lunch - may have lower energy",
            < 16 => "Afternoon - good for collaborative work",
            _ => "End of day - good for administrative tasks"
        };
    }

    private double CalculateTaskFitScore(TodoTask task, DayMeetingAnalysis dayAnalysis, DateOnly date)
    {
        var score = 0.5; // Base score

        // More available time = higher score
        var timeRatio = Math.Min(1.0, dayAnalysis.AvailableFocusMinutes / (double)Math.Max(30, task.EstimatedMinutes));
        score += timeRatio * 0.3;

        // Energy matching
        var meetingIntensity = dayAnalysis.TotalMeetings / 8.0; // 8+ meetings = max intensity
        if (task.EnergyLevel >= 4 && meetingIntensity < 0.5)
            score += 0.15; // Low meeting day good for high-energy tasks
        else if (task.EnergyLevel <= 2 && meetingIntensity > 0.5)
            score += 0.1; // High meeting day ok for low-energy tasks

        // Due date proximity
        if (task.DueDate.HasValue)
        {
            var daysUntilDue = (task.DueDate.Value.Date - date.ToDateTime(TimeOnly.MinValue)).Days;
            if (daysUntilDue == 0)
                score -= 0.1; // Same day is risky
            else if (daysUntilDue == 1)
                score += 0.05; // Day before is good
        }

        return Math.Max(0, Math.Min(1, score));
    }

    private static string GetSlotReason(double fitScore, DayMeetingAnalysis dayAnalysis, TodoTask task)
    {
        if (fitScore >= 0.8)
            return $"Excellent fit - {dayAnalysis.AvailableFocusMinutes} min focus time, light meeting load";
        if (fitScore >= 0.6)
            return $"Good fit - {dayAnalysis.AvailableFocusMinutes} min available";
        if (fitScore >= 0.4)
            return $"Moderate fit - some meeting conflicts";
        return "Tight schedule - consider alternative days";
    }

    #endregion
}
