using System.Text;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class SummaryService : ISummaryService
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IGoalService _goalService;
    private readonly IRepository<CaptureItem> _captureRepository;
    private readonly IRepository<FocusSessionLog> _focusSessionRepository;
    private readonly IRepository<Habit> _habitRepository;
    private readonly IRepository<HabitLog> _habitLogRepository;
    private readonly IRepository<CalendarEvent> _calendarRepository;

    public SummaryService(
        ITaskService taskService,
        IProjectService projectService,
        IGoalService goalService,
        IRepository<CaptureItem> captureRepository,
        IRepository<FocusSessionLog> focusSessionRepository,
        IRepository<Habit> habitRepository,
        IRepository<HabitLog> habitLogRepository,
        IRepository<CalendarEvent> calendarRepository)
    {
        _taskService = taskService;
        _projectService = projectService;
        _goalService = goalService;
        _captureRepository = captureRepository;
        _focusSessionRepository = focusSessionRepository;
        _habitRepository = habitRepository;
        _habitLogRepository = habitLogRepository;
        _calendarRepository = calendarRepository;
    }

    public async Task<SummaryReport> GenerateSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var report = new SummaryReport
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Get all tasks and filter completed ones in the period
        var allTasks = await _taskService.GetAllAsync();
        var taskList = allTasks.ToList();
        var completedTasks = taskList
            .Where(t => t.Status == TodoTaskStatus.Completed &&
                        t.CompletedAt.HasValue &&
                        t.CompletedAt.Value >= startDate &&
                        t.CompletedAt.Value <= endDate)
            .ToList();

        // Get all projects
        var allProjects = await _projectService.GetAllAsync();
        var projectList = allProjects.ToList();
        var projectDict = projectList.ToDictionary(p => p.Id);

        // Populate task statistics
        report.TotalTasksCompleted = completedTasks.Count;
        report.TotalEstimatedMinutes = completedTasks.Sum(t => t.EstimatedMinutes);
        report.TotalActualMinutes = completedTasks.Sum(t => t.ActualMinutes ?? t.EstimatedMinutes);
        report.DeepWorkTasksCompleted = completedTasks.Count(t => t.RequiresDeepWork);
        report.HighPriorityTasksCompleted = completedTasks.Count(t => t.Priority == 1);

        // Completed tasks summary
        report.CompletedTasks = completedTasks.Select(t => new CompletedTaskSummary
        {
            Id = t.Id,
            Title = t.Title,
            ProjectName = t.ProjectId.HasValue && projectDict.ContainsKey(t.ProjectId.Value)
                ? projectDict[t.ProjectId.Value].Name
                : null,
            CompletedAt = t.CompletedAt!.Value,
            EstimatedMinutes = t.EstimatedMinutes,
            ActualMinutes = t.ActualMinutes,
            Contexts = t.Contexts,
            Tags = t.Tags,
            RequiresDeepWork = t.RequiresDeepWork,
            Priority = t.Priority
        }).OrderByDescending(t => t.CompletedAt).ToList();

        // Time allocation by context
        var contextTime = new Dictionary<string, int>();
        foreach (var task in completedTasks)
        {
            var minutes = task.ActualMinutes ?? task.EstimatedMinutes;
            foreach (var context in task.Contexts)
            {
                if (!contextTime.ContainsKey(context))
                    contextTime[context] = 0;
                contextTime[context] += minutes;
            }
            // If no context, count as "Unassigned"
            if (!task.Contexts.Any())
            {
                if (!contextTime.ContainsKey("Unassigned"))
                    contextTime["Unassigned"] = 0;
                contextTime["Unassigned"] += minutes;
            }
        }
        report.TimeByContext = contextTime.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        // Time allocation by category
        var categoryTime = new Dictionary<string, int>();
        foreach (var task in completedTasks)
        {
            var minutes = task.ActualMinutes ?? task.EstimatedMinutes;
            var category = task.Category ?? "Uncategorized";
            if (!categoryTime.ContainsKey(category))
                categoryTime[category] = 0;
            categoryTime[category] += minutes;
        }
        report.TimeByCategory = categoryTime.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        // Project summaries - projects that had tasks completed in this period
        var projectIdsWithWork = completedTasks
            .Where(t => t.ProjectId.HasValue)
            .Select(t => t.ProjectId!.Value)
            .Distinct()
            .ToList();

        var projectSummaries = new List<ProjectSummary>();
        foreach (var projectId in projectIdsWithWork)
        {
            if (!projectDict.ContainsKey(projectId)) continue;

            var project = projectDict[projectId];
            var projectTasks = completedTasks.Where(t => t.ProjectId == projectId).ToList();
            var allProjectTasks = taskList.Where(t => t.ProjectId == projectId).ToList();
            var remainingTasks = allProjectTasks.Count(t => t.Status != TodoTaskStatus.Completed && t.Status != TodoTaskStatus.Deleted);

            projectSummaries.Add(new ProjectSummary
            {
                Id = project.Id,
                Name = project.Name,
                Category = project.Category,
                Status = project.Status,
                TasksCompleted = projectTasks.Count,
                TasksRemaining = remainingTasks,
                TotalMinutesSpent = projectTasks.Sum(t => t.ActualMinutes ?? t.EstimatedMinutes),
                WasCompletedInPeriod = project.Status == ProjectStatus.Completed &&
                                        project.CompletedAt.HasValue &&
                                        project.CompletedAt.Value >= startDate &&
                                        project.CompletedAt.Value <= endDate,
                CompletedAt = project.CompletedAt
            });
        }
        report.Projects = projectSummaries.OrderByDescending(p => p.TasksCompleted).ToList();
        report.ProjectsWorkedOn = projectSummaries.Count;

        // Time by project
        var projectTime = new Dictionary<string, int>();
        foreach (var ps in projectSummaries)
        {
            projectTime[ps.Name] = ps.TotalMinutesSpent;
        }
        // Add time from tasks without projects
        var noProjectMinutes = completedTasks
            .Where(t => !t.ProjectId.HasValue)
            .Sum(t => t.ActualMinutes ?? t.EstimatedMinutes);
        if (noProjectMinutes > 0)
        {
            projectTime["(No Project)"] = noProjectMinutes;
        }
        report.TimeByProject = projectTime.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        // Capture items / ideas from the period
        var captureItems = await _captureRepository.QueryAsync(c =>
            c.CreatedAt >= startDate && c.CreatedAt <= endDate);
        var captureList = captureItems.ToList();
        report.TotalCaptureItems = captureList.Count;
        report.CaptureItems = captureList.Select(c => new CaptureItemSummary
        {
            Id = c.Id,
            Text = c.CleanedText.Length > 0 ? c.CleanedText : c.RawText,
            CapturedAt = c.CreatedAt,
            IsProcessed = c.IsProcessed,
            Tags = c.ExtractedTags
        }).OrderByDescending(c => c.CapturedAt).ToList();

        // Goal progress
        var allGoals = await _goalService.GetAllAsync();
        var goalSummaries = new List<GoalProgressSummary>();

        // Pre-fetch habits for linking
        var allHabitsForGoals = await _habitRepository.GetAllAsync();
        var activeHabitsDict = allHabitsForGoals.Where(h => h.IsActive).ToDictionary(h => h.Id);

        foreach (var goal in allGoals.Where(g => g.Status != GoalStatus.Archived))
        {
            // Calculate linked tasks completed in period
            var linkedTasksCompleted = completedTasks.Count(t => goal.LinkedTaskIds.Contains(t.Id) ||
                                                                 t.GoalIds.Contains(goal.Id));

            // Calculate linked projects worked on
            var linkedProjectsWorkedOn = projectIdsWithWork.Count(pid => goal.LinkedProjectIds.Contains(pid));

            // Determine if goal was completed in this period
            var wasCompletedInPeriod = goal.Status == GoalStatus.Completed &&
                                       goal.CompletedAt.HasValue &&
                                       goal.CompletedAt.Value >= startDate &&
                                       goal.CompletedAt.Value <= endDate;

            // Get linked habits and their stats (bidirectional linking)
            var linkedHabitIds = new HashSet<Guid>(goal.LinkedHabitIds);
            foreach (var habit in activeHabitsDict.Values.Where(h => h.LinkedGoalIds.Contains(goal.Id)))
            {
                linkedHabitIds.Add(habit.Id);
            }

            var linkedHabitsInfo = new List<LinkedHabitInfo>();
            foreach (var habitId in linkedHabitIds)
            {
                if (!activeHabitsDict.TryGetValue(habitId, out var habit)) continue;

                // Will calculate stats after habitLogs are loaded
                linkedHabitsInfo.Add(new LinkedHabitInfo
                {
                    HabitId = habit.Id,
                    Name = habit.Name,
                    IsAiSuggested = habit.IsAiSuggested
                });
            }

            goalSummaries.Add(new GoalProgressSummary
            {
                Id = goal.Id,
                Title = goal.Title,
                Category = goal.Category,
                Status = goal.Status,
                StartProgressPercent = wasCompletedInPeriod ? 0 : goal.ProgressPercent, // Simplified - we don't track historical progress
                EndProgressPercent = goal.ProgressPercent,
                ProgressChange = wasCompletedInPeriod ? 100 : 0, // Would need historical data for accurate tracking
                TargetDate = goal.TargetDate,
                WasCompletedInPeriod = wasCompletedInPeriod,
                LinkedTasksCompleted = linkedTasksCompleted,
                LinkedProjectsWorkedOn = linkedProjectsWorkedOn,
                LinkedHabits = linkedHabitsInfo
            });
        }
        report.GoalProgress = goalSummaries
            .OrderByDescending(g => g.WasCompletedInPeriod)
            .ThenByDescending(g => g.LinkedTasksCompleted)
            .ToList();

        // Top tags from completed tasks
        var tagCounts = new Dictionary<string, int>();
        foreach (var task in completedTasks)
        {
            foreach (var tag in task.Tags)
            {
                if (!tagCounts.ContainsKey(tag))
                    tagCounts[tag] = 0;
                tagCounts[tag]++;
            }
        }
        report.TopTags = tagCounts
            .OrderByDescending(x => x.Value)
            .Take(10)
            .Select(x => x.Key)
            .ToList();

        // Tasks completed by day of week
        report.TasksCompletedByDayOfWeek = completedTasks
            .GroupBy(t => t.CompletedAt!.Value.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        // Average task completion rate (simplified: tasks per day in the period)
        var daysInPeriod = (endDate - startDate).TotalDays;
        if (daysInPeriod > 0)
        {
            report.AverageTaskCompletionRate = report.TotalTasksCompleted / daysInPeriod;
        }

        // Focus Sessions
        var focusSessions = await _focusSessionRepository.QueryAsync(fs =>
            fs.EndedAt >= startDate && fs.EndedAt <= endDate);
        var focusSessionList = focusSessions.ToList();

        report.TotalFocusSessions = focusSessionList.Count;
        report.TotalFocusMinutes = focusSessionList.Sum(fs => fs.DurationMinutes);
        report.AverageFocusRating = focusSessionList.Any()
            ? focusSessionList.Average(fs => fs.FocusRating)
            : 0;
        report.FocusSessionsDistracted = focusSessionList.Count(fs => fs.WasDistracted);
        report.FocusSessions = focusSessionList
            .OrderByDescending(fs => fs.StartedAt)
            .Take(20)
            .Select(fs => new FocusSessionSummary
            {
                Id = fs.Id,
                TaskTitle = fs.TaskTitle,
                DurationMinutes = fs.DurationMinutes,
                StartedAt = fs.StartedAt,
                FocusRating = fs.FocusRating,
                WasDistracted = fs.WasDistracted,
                TaskCompleted = fs.TaskCompleted,
                Context = fs.Context
            })
            .ToList();

        // Habits
        var allHabits = await _habitRepository.QueryAsync(h => h.IsActive);
        var habitList = allHabits.ToList();
        var habitLogs = await _habitLogRepository.QueryAsync(hl =>
            hl.Date >= DateOnly.FromDateTime(startDate) && hl.Date <= DateOnly.FromDateTime(endDate));
        var habitLogList = habitLogs.ToList();

        report.TotalHabitsTracked = habitList.Count;
        report.TotalHabitCompletions = habitLogList.Count(hl => hl.Completed);

        // Build goal title lookup for habit linking
        var goalTitles = allGoals.ToDictionary(g => g.Id, g => g.Title);

        var habitSummaries = new List<HabitSummary>();
        foreach (var habit in habitList)
        {
            var logsForHabit = habitLogList.Where(hl => hl.HabitId == habit.Id).ToList();
            var completions = logsForHabit.Count(hl => hl.Completed);
            var daysTracked = logsForHabit.Select(hl => hl.Date).Distinct().Count();

            // Calculate expected completions based on frequency, but only for days since habit was created
            var effectiveStartDate = habit.StartDate > startDate ? habit.StartDate : startDate;
            var activeDaysInPeriod = Math.Max(1, (int)(endDate - effectiveStartDate).TotalDays + 1);
            var expectedCompletions = CalculateExpectedCompletions(habit, effectiveStartDate, endDate);
            var completionRate = expectedCompletions > 0
                ? (double)completions / expectedCompletions * 100
                : 0;
            var currentStreak = CalculateCurrentStreak(habit.Id, habitLogList);

            // Get linked goal titles (bidirectional)
            var linkedGoalIds = new HashSet<Guid>(habit.LinkedGoalIds);
            foreach (var goal in allGoals.Where(g => g.LinkedHabitIds.Contains(habit.Id)))
            {
                linkedGoalIds.Add(goal.Id);
            }
            var linkedGoalTitles = linkedGoalIds
                .Where(id => goalTitles.ContainsKey(id))
                .Select(id => goalTitles[id])
                .ToList();

            habitSummaries.Add(new HabitSummary
            {
                Id = habit.Id,
                Name = habit.Name,
                Icon = habit.Icon,
                Color = habit.Color,
                Frequency = habit.Frequency,
                TargetCount = habit.TargetCount,
                CompletionsInPeriod = completions,
                DaysTracked = daysTracked,
                CompletionRate = Math.Min(completionRate, 100),
                CurrentStreak = currentStreak,
                ActiveDaysInPeriod = activeDaysInPeriod,
                HabitStartDate = habit.StartDate,
                IsAiSuggested = habit.IsAiSuggested,
                LinkedGoalTitles = linkedGoalTitles
            });

            // Update linked habit info in goal summaries with stats
            foreach (var goalSummary in report.GoalProgress)
            {
                var linkedHabitInfo = goalSummary.LinkedHabits.FirstOrDefault(lh => lh.HabitId == habit.Id);
                if (linkedHabitInfo != null)
                {
                    linkedHabitInfo.CompletionsInPeriod = completions;
                    linkedHabitInfo.CompletionRate = Math.Min(completionRate, 100);
                    linkedHabitInfo.CurrentStreak = currentStreak;
                }
            }
        }
        report.HabitProgress = habitSummaries.OrderByDescending(h => h.CompletionRate).ToList();
        report.HabitCompletionRate = habitSummaries.Any()
            ? habitSummaries.Average(h => h.CompletionRate)
            : 0;

        // Calendar Events / Meetings
        var calendarEvents = await _calendarRepository.QueryAsync(ce =>
            ce.StartTime >= startDate && ce.StartTime <= endDate);
        var eventList = calendarEvents.ToList();

        report.TotalMeetings = eventList.Count;
        report.TotalMeetingMinutes = eventList.Sum(e => (int)(e.EndTime - e.StartTime).TotalMinutes);
        report.MeetingsByCategory = eventList
            .GroupBy(e => e.EffectiveCategory.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return report;
    }

    private static int CalculateExpectedCompletions(Habit habit, DateTime startDate, DateTime endDate)
    {
        var days = (int)(endDate - startDate).TotalDays + 1;

        return habit.Frequency switch
        {
            HabitFrequency.Daily => days * habit.TargetCount,
            HabitFrequency.Weekly => (days / 7 + 1) * habit.TargetCount,
            HabitFrequency.Weekdays => CountWeekdays(startDate, endDate) * habit.TargetCount,
            HabitFrequency.Weekends => CountWeekendDays(startDate, endDate) * habit.TargetCount,
            HabitFrequency.Monthly => (days / 30 + 1) * habit.TargetCount,
            _ => days * habit.TargetCount
        };
    }

    private static int CountWeekdays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static int CountWeekendDays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static int CalculateCurrentStreak(Guid habitId, List<HabitLog> logs)
    {
        var habitLogs = logs
            .Where(l => l.HabitId == habitId && l.Completed)
            .OrderByDescending(l => l.Date)
            .ToList();

        if (!habitLogs.Any()) return 0;

        var streak = 0;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var expectedDate = today;

        foreach (var log in habitLogs)
        {
            // Allow for 1-day gap (yesterday or today)
            if (log.Date == expectedDate || log.Date == expectedDate.AddDays(-1))
            {
                streak++;
                expectedDate = log.Date.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    public string ExportToMarkdown(SummaryReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Work Summary Report");
        sb.AppendLine();
        sb.AppendLine($"**Period:** {report.StartDate:MMMM d, yyyy} - {report.EndDate:MMMM d, yyyy}");
        sb.AppendLine($"**Generated:** {report.GeneratedAt:MMMM d, yyyy h:mm tt}");
        sb.AppendLine();

        // Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Tasks Completed:** {report.TotalTasksCompleted}");
        sb.AppendLine($"- **Projects Worked On:** {report.ProjectsWorkedOn}");
        sb.AppendLine($"- **Total Time Tracked:** {FormatMinutes(report.TotalActualMinutes)}");
        sb.AppendLine($"- **Focus Sessions:** {report.TotalFocusSessions} ({FormatMinutes(report.TotalFocusMinutes)})");
        sb.AppendLine($"- **Habits Completed:** {report.TotalHabitCompletions}");
        sb.AppendLine($"- **Meetings:** {report.TotalMeetings} ({FormatMinutes(report.TotalMeetingMinutes)})");
        if (report.AverageTaskCompletionRate > 0)
        {
            sb.AppendLine($"- **Avg Tasks/Day:** {report.AverageTaskCompletionRate:F1}");
        }
        sb.AppendLine();

        // Goals Progress
        if (report.GoalProgress.Any())
        {
            sb.AppendLine("## Goals Progress");
            sb.AppendLine();
            foreach (var goal in report.GoalProgress)
            {
                var statusIcon = goal.Status switch
                {
                    GoalStatus.Completed => "[COMPLETED]",
                    GoalStatus.Active => "[ACTIVE]",
                    GoalStatus.OnHold => "[ON HOLD]",
                    _ => ""
                };
                sb.AppendLine($"### {goal.Title} {statusIcon}");
                sb.AppendLine();
                sb.AppendLine($"- **Category:** {goal.Category}");
                sb.AppendLine($"- **Progress:** {goal.EndProgressPercent}%");
                if (goal.TargetDate.HasValue)
                {
                    sb.AppendLine($"- **Target Date:** {goal.TargetDate.Value:MMMM d, yyyy}");
                }
                if (goal.LinkedTasksCompleted > 0)
                {
                    sb.AppendLine($"- **Related Tasks Completed:** {goal.LinkedTasksCompleted}");
                }
                if (goal.LinkedProjectsWorkedOn > 0)
                {
                    sb.AppendLine($"- **Related Projects Worked On:** {goal.LinkedProjectsWorkedOn}");
                }
                sb.AppendLine();
            }
        }

        // Focus Sessions
        if (report.FocusSessions.Any())
        {
            sb.AppendLine("## Focus Sessions");
            sb.AppendLine();
            sb.AppendLine($"- **Total Sessions:** {report.TotalFocusSessions}");
            sb.AppendLine($"- **Total Time:** {FormatMinutes(report.TotalFocusMinutes)}");
            sb.AppendLine($"- **Average Rating:** {report.AverageFocusRating:F1}/5");
            sb.AppendLine($"- **Distracted Sessions:** {report.FocusSessionsDistracted}");
            sb.AppendLine();
        }

        // Habits
        if (report.HabitProgress.Any())
        {
            sb.AppendLine("## Habit Progress");
            sb.AppendLine();
            sb.AppendLine($"Overall completion rate: **{report.HabitCompletionRate:F0}%**");
            sb.AppendLine();
            foreach (var habit in report.HabitProgress)
            {
                var streakBadge = habit.CurrentStreak > 0 ? $" 🔥 {habit.CurrentStreak}" : "";
                sb.AppendLine($"- **{habit.Name}:** {habit.CompletionsInPeriod} completions ({habit.CompletionRate:F0}%){streakBadge}");
            }
            sb.AppendLine();
        }

        // Meetings
        if (report.TotalMeetings > 0)
        {
            sb.AppendLine("## Meetings");
            sb.AppendLine();
            sb.AppendLine($"- **Total Meetings:** {report.TotalMeetings}");
            sb.AppendLine($"- **Total Meeting Time:** {FormatMinutes(report.TotalMeetingMinutes)}");
            if (report.MeetingsByCategory.Any())
            {
                sb.AppendLine();
                sb.AppendLine("### By Category");
                foreach (var (category, count) in report.MeetingsByCategory.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"- **{category}:** {count}");
                }
            }
            sb.AppendLine();
        }

        // Projects
        if (report.Projects.Any())
        {
            sb.AppendLine("## Projects");
            sb.AppendLine();
            foreach (var project in report.Projects)
            {
                var statusBadge = project.WasCompletedInPeriod ? " [COMPLETED]" : "";
                sb.AppendLine($"### {project.Name}{statusBadge}");
                sb.AppendLine();
                sb.AppendLine($"- **Tasks Completed:** {project.TasksCompleted}");
                sb.AppendLine($"- **Tasks Remaining:** {project.TasksRemaining}");
                sb.AppendLine($"- **Time Spent:** {FormatMinutes(project.TotalMinutesSpent)}");
                if (!string.IsNullOrEmpty(project.Category))
                {
                    sb.AppendLine($"- **Category:** {project.Category}");
                }
                sb.AppendLine();
            }
        }

        // Time Allocation
        sb.AppendLine("## Time Allocation");
        sb.AppendLine();

        if (report.TimeByContext.Any())
        {
            sb.AppendLine("### By Context");
            sb.AppendLine();
            foreach (var (context, minutes) in report.TimeByContext)
            {
                var percentage = report.TotalActualMinutes > 0
                    ? (double)minutes / report.TotalActualMinutes * 100
                    : 0;
                sb.AppendLine($"- **{context}:** {FormatMinutes(minutes)} ({percentage:F1}%)");
            }
            sb.AppendLine();
        }

        if (report.TimeByCategory.Any())
        {
            sb.AppendLine("### By Category");
            sb.AppendLine();
            foreach (var (category, minutes) in report.TimeByCategory)
            {
                var percentage = report.TotalActualMinutes > 0
                    ? (double)minutes / report.TotalActualMinutes * 100
                    : 0;
                sb.AppendLine($"- **{category}:** {FormatMinutes(minutes)} ({percentage:F1}%)");
            }
            sb.AppendLine();
        }

        // Task List
        if (report.CompletedTasks.Any())
        {
            sb.AppendLine("## Completed Tasks");
            sb.AppendLine();
            sb.AppendLine("| Task | Project | Completed | Time |");
            sb.AppendLine("|------|---------|-----------|------|");
            foreach (var task in report.CompletedTasks.Take(50)) // Limit to 50 for readability
            {
                var projectName = task.ProjectName ?? "-";
                var time = task.ActualMinutes.HasValue
                    ? FormatMinutes(task.ActualMinutes.Value)
                    : FormatMinutes(task.EstimatedMinutes);
                sb.AppendLine($"| {EscapeMarkdown(task.Title)} | {EscapeMarkdown(projectName)} | {task.CompletedAt:MMM d} | {time} |");
            }
            if (report.CompletedTasks.Count > 50)
            {
                sb.AppendLine();
                sb.AppendLine($"*...and {report.CompletedTasks.Count - 50} more tasks*");
            }
            sb.AppendLine();
        }

        // Ideas/Captures
        if (report.CaptureItems.Any())
        {
            sb.AppendLine("## Ideas & Captures");
            sb.AppendLine();
            foreach (var item in report.CaptureItems.Take(20))
            {
                var processedBadge = item.IsProcessed ? " [processed]" : "";
                sb.AppendLine($"- {EscapeMarkdown(item.Text)}{processedBadge} *(captured {item.CapturedAt:MMM d})*");
            }
            if (report.CaptureItems.Count > 20)
            {
                sb.AppendLine();
                sb.AppendLine($"*...and {report.CaptureItems.Count - 20} more items*");
            }
            sb.AppendLine();
        }

        // Top Tags
        if (report.TopTags.Any())
        {
            sb.AppendLine("## Top Tags");
            sb.AppendLine();
            sb.AppendLine(string.Join(", ", report.TopTags.Select(t => $"`#{t}`")));
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*Generated by Self Organizer*");

        return sb.ToString();
    }

    public string ExportToHtml(SummaryReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>Work Summary Report</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetReportStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"report-container\">");

        // Header
        sb.AppendLine("        <header class=\"report-header\">");
        sb.AppendLine("            <h1>Work Summary Report</h1>");
        sb.AppendLine($"            <p class=\"period\">Period: {report.StartDate:MMMM d, yyyy} - {report.EndDate:MMMM d, yyyy}</p>");
        sb.AppendLine($"            <p class=\"generated\">Generated: {report.GeneratedAt:MMMM d, yyyy h:mm tt}</p>");
        sb.AppendLine("        </header>");

        // Executive Summary Cards
        sb.AppendLine("        <section class=\"summary-section\">");
        sb.AppendLine("            <h2>Executive Summary</h2>");
        sb.AppendLine("            <div class=\"summary-cards\">");
        sb.AppendLine($"                <div class=\"summary-card\">");
        sb.AppendLine($"                    <div class=\"card-value\">{report.TotalTasksCompleted}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Tasks Completed</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"summary-card\">");
        sb.AppendLine($"                    <div class=\"card-value\">{report.ProjectsWorkedOn}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Projects</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"summary-card\">");
        sb.AppendLine($"                    <div class=\"card-value\">{FormatMinutes(report.TotalActualMinutes)}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Time Tracked</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"summary-card\">");
        sb.AppendLine($"                    <div class=\"card-value\">{report.TotalFocusSessions}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Focus Sessions</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"summary-card highlight\">");
        sb.AppendLine($"                    <div class=\"card-value\">{report.TotalHabitCompletions}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Habits Completed</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"summary-card highlight\">");
        sb.AppendLine($"                    <div class=\"card-value\">{report.TotalMeetings}</div>");
        sb.AppendLine($"                    <div class=\"card-label\">Meetings</div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </section>");

        // Goals Progress
        if (report.GoalProgress.Any())
        {
            sb.AppendLine("        <section class=\"goals-section\">");
            sb.AppendLine("            <h2>Goals Progress</h2>");
            sb.AppendLine("            <div class=\"goals-grid\">");
            foreach (var goal in report.GoalProgress)
            {
                var statusClass = goal.Status switch
                {
                    GoalStatus.Completed => "completed",
                    GoalStatus.Active => "active",
                    GoalStatus.OnHold => "onhold",
                    _ => ""
                };
                sb.AppendLine($"                <div class=\"goal-card {statusClass}\">");
                sb.AppendLine($"                    <div class=\"goal-header\">");
                sb.AppendLine($"                        <h3>{HtmlEncode(goal.Title)}</h3>");
                sb.AppendLine($"                        <span class=\"status-badge\">{goal.Status}</span>");
                sb.AppendLine($"                    </div>");
                sb.AppendLine($"                    <div class=\"progress-bar-container\">");
                sb.AppendLine($"                        <div class=\"progress-bar\" style=\"width: {goal.EndProgressPercent}%\"></div>");
                sb.AppendLine($"                        <span class=\"progress-text\">{goal.EndProgressPercent}%</span>");
                sb.AppendLine($"                    </div>");
                sb.AppendLine($"                    <div class=\"goal-meta\">");
                sb.AppendLine($"                        <span class=\"category\">{goal.Category}</span>");
                if (goal.TargetDate.HasValue)
                {
                    sb.AppendLine($"                        <span class=\"target\">Target: {goal.TargetDate.Value:MMM d, yyyy}</span>");
                }
                sb.AppendLine($"                    </div>");
                if (goal.LinkedTasksCompleted > 0 || goal.LinkedProjectsWorkedOn > 0)
                {
                    sb.AppendLine($"                    <div class=\"goal-stats\">");
                    if (goal.LinkedTasksCompleted > 0)
                        sb.AppendLine($"                        <span>{goal.LinkedTasksCompleted} related tasks completed</span>");
                    if (goal.LinkedProjectsWorkedOn > 0)
                        sb.AppendLine($"                        <span>{goal.LinkedProjectsWorkedOn} related projects</span>");
                    sb.AppendLine($"                    </div>");
                }
                sb.AppendLine($"                </div>");
            }
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        // Time Allocation
        if (report.TimeByContext.Any() || report.TimeByCategory.Any())
        {
            sb.AppendLine("        <section class=\"time-section\">");
            sb.AppendLine("            <h2>Time Allocation</h2>");
            sb.AppendLine("            <div class=\"time-charts\">");

            if (report.TimeByContext.Any())
            {
                sb.AppendLine("                <div class=\"chart-container\">");
                sb.AppendLine("                    <h3>By Context</h3>");
                sb.AppendLine("                    <div class=\"bar-chart\">");
                var maxContext = report.TimeByContext.Values.Max();
                foreach (var (context, minutes) in report.TimeByContext)
                {
                    var percentage = report.TotalActualMinutes > 0
                        ? (double)minutes / report.TotalActualMinutes * 100
                        : 0;
                    var barWidth = maxContext > 0 ? (double)minutes / maxContext * 100 : 0;
                    sb.AppendLine($"                        <div class=\"bar-item\">");
                    sb.AppendLine($"                            <span class=\"bar-label\">{HtmlEncode(context)}</span>");
                    sb.AppendLine($"                            <div class=\"bar-track\">");
                    sb.AppendLine($"                                <div class=\"bar-fill\" style=\"width: {barWidth:F0}%\"></div>");
                    sb.AppendLine($"                            </div>");
                    sb.AppendLine($"                            <span class=\"bar-value\">{FormatMinutes(minutes)} ({percentage:F0}%)</span>");
                    sb.AppendLine($"                        </div>");
                }
                sb.AppendLine("                    </div>");
                sb.AppendLine("                </div>");
            }

            if (report.TimeByCategory.Any())
            {
                sb.AppendLine("                <div class=\"chart-container\">");
                sb.AppendLine("                    <h3>By Category</h3>");
                sb.AppendLine("                    <div class=\"bar-chart\">");
                var maxCategory = report.TimeByCategory.Values.Max();
                foreach (var (category, minutes) in report.TimeByCategory)
                {
                    var percentage = report.TotalActualMinutes > 0
                        ? (double)minutes / report.TotalActualMinutes * 100
                        : 0;
                    var barWidth = maxCategory > 0 ? (double)minutes / maxCategory * 100 : 0;
                    sb.AppendLine($"                        <div class=\"bar-item\">");
                    sb.AppendLine($"                            <span class=\"bar-label\">{HtmlEncode(category)}</span>");
                    sb.AppendLine($"                            <div class=\"bar-track\">");
                    sb.AppendLine($"                                <div class=\"bar-fill category\" style=\"width: {barWidth:F0}%\"></div>");
                    sb.AppendLine($"                            </div>");
                    sb.AppendLine($"                            <span class=\"bar-value\">{FormatMinutes(minutes)} ({percentage:F0}%)</span>");
                    sb.AppendLine($"                        </div>");
                }
                sb.AppendLine("                    </div>");
                sb.AppendLine("                </div>");
            }

            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        // Projects
        if (report.Projects.Any())
        {
            sb.AppendLine("        <section class=\"projects-section\">");
            sb.AppendLine("            <h2>Projects</h2>");
            sb.AppendLine("            <div class=\"projects-grid\">");
            foreach (var project in report.Projects)
            {
                var completedClass = project.WasCompletedInPeriod ? "completed" : "";
                sb.AppendLine($"                <div class=\"project-card {completedClass}\">");
                sb.AppendLine($"                    <div class=\"project-header\">");
                sb.AppendLine($"                        <h3>{HtmlEncode(project.Name)}</h3>");
                if (project.WasCompletedInPeriod)
                {
                    sb.AppendLine($"                        <span class=\"completed-badge\">Completed</span>");
                }
                sb.AppendLine($"                    </div>");
                sb.AppendLine($"                    <div class=\"project-stats\">");
                sb.AppendLine($"                        <div class=\"stat\">");
                sb.AppendLine($"                            <span class=\"stat-value\">{project.TasksCompleted}</span>");
                sb.AppendLine($"                            <span class=\"stat-label\">Tasks Done</span>");
                sb.AppendLine($"                        </div>");
                sb.AppendLine($"                        <div class=\"stat\">");
                sb.AppendLine($"                            <span class=\"stat-value\">{project.TasksRemaining}</span>");
                sb.AppendLine($"                            <span class=\"stat-label\">Remaining</span>");
                sb.AppendLine($"                        </div>");
                sb.AppendLine($"                        <div class=\"stat\">");
                sb.AppendLine($"                            <span class=\"stat-value\">{FormatMinutes(project.TotalMinutesSpent)}</span>");
                sb.AppendLine($"                            <span class=\"stat-label\">Time</span>");
                sb.AppendLine($"                        </div>");
                sb.AppendLine($"                    </div>");
                if (!string.IsNullOrEmpty(project.Category))
                {
                    sb.AppendLine($"                    <div class=\"project-category\">{HtmlEncode(project.Category)}</div>");
                }
                sb.AppendLine($"                </div>");
            }
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        // Completed Tasks Table
        if (report.CompletedTasks.Any())
        {
            sb.AppendLine("        <section class=\"tasks-section\">");
            sb.AppendLine("            <h2>Completed Tasks</h2>");
            sb.AppendLine("            <table class=\"tasks-table\">");
            sb.AppendLine("                <thead>");
            sb.AppendLine("                    <tr>");
            sb.AppendLine("                        <th>Task</th>");
            sb.AppendLine("                        <th>Project</th>");
            sb.AppendLine("                        <th>Completed</th>");
            sb.AppendLine("                        <th>Time</th>");
            sb.AppendLine("                    </tr>");
            sb.AppendLine("                </thead>");
            sb.AppendLine("                <tbody>");
            foreach (var task in report.CompletedTasks.Take(50))
            {
                var priorityClass = task.Priority == 1 ? "high-priority" : "";
                var projectName = task.ProjectName ?? "-";
                var time = task.ActualMinutes.HasValue
                    ? FormatMinutes(task.ActualMinutes.Value)
                    : FormatMinutes(task.EstimatedMinutes);
                sb.AppendLine($"                    <tr class=\"{priorityClass}\">");
                sb.AppendLine($"                        <td>");
                sb.AppendLine($"                            {HtmlEncode(task.Title)}");
                if (task.RequiresDeepWork)
                    sb.AppendLine($"                            <span class=\"deep-work-badge\">Deep Work</span>");
                sb.AppendLine($"                        </td>");
                sb.AppendLine($"                        <td>{HtmlEncode(projectName)}</td>");
                sb.AppendLine($"                        <td>{task.CompletedAt:MMM d}</td>");
                sb.AppendLine($"                        <td>{time}</td>");
                sb.AppendLine($"                    </tr>");
            }
            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            if (report.CompletedTasks.Count > 50)
            {
                sb.AppendLine($"            <p class=\"more-items\">...and {report.CompletedTasks.Count - 50} more tasks</p>");
            }
            sb.AppendLine("        </section>");
        }

        // Ideas & Captures
        if (report.CaptureItems.Any())
        {
            sb.AppendLine("        <section class=\"captures-section\">");
            sb.AppendLine("            <h2>Ideas & Captures</h2>");
            sb.AppendLine("            <ul class=\"captures-list\">");
            foreach (var item in report.CaptureItems.Take(20))
            {
                var processedClass = item.IsProcessed ? "processed" : "";
                sb.AppendLine($"                <li class=\"{processedClass}\">");
                sb.AppendLine($"                    <span class=\"capture-text\">{HtmlEncode(item.Text)}</span>");
                sb.AppendLine($"                    <span class=\"capture-date\">{item.CapturedAt:MMM d}</span>");
                if (item.IsProcessed)
                    sb.AppendLine($"                    <span class=\"processed-badge\">Processed</span>");
                sb.AppendLine($"                </li>");
            }
            sb.AppendLine("            </ul>");
            if (report.CaptureItems.Count > 20)
            {
                sb.AppendLine($"            <p class=\"more-items\">...and {report.CaptureItems.Count - 20} more items</p>");
            }
            sb.AppendLine("        </section>");
        }

        // Top Tags
        if (report.TopTags.Any())
        {
            sb.AppendLine("        <section class=\"tags-section\">");
            sb.AppendLine("            <h2>Top Tags</h2>");
            sb.AppendLine("            <div class=\"tags-cloud\">");
            foreach (var tag in report.TopTags)
            {
                sb.AppendLine($"                <span class=\"tag\">#{HtmlEncode(tag)}</span>");
            }
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");
        }

        // Footer
        sb.AppendLine("        <footer class=\"report-footer\">");
        sb.AppendLine("            <p>Generated by Self Organizer</p>");
        sb.AppendLine("        </footer>");

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes < 60)
            return $"{minutes}m";
        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
    }

    private static string EscapeMarkdown(string text)
    {
        return text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
    }

    private static string HtmlEncode(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }

    private static string GetReportStyles()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background: #f5f5f5;
        }
        .report-container {
            max-width: 1000px;
            margin: 0 auto;
            padding: 40px 20px;
            background: white;
        }
        .report-header {
            text-align: center;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 2px solid #e0e0e0;
        }
        .report-header h1 {
            font-size: 2.5rem;
            color: #1a1a1a;
            margin-bottom: 10px;
        }
        .report-header .period {
            font-size: 1.2rem;
            color: #666;
        }
        .report-header .generated {
            font-size: 0.9rem;
            color: #999;
        }
        section {
            margin-bottom: 40px;
        }
        h2 {
            font-size: 1.5rem;
            color: #1a1a1a;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 1px solid #e0e0e0;
        }
        h3 {
            font-size: 1.1rem;
            color: #333;
            margin-bottom: 10px;
        }
        .summary-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
            gap: 16px;
        }
        .summary-card {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 12px;
            text-align: center;
        }
        .summary-card.highlight {
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }
        .card-value {
            font-size: 2rem;
            font-weight: 700;
        }
        .card-label {
            font-size: 0.85rem;
            opacity: 0.9;
            margin-top: 4px;
        }
        .goals-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 16px;
        }
        .goal-card {
            background: #f8f9fa;
            border-radius: 12px;
            padding: 20px;
            border-left: 4px solid #667eea;
        }
        .goal-card.completed {
            border-left-color: #28a745;
            background: #f0fff4;
        }
        .goal-card.onhold {
            border-left-color: #ffc107;
        }
        .goal-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 12px;
        }
        .status-badge {
            font-size: 0.75rem;
            padding: 4px 8px;
            border-radius: 4px;
            background: #e9ecef;
            color: #495057;
        }
        .progress-bar-container {
            position: relative;
            background: #e9ecef;
            border-radius: 8px;
            height: 24px;
            margin-bottom: 12px;
            overflow: hidden;
        }
        .progress-bar {
            height: 100%;
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            border-radius: 8px;
            transition: width 0.3s ease;
        }
        .progress-text {
            position: absolute;
            right: 8px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 0.85rem;
            font-weight: 600;
            color: #333;
        }
        .goal-meta {
            display: flex;
            justify-content: space-between;
            font-size: 0.85rem;
            color: #666;
        }
        .goal-stats {
            margin-top: 12px;
            padding-top: 12px;
            border-top: 1px solid #e0e0e0;
            font-size: 0.85rem;
            color: #666;
        }
        .goal-stats span {
            display: block;
            margin-bottom: 4px;
        }
        .time-charts {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 24px;
        }
        .chart-container {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 12px;
        }
        .bar-chart {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }
        .bar-item {
            display: grid;
            grid-template-columns: 100px 1fr 80px;
            gap: 12px;
            align-items: center;
        }
        .bar-label {
            font-size: 0.85rem;
            color: #333;
            text-align: right;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        .bar-track {
            height: 20px;
            background: #e9ecef;
            border-radius: 4px;
            overflow: hidden;
        }
        .bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            border-radius: 4px;
        }
        .bar-fill.category {
            background: linear-gradient(90deg, #11998e 0%, #38ef7d 100%);
        }
        .bar-value {
            font-size: 0.8rem;
            color: #666;
        }
        .projects-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 16px;
        }
        .project-card {
            background: #f8f9fa;
            border-radius: 12px;
            padding: 20px;
        }
        .project-card.completed {
            background: #f0fff4;
            border: 1px solid #28a745;
        }
        .project-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 16px;
        }
        .completed-badge {
            font-size: 0.75rem;
            padding: 4px 8px;
            border-radius: 4px;
            background: #28a745;
            color: white;
        }
        .project-stats {
            display: flex;
            justify-content: space-between;
            text-align: center;
        }
        .stat-value {
            display: block;
            font-size: 1.5rem;
            font-weight: 700;
            color: #333;
        }
        .stat-label {
            font-size: 0.75rem;
            color: #666;
        }
        .project-category {
            margin-top: 12px;
            font-size: 0.85rem;
            color: #666;
        }
        .tasks-table {
            width: 100%;
            border-collapse: collapse;
        }
        .tasks-table th,
        .tasks-table td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #e0e0e0;
        }
        .tasks-table th {
            background: #f8f9fa;
            font-weight: 600;
            color: #333;
        }
        .tasks-table tr.high-priority td:first-child {
            border-left: 3px solid #dc3545;
        }
        .deep-work-badge {
            display: inline-block;
            font-size: 0.7rem;
            padding: 2px 6px;
            border-radius: 4px;
            background: #667eea;
            color: white;
            margin-left: 8px;
        }
        .captures-list {
            list-style: none;
        }
        .captures-list li {
            padding: 12px;
            background: #f8f9fa;
            border-radius: 8px;
            margin-bottom: 8px;
            display: flex;
            align-items: center;
            gap: 12px;
        }
        .captures-list li.processed {
            opacity: 0.7;
        }
        .capture-text {
            flex: 1;
        }
        .capture-date {
            font-size: 0.85rem;
            color: #666;
        }
        .processed-badge {
            font-size: 0.7rem;
            padding: 2px 6px;
            border-radius: 4px;
            background: #28a745;
            color: white;
        }
        .tags-cloud {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
        }
        .tag {
            display: inline-block;
            padding: 6px 12px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-radius: 20px;
            font-size: 0.9rem;
        }
        .more-items {
            margin-top: 16px;
            font-style: italic;
            color: #666;
        }
        .report-footer {
            text-align: center;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #e0e0e0;
            color: #999;
            font-size: 0.9rem;
        }
        @media print {
            body {
                background: white;
            }
            .report-container {
                padding: 0;
            }
            .summary-card {
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }
        }
        @media (max-width: 600px) {
            .bar-item {
                grid-template-columns: 80px 1fr;
            }
            .bar-value {
                display: none;
            }
        }
        ";
    }
}
