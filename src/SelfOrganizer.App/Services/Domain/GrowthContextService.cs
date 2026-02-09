using System.Net;
using System.Text;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class GrowthContextService : IGrowthContextService
{
    private readonly IRepository<GrowthSnapshot> _snapshotRepo;
    private readonly IRepository<Skill> _skillRepo;
    private readonly IGoalService _goalService;
    private readonly IRepository<TodoTask> _taskRepo;
    private readonly IRepository<HabitLog> _habitLogRepo;
    private readonly IRepository<Habit> _habitRepo;
    private readonly IRepository<FocusSessionLog> _focusRepo;
    private readonly IRepository<WeeklySnapshot> _weeklyRepo;
    private readonly IRepository<CareerPlan> _careerPlanRepo;
    private readonly IProjectService _projectService;
    private readonly IUserPreferencesProvider _prefsProvider;

    public GrowthContextService(
        IRepository<GrowthSnapshot> snapshotRepo,
        IRepository<Skill> skillRepo,
        IGoalService goalService,
        IRepository<TodoTask> taskRepo,
        IRepository<HabitLog> habitLogRepo,
        IRepository<Habit> habitRepo,
        IRepository<FocusSessionLog> focusRepo,
        IRepository<WeeklySnapshot> weeklyRepo,
        IRepository<CareerPlan> careerPlanRepo,
        IProjectService projectService,
        IUserPreferencesProvider prefsProvider)
    {
        _snapshotRepo = snapshotRepo;
        _skillRepo = skillRepo;
        _goalService = goalService;
        _taskRepo = taskRepo;
        _habitLogRepo = habitLogRepo;
        _habitRepo = habitRepo;
        _focusRepo = focusRepo;
        _weeklyRepo = weeklyRepo;
        _careerPlanRepo = careerPlanRepo;
        _projectService = projectService;
        _prefsProvider = prefsProvider;
    }

    public async Task<GrowthSnapshot> CaptureSnapshotAsync(SnapshotTrigger trigger = SnapshotTrigger.Manual, string? notes = null)
    {
        var skills = (await _skillRepo.GetAllAsync()).Where(s => s.IsActive).ToList();
        var goals = (await _goalService.GetAllAsync()).Where(g => g.Status != GoalStatus.Archived).ToList();
        var prefs = await _prefsProvider.GetPreferencesAsync();

        var snapshot = new GrowthSnapshot
        {
            SnapshotDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Trigger = trigger,
            Notes = notes,
            AppModeAtCapture = prefs.AppMode
        };

        foreach (var skill in skills)
        {
            snapshot.SkillProficiencies[skill.Id] = skill.CurrentProficiency;
            snapshot.SkillNames[skill.Id] = skill.Name;
        }

        foreach (var goal in goals)
        {
            snapshot.GoalProgress[goal.Id] = goal.ProgressPercent;
            snapshot.GoalTitles[goal.Id] = goal.Title;
        }

        if (prefs.LifeAreaRatings != null)
        {
            snapshot.BalanceRatings = new Dictionary<string, int>(prefs.LifeAreaRatings);
        }

        return await _snapshotRepo.AddAsync(snapshot);
    }

    public async Task<IEnumerable<GrowthSnapshot>> GetSnapshotsInRangeAsync(DateOnly start, DateOnly end)
    {
        var all = await _snapshotRepo.GetAllAsync();
        return all.Where(s => s.SnapshotDate >= start && s.SnapshotDate <= end)
            .OrderBy(s => s.SnapshotDate);
    }

    public async Task<GrowthSnapshot?> GetLatestSnapshotAsync()
    {
        var all = await _snapshotRepo.GetAllAsync();
        return all.OrderByDescending(s => s.SnapshotDate).FirstOrDefault();
    }

    public async Task DeleteSnapshotAsync(Guid id)
    {
        await _snapshotRepo.DeleteAsync(id);
    }

    public async Task<GrowthContextSummary> GetContextForPeriodAsync(DateOnly periodStart, DateOnly periodEnd)
    {
        var periodStartDt = periodStart.ToDateTime(TimeOnly.MinValue);
        var periodEndDt = periodEnd.ToDateTime(TimeOnly.MaxValue);

        // Load all data in parallel
        var snapshotsTask = GetSnapshotsInRangeAsync(periodStart, periodEnd);
        var tasksTask = _taskRepo.GetAllAsync();
        var habitLogsTask = _habitLogRepo.GetAllAsync();
        var habitsTask = _habitRepo.GetAllAsync();
        var focusTask = _focusRepo.GetAllAsync();
        var weeklyTask = _weeklyRepo.GetAllAsync();
        var plansTask = _careerPlanRepo.GetAllAsync();
        var projectsTask = _projectService.GetAllAsync();

        await Task.WhenAll(snapshotsTask, tasksTask, habitLogsTask, habitsTask, focusTask, weeklyTask, plansTask, projectsTask);

        var snapshots = snapshotsTask.Result.ToList();
        var closestSnapshot = snapshots.LastOrDefault(); // latest in range

        // Find previous snapshot for delta computation
        GrowthSnapshot? previousSnapshot = null;
        if (closestSnapshot != null)
        {
            var allSnapshots = await _snapshotRepo.GetAllAsync();
            previousSnapshot = allSnapshots
                .Where(s => s.SnapshotDate < closestSnapshot.SnapshotDate)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefault();
        }

        var summary = new GrowthContextSummary
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            HasSnapshot = closestSnapshot != null,
            SnapshotId = closestSnapshot?.Id,
            SnapshotNotes = closestSnapshot?.Notes
        };

        // Skills from snapshot
        if (closestSnapshot != null)
        {
            foreach (var (skillId, proficiency) in closestSnapshot.SkillProficiencies)
            {
                var name = closestSnapshot.SkillNames.GetValueOrDefault(skillId, "Unknown Skill");
                int? previous = previousSnapshot?.SkillProficiencies.GetValueOrDefault(skillId);
                summary.Skills.Add(new SkillDataPoint
                {
                    SkillId = skillId,
                    Name = name,
                    Proficiency = proficiency,
                    PreviousProficiency = previous == 0 ? null : previous
                });
            }

            foreach (var (goalId, progress) in closestSnapshot.GoalProgress)
            {
                var title = closestSnapshot.GoalTitles.GetValueOrDefault(goalId, "Unknown Goal");
                int? previous = previousSnapshot?.GoalProgress.GetValueOrDefault(goalId);
                summary.Goals.Add(new GoalDataPoint
                {
                    GoalId = goalId,
                    Title = title,
                    ProgressPercent = progress,
                    PreviousProgressPercent = previous == 0 ? null : previous
                });
            }

            summary.BalanceRatings = new Dictionary<string, int>(closestSnapshot.BalanceRatings);
        }

        // Tasks completed in period
        var completedTasks = tasksTask.Result
            .Where(t => t.CompletedAt.HasValue
                && t.CompletedAt.Value >= periodStartDt
                && t.CompletedAt.Value <= periodEndDt)
            .ToList();
        summary.TasksCompleted = completedTasks.Count;
        summary.TasksByContext = completedTasks
            .SelectMany(t => t.Contexts)
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        // Habits in period
        var periodLogs = habitLogsTask.Result
            .Where(l => l.Date >= periodStart && l.Date <= periodEnd)
            .ToList();
        summary.HabitCompletionCount = periodLogs.Count(l => l.Completed);
        var totalLogs = periodLogs.Count;
        summary.HabitCompletionRate = totalLogs > 0 ? (double)summary.HabitCompletionCount / totalLogs : 0;

        // Focus sessions in period
        var periodFocus = focusTask.Result
            .Where(f => f.StartedAt >= periodStartDt && f.StartedAt <= periodEndDt)
            .ToList();
        summary.FocusSessionCount = periodFocus.Count;
        summary.FocusMinutes = periodFocus.Sum(f => f.DurationMinutes);

        // Weekly snapshots in period (for energy/mood/balance)
        var periodWeekly = weeklyTask.Result
            .Where(w => w.WeekStart >= periodStart && w.WeekStart <= periodEnd && w.ReviewCompleted)
            .ToList();
        if (periodWeekly.Count > 0)
        {
            var energyValues = periodWeekly.Where(w => w.AverageEnergyLevel.HasValue).Select(w => (double)w.AverageEnergyLevel!.Value).ToList();
            var moodValues = periodWeekly.Where(w => w.AverageMoodLevel.HasValue).Select(w => (double)w.AverageMoodLevel!.Value).ToList();
            var wlbValues = periodWeekly.Where(w => w.WorkLifeBalanceRating.HasValue).Select(w => (double)w.WorkLifeBalanceRating!.Value).ToList();

            summary.AverageEnergy = energyValues.Count > 0 ? Math.Round(energyValues.Average(), 1) : null;
            summary.AverageMood = moodValues.Count > 0 ? Math.Round(moodValues.Average(), 1) : null;
            summary.AverageWorkLifeBalance = wlbValues.Count > 0 ? Math.Round(wlbValues.Average(), 1) : null;
        }

        // Milestones completed in period
        var allPlans = plansTask.Result.ToList();
        var completedMilestones = allPlans
            .SelectMany(p => p.Milestones)
            .Where(m => m.CompletedDate.HasValue
                && m.CompletedDate.Value >= periodStartDt
                && m.CompletedDate.Value <= periodEndDt)
            .ToList();
        summary.MilestonesCompletedInPeriod = completedMilestones.Count;
        summary.MilestonesCompletedNames = completedMilestones.Select(m => m.Title).ToList();

        // Active projects
        var activeProjects = projectsTask.Result
            .Where(p => p.Status == ProjectStatus.Active)
            .ToList();
        summary.ActiveProjectNames = activeProjects.Select(p => p.Name).ToList();

        return summary;
    }

    public async Task<List<GrowthContextSummary>> GetJourneyAsync(DateOnly start, DateOnly end, bool monthly = true)
    {
        var periods = new List<(DateOnly Start, DateOnly End)>();

        var current = new DateOnly(start.Year, start.Month, 1);
        while (current < end)
        {
            var periodEnd = monthly
                ? current.AddMonths(1).AddDays(-1)
                : current.AddMonths(3).AddDays(-1);

            if (periodEnd > end)
                periodEnd = end;

            periods.Add((current, periodEnd));
            current = monthly ? current.AddMonths(1) : current.AddMonths(3);
        }

        // Load all data once
        var periodStartDt = start.ToDateTime(TimeOnly.MinValue);
        var periodEndDt = end.ToDateTime(TimeOnly.MaxValue);

        var allSnapshots = (await _snapshotRepo.GetAllAsync()).OrderBy(s => s.SnapshotDate).ToList();
        var allTasks = (await _taskRepo.GetAllAsync()).ToList();
        var allHabitLogs = (await _habitLogRepo.GetAllAsync()).ToList();
        var allFocus = (await _focusRepo.GetAllAsync()).ToList();
        var allWeekly = (await _weeklyRepo.GetAllAsync()).ToList();
        var allPlans = (await _careerPlanRepo.GetAllAsync()).ToList();
        var allProjects = (await _projectService.GetAllAsync()).ToList();

        var summaries = new List<GrowthContextSummary>();

        foreach (var (pStart, pEnd) in periods)
        {
            var pStartDt = pStart.ToDateTime(TimeOnly.MinValue);
            var pEndDt = pEnd.ToDateTime(TimeOnly.MaxValue);

            var periodSnapshots = allSnapshots.Where(s => s.SnapshotDate >= pStart && s.SnapshotDate <= pEnd).ToList();
            var closestSnapshot = periodSnapshots.LastOrDefault();

            GrowthSnapshot? previousSnapshot = null;
            if (closestSnapshot != null)
            {
                previousSnapshot = allSnapshots
                    .Where(s => s.SnapshotDate < closestSnapshot.SnapshotDate)
                    .OrderByDescending(s => s.SnapshotDate)
                    .FirstOrDefault();
            }

            var summary = new GrowthContextSummary
            {
                PeriodStart = pStart,
                PeriodEnd = pEnd,
                HasSnapshot = closestSnapshot != null,
                SnapshotId = closestSnapshot?.Id,
                SnapshotNotes = closestSnapshot?.Notes
            };

            // Skills from snapshot
            if (closestSnapshot != null)
            {
                foreach (var (skillId, proficiency) in closestSnapshot.SkillProficiencies)
                {
                    var name = closestSnapshot.SkillNames.GetValueOrDefault(skillId, "Unknown Skill");
                    int? previous = previousSnapshot?.SkillProficiencies.GetValueOrDefault(skillId);
                    summary.Skills.Add(new SkillDataPoint
                    {
                        SkillId = skillId,
                        Name = name,
                        Proficiency = proficiency,
                        PreviousProficiency = previous == 0 ? null : previous
                    });
                }

                foreach (var (goalId, progress) in closestSnapshot.GoalProgress)
                {
                    var title = closestSnapshot.GoalTitles.GetValueOrDefault(goalId, "Unknown Goal");
                    int? previous = previousSnapshot?.GoalProgress.GetValueOrDefault(goalId);
                    summary.Goals.Add(new GoalDataPoint
                    {
                        GoalId = goalId,
                        Title = title,
                        ProgressPercent = progress,
                        PreviousProgressPercent = previous == 0 ? null : previous
                    });
                }

                summary.BalanceRatings = new Dictionary<string, int>(closestSnapshot.BalanceRatings);
            }

            // Tasks
            var completedTasks = allTasks
                .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= pStartDt && t.CompletedAt.Value <= pEndDt)
                .ToList();
            summary.TasksCompleted = completedTasks.Count;
            summary.TasksByContext = completedTasks
                .SelectMany(t => t.Contexts)
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            // Habits
            var periodLogs = allHabitLogs.Where(l => l.Date >= pStart && l.Date <= pEnd).ToList();
            summary.HabitCompletionCount = periodLogs.Count(l => l.Completed);
            var totalLogs = periodLogs.Count;
            summary.HabitCompletionRate = totalLogs > 0 ? (double)summary.HabitCompletionCount / totalLogs : 0;

            // Focus
            var periodFocus = allFocus.Where(f => f.StartedAt >= pStartDt && f.StartedAt <= pEndDt).ToList();
            summary.FocusSessionCount = periodFocus.Count;
            summary.FocusMinutes = periodFocus.Sum(f => f.DurationMinutes);

            // Weekly snapshots
            var periodWeekly = allWeekly.Where(w => w.WeekStart >= pStart && w.WeekStart <= pEnd && w.ReviewCompleted).ToList();
            if (periodWeekly.Count > 0)
            {
                var energyValues = periodWeekly.Where(w => w.AverageEnergyLevel.HasValue).Select(w => (double)w.AverageEnergyLevel!.Value).ToList();
                var moodValues = periodWeekly.Where(w => w.AverageMoodLevel.HasValue).Select(w => (double)w.AverageMoodLevel!.Value).ToList();
                var wlbValues = periodWeekly.Where(w => w.WorkLifeBalanceRating.HasValue).Select(w => (double)w.WorkLifeBalanceRating!.Value).ToList();

                summary.AverageEnergy = energyValues.Count > 0 ? Math.Round(energyValues.Average(), 1) : null;
                summary.AverageMood = moodValues.Count > 0 ? Math.Round(moodValues.Average(), 1) : null;
                summary.AverageWorkLifeBalance = wlbValues.Count > 0 ? Math.Round(wlbValues.Average(), 1) : null;
            }

            // Milestones
            var completedMilestones = allPlans
                .SelectMany(p => p.Milestones)
                .Where(m => m.CompletedDate.HasValue && m.CompletedDate.Value >= pStartDt && m.CompletedDate.Value <= pEndDt)
                .ToList();
            summary.MilestonesCompletedInPeriod = completedMilestones.Count;
            summary.MilestonesCompletedNames = completedMilestones.Select(m => m.Title).ToList();

            // Active projects
            summary.ActiveProjectNames = allProjects.Where(p => p.Status == ProjectStatus.Active).Select(p => p.Name).ToList();

            summaries.Add(summary);
        }

        return summaries;
    }

    public async Task AutoSnapshotIfDueAsync()
    {
        var latest = await GetLatestSnapshotAsync();
        if (latest == null || latest.SnapshotDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)))
        {
            await CaptureSnapshotAsync(SnapshotTrigger.Auto);
        }
    }

    public async Task<string> ExportCareerPlanHtmlAsync(Guid? careerPlanId = null)
    {
        // Load all data in parallel
        var plansTask = _careerPlanRepo.GetAllAsync();
        var skillsTask = _skillRepo.GetAllAsync();
        var goalsTask = _goalService.GetAllAsync();
        var projectsTask = _projectService.GetAllAsync();
        var habitsTask = _habitRepo.GetAllAsync();
        var habitLogsTask = _habitLogRepo.GetAllAsync();
        var focusTask = _focusRepo.GetAllAsync();
        var weeklyTask = _weeklyRepo.GetAllAsync();
        var snapshotsTask = _snapshotRepo.GetAllAsync();
        var tasksTask = _taskRepo.GetAllAsync();

        await Task.WhenAll(plansTask, skillsTask, goalsTask, projectsTask, habitsTask,
            habitLogsTask, focusTask, weeklyTask, snapshotsTask, tasksTask);

        var allPlans = plansTask.Result.ToList();
        var allSkills = skillsTask.Result.ToList();
        var allGoals = goalsTask.Result.ToList();
        var allProjects = projectsTask.Result.ToList();
        var allHabits = habitsTask.Result.ToList();
        var allHabitLogs = habitLogsTask.Result.ToList();
        var allFocus = focusTask.Result.ToList();
        var allWeekly = weeklyTask.Result.ToList();
        var allSnapshots = snapshotsTask.Result.OrderBy(s => s.SnapshotDate).ToList();
        var allTasks = tasksTask.Result.ToList();

        // Filter plans
        List<CareerPlan> plans;
        if (careerPlanId.HasValue)
        {
            var plan = allPlans.FirstOrDefault(p => p.Id == careerPlanId.Value);
            plans = plan != null ? new List<CareerPlan> { plan } : new List<CareerPlan>();
        }
        else
        {
            plans = allPlans.Where(p => p.Status == CareerPlanStatus.Active || p.Status == CareerPlanStatus.Draft).ToList();
            if (plans.Count == 0) plans = allPlans;
        }

        // Build lookup dictionaries
        var skillDict = allSkills.ToDictionary(s => s.Id);
        var goalDict = allGoals.ToDictionary(g => g.Id);
        var projectDict = allProjects.ToDictionary(p => p.Id);
        var habitDict = allHabits.ToDictionary(h => h.Id);

        return BuildCareerNarrativeHtml(plans, skillDict, goalDict, projectDict, habitDict,
            allHabitLogs, allFocus, allWeekly, allSnapshots, allTasks);
    }

    public Task<string> ExportCareerPlanHtmlAsync(CareerExportData data)
    {
        var plans = data.Plans.Where(p => p.Status == CareerPlanStatus.Active || p.Status == CareerPlanStatus.Draft).ToList();
        if (plans.Count == 0) plans = data.Plans;

        var html = BuildCareerNarrativeHtml(
            plans,
            data.Skills.ToDictionary(s => s.Id),
            data.Goals.ToDictionary(g => g.Id),
            data.Projects.ToDictionary(p => p.Id),
            data.Habits.ToDictionary(h => h.Id),
            data.HabitLogs,
            data.FocusSessions,
            data.WeeklySnapshots,
            data.GrowthSnapshots.OrderBy(s => s.SnapshotDate).ToList(),
            data.Tasks);

        return Task.FromResult(html);
    }

    private string BuildCareerNarrativeHtml(
        List<CareerPlan> plans,
        Dictionary<Guid, Skill> skillDict,
        Dictionary<Guid, Goal> goalDict,
        Dictionary<Guid, Project> projectDict,
        Dictionary<Guid, Habit> habitDict,
        List<HabitLog> allHabitLogs,
        List<FocusSessionLog> allFocus,
        List<WeeklySnapshot> allWeekly,
        List<GrowthSnapshot> allSnapshots,
        List<TodoTask> allTasks)
    {
        var sb = new StringBuilder();
        var now = DateTime.UtcNow;
        var E = (string? s) => WebUtility.HtmlEncode(s ?? "");

        // Use the first/primary plan for hero content
        var primaryPlan = plans.FirstOrDefault();

        // Aggregate all linked entity IDs across all plans
        var linkedSkillIds = plans.SelectMany(p => p.LinkedSkillIds).Distinct().ToHashSet();
        var linkedGoalIds = plans.SelectMany(p => p.LinkedGoalIds).Distinct().ToHashSet();
        var linkedProjectIds = plans.SelectMany(p => p.LinkedProjectIds).Distinct().ToHashSet();
        var linkedHabitIds = plans.SelectMany(p => p.LinkedHabitIds).Distinct().ToHashSet();
        var allMilestones = plans.SelectMany(p => p.Milestones).OrderBy(m => m.SortOrder).ToList();

        // Resolve linked entities
        var linkedSkills = linkedSkillIds.Select(id => skillDict.GetValueOrDefault(id)).Where(s => s != null).ToList()!;
        var linkedGoals = linkedGoalIds.Select(id => goalDict.GetValueOrDefault(id)).Where(g => g != null).ToList()!;
        var linkedProjects = linkedProjectIds.Select(id => projectDict.GetValueOrDefault(id)).Where(p => p != null).ToList()!;
        var linkedHabits = linkedHabitIds.Select(id => habitDict.GetValueOrDefault(id)).Where(h => h != null).ToList()!;

        // Compute aggregate stats
        var completedMilestones = allMilestones.Where(m => m.Status == MilestoneStatus.Completed).ToList();
        var activeMilestones = allMilestones.Where(m => m.Status == MilestoneStatus.InProgress).ToList();
        var upcomingMilestones = allMilestones.Where(m => m.Status == MilestoneStatus.NotStarted).ToList();
        var totalFocusMinutes = allFocus.Sum(f => f.DurationMinutes);
        var totalFocusHours = Math.Round(totalFocusMinutes / 60.0, 1);

        // Habit completion rate
        var linkedHabitLogCount = allHabitLogs.Count(l => linkedHabitIds.Contains(l.HabitId));
        var linkedHabitCompletions = allHabitLogs.Count(l => linkedHabitIds.Contains(l.HabitId) && l.Completed);
        var habitCompletionRate = linkedHabitLogCount > 0 ? Math.Round((double)linkedHabitCompletions / linkedHabitLogCount * 100) : 0;

        // Completed tasks
        var completedTasks = allTasks.Where(t => t.CompletedAt.HasValue).ToList();

        // Weekly reviews
        var completedWeeklies = allWeekly.Where(w => w.ReviewCompleted).OrderBy(w => w.WeekStart).ToList();

        // ── HTML Document ──
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{E(primaryPlan?.Title ?? "Career Development Narrative")}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetNarrativeStyles());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"document\">");

        // ── I. Cover / Hero ──
        sb.AppendLine("<header class=\"hero\">");
        sb.AppendLine("  <div class=\"hero-content\">");
        sb.AppendLine("    <p class=\"hero-eyebrow\">Career Development Narrative</p>");
        sb.AppendLine($"    <h1 class=\"hero-title\">{E(primaryPlan?.Title ?? "My Career Plan")}</h1>");
        if (primaryPlan != null && (!string.IsNullOrEmpty(primaryPlan.CurrentRole) || !string.IsNullOrEmpty(primaryPlan.TargetRole)))
        {
            sb.AppendLine("    <div class=\"hero-roles\">");
            if (!string.IsNullOrEmpty(primaryPlan.CurrentRole))
                sb.AppendLine($"      <span class=\"role-current\">{E(primaryPlan.CurrentRole)}</span>");
            if (!string.IsNullOrEmpty(primaryPlan.CurrentRole) && !string.IsNullOrEmpty(primaryPlan.TargetRole))
                sb.AppendLine("      <span class=\"role-arrow\">&rarr;</span>");
            if (!string.IsNullOrEmpty(primaryPlan.TargetRole))
                sb.AppendLine($"      <span class=\"role-target\">{E(primaryPlan.TargetRole)}</span>");
            sb.AppendLine("    </div>");
        }
        if (primaryPlan != null)
        {
            sb.AppendLine($"    <div class=\"hero-progress\">");
            sb.AppendLine($"      <div class=\"progress-bar\"><div class=\"progress-fill\" style=\"width:{primaryPlan.ProgressPercent}%\"></div></div>");
            sb.AppendLine($"      <span class=\"progress-label\">{primaryPlan.ProgressPercent}% Complete</span>");
            sb.AppendLine("    </div>");
        }
        if (primaryPlan?.StartDate != null || primaryPlan?.TargetDate != null)
        {
            var dateRange = "";
            if (primaryPlan.StartDate != null) dateRange += primaryPlan.StartDate.Value.ToString("MMMM yyyy");
            if (primaryPlan.StartDate != null && primaryPlan.TargetDate != null) dateRange += " \u2013 ";
            if (primaryPlan.TargetDate != null) dateRange += primaryPlan.TargetDate.Value.ToString("MMMM yyyy");
            sb.AppendLine($"    <p class=\"hero-dates\">{E(dateRange)}</p>");
        }
        sb.AppendLine($"    <p class=\"hero-generated\">Generated {now:MMMM d, yyyy}</p>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</header>");

        // ── II. Executive Summary ──
        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("  <h2 class=\"section-title\">Executive Summary</h2>");
        sb.AppendLine("  <div class=\"metrics-grid\">");
        AppendMetricCard(sb, linkedSkills.Count.ToString(), "Skills in Development", "skill");
        AppendMetricCard(sb, linkedGoals.Count(g => g!.Status == GoalStatus.Active).ToString(), "Active Goals", "goal");
        AppendMetricCard(sb, completedMilestones.Count + " / " + allMilestones.Count(m => m.Status != MilestoneStatus.Skipped), "Milestones", "milestone");
        AppendMetricCard(sb, linkedProjects.Count(p => p!.Status == ProjectStatus.Active).ToString(), "Active Projects", "project");
        AppendMetricCard(sb, totalFocusHours.ToString("0.#") + "h", "Focus Time", "focus");
        AppendMetricCard(sb, habitCompletionRate + "%", "Habit Rate", "habit");
        sb.AppendLine("  </div>");

        // Narrative opening
        if (primaryPlan?.Description != null)
        {
            sb.AppendLine($"  <div class=\"narrative-block\">");
            sb.AppendLine($"    <p>{E(primaryPlan.Description)}</p>");
            sb.AppendLine("  </div>");
        }
        sb.AppendLine("</section>");

        // ── III. The Roadmap (Milestones) ──
        if (allMilestones.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">The Roadmap</h2>");
            sb.AppendLine("  <div class=\"timeline\">");
            foreach (var ms in allMilestones.Where(m => m.Status != MilestoneStatus.Skipped))
            {
                var statusClass = ms.Status switch
                {
                    MilestoneStatus.Completed => "completed",
                    MilestoneStatus.InProgress => "active",
                    _ => "upcoming"
                };
                var categoryLabel = ms.Category.ToString();
                var dateLabel = ms.Status == MilestoneStatus.Completed && ms.CompletedDate.HasValue
                    ? ms.CompletedDate.Value.ToString("MMM yyyy")
                    : ms.TargetDate.HasValue ? ms.TargetDate.Value.ToString("MMM yyyy") : "";

                sb.AppendLine($"    <div class=\"timeline-item {statusClass}\">");
                sb.AppendLine($"      <div class=\"timeline-marker\"></div>");
                sb.AppendLine($"      <div class=\"timeline-content\">");
                sb.AppendLine($"        <div class=\"timeline-header\">");
                sb.AppendLine($"          <h4>{E(ms.Title)}</h4>");
                sb.AppendLine($"          <span class=\"badge badge-{statusClass}\">{categoryLabel}</span>");
                sb.AppendLine($"        </div>");
                if (!string.IsNullOrEmpty(dateLabel))
                    sb.AppendLine($"        <p class=\"timeline-date\">{E(dateLabel)}</p>");
                if (!string.IsNullOrEmpty(ms.Description))
                    sb.AppendLine($"        <p class=\"timeline-desc\">{E(ms.Description)}</p>");

                // Linked skills for this milestone
                var msSkills = ms.LinkedSkillIds
                    .Select(id => skillDict.GetValueOrDefault(id))
                    .Where(s => s != null)
                    .ToList();
                if (msSkills.Count > 0)
                {
                    sb.AppendLine("        <div class=\"timeline-skills\">");
                    foreach (var s in msSkills)
                        sb.AppendLine($"          <span class=\"skill-chip\">{E(s!.Name)}</span>");
                    sb.AppendLine("        </div>");
                }
                sb.AppendLine($"      </div>");
                sb.AppendLine($"    </div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // ── IV. Skills Landscape ──
        if (linkedSkills.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">Skills Landscape</h2>");

            var grouped = linkedSkills.GroupBy(s => s!.Category).OrderBy(g => g.Key);
            foreach (var group in grouped)
            {
                var catName = group.Key switch
                {
                    SkillCategory.Technical => "Technical",
                    SkillCategory.SoftSkills => "Soft Skills",
                    SkillCategory.Creative => "Creative",
                    SkillCategory.DomainKnowledge => "Domain Knowledge",
                    SkillCategory.ToolsSoftware => "Tools & Software",
                    _ => group.Key.ToString()
                };
                sb.AppendLine($"  <h3 class=\"subsection-title\">{E(catName)}</h3>");
                sb.AppendLine("  <div class=\"skills-grid\">");
                foreach (var skill in group.OrderByDescending(s => s!.CurrentProficiency))
                {
                    var s = skill!;
                    var pctCurrent = s.TargetProficiency > 0 ? (int)Math.Round((double)s.CurrentProficiency / s.TargetProficiency * 100) : 0;
                    if (pctCurrent > 100) pctCurrent = 100;

                    // Find delta from snapshots
                    var earliestSnap = allSnapshots.FirstOrDefault(sn => sn.SkillProficiencies.ContainsKey(s.Id));
                    var earliestProf = earliestSnap?.SkillProficiencies.GetValueOrDefault(s.Id) ?? s.CurrentProficiency;
                    var delta = s.CurrentProficiency - earliestProf;
                    var deltaStr = delta > 0 ? $"+{delta}" : delta == 0 ? "\u2013" : delta.ToString();

                    sb.AppendLine("    <div class=\"skill-card\">");
                    sb.AppendLine($"      <div class=\"skill-header\">");
                    sb.AppendLine($"        <span class=\"skill-name\">{E(s.Name)}</span>");
                    sb.AppendLine($"        <span class=\"skill-level\">{Skill.GetProficiencyName(s.CurrentProficiency)}</span>");
                    sb.AppendLine($"      </div>");
                    sb.AppendLine($"      <div class=\"skill-bar\">");
                    sb.AppendLine($"        <div class=\"skill-bar-fill\" style=\"width:{pctCurrent}%;background:{E(s.Color ?? "#c8a96e")}\"></div>");
                    sb.AppendLine($"      </div>");
                    sb.AppendLine($"      <div class=\"skill-meta\">");
                    sb.AppendLine($"        <span>{s.CurrentProficiency}/{s.TargetProficiency}</span>");
                    sb.AppendLine($"        <span class=\"skill-delta delta-{(delta > 0 ? "up" : delta < 0 ? "down" : "flat")}\">{deltaStr}</span>");
                    sb.AppendLine($"      </div>");
                    sb.AppendLine("    </div>");
                }
                sb.AppendLine("  </div>");
            }

            // Strengths & Growth Opportunities
            var strengths = linkedSkills.Where(s => s!.CurrentProficiency >= 3 || s.CurrentProficiency >= s.TargetProficiency).ToList();
            var opportunities = linkedSkills.Where(s => s!.TargetProficiency - s.CurrentProficiency >= 2).OrderByDescending(s => s!.TargetProficiency - s!.CurrentProficiency).ToList();

            if (strengths.Count > 0)
            {
                sb.AppendLine("  <div class=\"callout callout-strength\">");
                sb.AppendLine("    <h4>Strengths</h4>");
                sb.AppendLine($"    <p>{string.Join(", ", strengths.Select(s => E(s!.Name)))}</p>");
                sb.AppendLine("  </div>");
            }
            if (opportunities.Count > 0)
            {
                sb.AppendLine("  <div class=\"callout callout-opportunity\">");
                sb.AppendLine("    <h4>Growth Opportunities</h4>");
                sb.AppendLine($"    <p>{string.Join(", ", opportunities.Select(s => $"{E(s!.Name)} ({s.CurrentProficiency}\u2192{s.TargetProficiency})"))}</p>");
                sb.AppendLine("  </div>");
            }

            sb.AppendLine("</section>");
        }

        // ── V. Goals & Progress ──
        if (linkedGoals.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">Goals &amp; Progress</h2>");
            sb.AppendLine("  <div class=\"goals-list\">");
            foreach (var goal in linkedGoals.OrderByDescending(g => g!.Status == GoalStatus.Active).ThenByDescending(g => g!.ProgressPercent))
            {
                var g = goal!;
                var statusLabel = g.Status.ToString();
                var statusClass = g.Status == GoalStatus.Completed ? "completed" : g.Status == GoalStatus.Active ? "active" : "upcoming";

                sb.AppendLine($"    <div class=\"goal-card {statusClass}\">");
                sb.AppendLine($"      <div class=\"goal-header\">");
                sb.AppendLine($"        <h4>{E(g.Title)}</h4>");
                sb.AppendLine($"        <span class=\"badge badge-{statusClass}\">{g.Category}</span>");
                sb.AppendLine($"      </div>");
                sb.AppendLine($"      <div class=\"goal-progress\">");
                sb.AppendLine($"        <div class=\"progress-bar\"><div class=\"progress-fill\" style=\"width:{g.ProgressPercent}%\"></div></div>");
                sb.AppendLine($"        <span>{g.ProgressPercent}%</span>");
                sb.AppendLine($"      </div>");
                if (!string.IsNullOrEmpty(g.Description))
                    sb.AppendLine($"      <p class=\"goal-desc\">{E(g.Description)}</p>");

                // Linked entities
                var goalSkills = g.LinkedSkillIds.Select(id => skillDict.GetValueOrDefault(id)).Where(s => s != null).ToList();
                var goalProjects = g.LinkedProjectIds.Select(id => projectDict.GetValueOrDefault(id)).Where(p => p != null).ToList();
                if (goalSkills.Count > 0 || goalProjects.Count > 0)
                {
                    sb.AppendLine("      <div class=\"goal-links\">");
                    foreach (var s in goalSkills)
                        sb.AppendLine($"        <span class=\"skill-chip\">{E(s!.Name)}</span>");
                    foreach (var p in goalProjects)
                        sb.AppendLine($"        <span class=\"project-chip\">{E(p!.Name)}</span>");
                    sb.AppendLine("      </div>");
                }

                // Completion reflection
                if (g.Status == GoalStatus.Completed && !string.IsNullOrEmpty(g.CompletionReflection))
                {
                    sb.AppendLine($"      <blockquote class=\"reflection-quote\">{E(g.CompletionReflection)}</blockquote>");
                }
                sb.AppendLine("    </div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // ── VI. Projects in Motion ──
        var activeProjects = linkedProjects.Where(p => p!.Status == ProjectStatus.Active).ToList();
        if (activeProjects.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">Projects in Motion</h2>");
            sb.AppendLine("  <div class=\"projects-grid\">");
            foreach (var proj in activeProjects)
            {
                var p = proj!;
                sb.AppendLine("    <div class=\"project-card\">");
                sb.AppendLine($"      <h4>{E(p.Name)}</h4>");
                if (!string.IsNullOrEmpty(p.Description))
                    sb.AppendLine($"      <p>{E(p.Description)}</p>");
                var projSkills = p.LinkedSkillIds.Select(id => skillDict.GetValueOrDefault(id)).Where(s => s != null).ToList();
                if (projSkills.Count > 0)
                {
                    sb.AppendLine("      <div class=\"project-skills\">");
                    foreach (var s in projSkills)
                        sb.AppendLine($"        <span class=\"skill-chip\">{E(s!.Name)}</span>");
                    sb.AppendLine("      </div>");
                }
                sb.AppendLine("    </div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // ── VII. Habits & Consistency ──
        if (linkedHabits.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">Habits &amp; Consistency</h2>");
            sb.AppendLine("  <div class=\"habits-grid\">");
            foreach (var habit in linkedHabits)
            {
                var h = habit!;
                var logs = allHabitLogs.Where(l => l.HabitId == h.Id).ToList();
                var completions = logs.Count(l => l.Completed);
                var rate = logs.Count > 0 ? Math.Round((double)completions / logs.Count * 100) : 0;
                var hSkills = h.LinkedSkillIds.Select(id => skillDict.GetValueOrDefault(id)).Where(s => s != null).ToList();
                var hGoals = h.LinkedGoalIds.Select(id => goalDict.GetValueOrDefault(id)).Where(g => g != null).ToList();

                sb.AppendLine("    <div class=\"habit-card\">");
                sb.AppendLine($"      <h4>{E(h.Name)}</h4>");
                sb.AppendLine($"      <div class=\"habit-rate\">");
                sb.AppendLine($"        <div class=\"progress-bar\"><div class=\"progress-fill\" style=\"width:{rate}%\"></div></div>");
                sb.AppendLine($"        <span>{rate}% ({completions}/{logs.Count})</span>");
                sb.AppendLine($"      </div>");
                if (hSkills.Count > 0 || hGoals.Count > 0)
                {
                    sb.AppendLine("      <div class=\"habit-links\">");
                    foreach (var s in hSkills)
                        sb.AppendLine($"        <span class=\"skill-chip\">{E(s!.Name)}</span>");
                    foreach (var g in hGoals)
                        sb.AppendLine($"        <span class=\"goal-chip\">{E(g!.Title)}</span>");
                    sb.AppendLine("      </div>");
                }
                sb.AppendLine("    </div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("</section>");
        }

        // ── VIII. Growth Over Time ──
        if (allSnapshots.Count > 0 || completedWeeklies.Count > 0)
        {
            sb.AppendLine("<section class=\"section\">");
            sb.AppendLine("  <h2 class=\"section-title\">Growth Over Time</h2>");

            // Skill proficiency evolution
            if (allSnapshots.Count >= 2 && linkedSkillIds.Count > 0)
            {
                sb.AppendLine("  <h3 class=\"subsection-title\">Skill Progression</h3>");
                sb.AppendLine("  <table class=\"data-table\">");
                sb.AppendLine("    <thead><tr><th>Skill</th>");
                foreach (var snap in allSnapshots.TakeLast(6))
                    sb.AppendLine($"      <th>{snap.SnapshotDate:MMM yy}</th>");
                sb.AppendLine("    </tr></thead>");
                sb.AppendLine("    <tbody>");
                foreach (var skillId in linkedSkillIds)
                {
                    var name = skillDict.GetValueOrDefault(skillId)?.Name ?? "Unknown";
                    sb.AppendLine($"    <tr><td>{E(name)}</td>");
                    foreach (var snap in allSnapshots.TakeLast(6))
                    {
                        var val = snap.SkillProficiencies.GetValueOrDefault(skillId);
                        sb.AppendLine($"      <td class=\"prof-cell prof-{val}\">{(val > 0 ? val.ToString() : "\u2013")}</td>");
                    }
                    sb.AppendLine("    </tr>");
                }
                sb.AppendLine("    </tbody>");
                sb.AppendLine("  </table>");
            }

            // Weekly reflection highlights
            if (completedWeeklies.Count > 0)
            {
                sb.AppendLine("  <h3 class=\"subsection-title\">Reflection Highlights</h3>");
                sb.AppendLine("  <div class=\"reflections-list\">");
                foreach (var w in completedWeeklies.TakeLast(8))
                {
                    sb.AppendLine($"    <div class=\"reflection-card\">");
                    sb.AppendLine($"      <div class=\"reflection-week\">Week of {w.WeekStart:MMM d, yyyy}</div>");
                    if (!string.IsNullOrEmpty(w.BiggestWin))
                        sb.AppendLine($"      <div class=\"reflection-win\"><strong>Win:</strong> {E(w.BiggestWin)}</div>");
                    if (!string.IsNullOrEmpty(w.LessonsLearned))
                        sb.AppendLine($"      <div class=\"reflection-lesson\"><strong>Lesson:</strong> {E(w.LessonsLearned)}</div>");
                    var wellbeing = new List<string>();
                    if (w.AverageEnergyLevel.HasValue) wellbeing.Add($"Energy {w.AverageEnergyLevel}/5");
                    if (w.AverageMoodLevel.HasValue) wellbeing.Add($"Mood {w.AverageMoodLevel}/5");
                    if (w.WorkLifeBalanceRating.HasValue) wellbeing.Add($"Balance {w.WorkLifeBalanceRating}/5");
                    if (wellbeing.Count > 0)
                        sb.AppendLine($"      <div class=\"reflection-wellbeing\">{string.Join(" \u00b7 ", wellbeing)}</div>");
                    sb.AppendLine("    </div>");
                }
                sb.AppendLine("  </div>");
            }

            // Energy/Mood/Balance trends
            var weekliesWithWellbeing = completedWeeklies.Where(w => w.AverageEnergyLevel.HasValue || w.AverageMoodLevel.HasValue).ToList();
            if (weekliesWithWellbeing.Count >= 3)
            {
                var avgEnergy = weekliesWithWellbeing.Where(w => w.AverageEnergyLevel.HasValue).Select(w => (double)w.AverageEnergyLevel!.Value).Average();
                var avgMood = weekliesWithWellbeing.Where(w => w.AverageMoodLevel.HasValue).Select(w => (double)w.AverageMoodLevel!.Value).Average();
                sb.AppendLine("  <div class=\"callout callout-wellbeing\">");
                sb.AppendLine($"    <h4>Wellbeing Averages</h4>");
                sb.AppendLine($"    <p>Average Energy: {avgEnergy:0.1}/5 &middot; Average Mood: {avgMood:0.1}/5</p>");
                sb.AppendLine("  </div>");
            }
            sb.AppendLine("</section>");
        }

        // ── IX. Looking Forward ──
        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("  <h2 class=\"section-title\">Looking Forward</h2>");

        if (upcomingMilestones.Count > 0)
        {
            sb.AppendLine("  <h3 class=\"subsection-title\">Upcoming Milestones</h3>");
            sb.AppendLine("  <div class=\"upcoming-list\">");
            foreach (var ms in upcomingMilestones.Where(m => m.TargetDate.HasValue).OrderBy(m => m.TargetDate))
            {
                sb.AppendLine($"    <div class=\"upcoming-item\">");
                sb.AppendLine($"      <span class=\"upcoming-date\">{ms.TargetDate!.Value:MMM yyyy}</span>");
                sb.AppendLine($"      <span class=\"upcoming-title\">{E(ms.Title)}</span>");
                sb.AppendLine($"      <span class=\"badge badge-upcoming\">{ms.Category}</span>");
                sb.AppendLine($"    </div>");
            }
            sb.AppendLine("  </div>");
        }

        // Skills still in development
        var developingSkills = linkedSkills.Where(s => s!.CurrentProficiency < s.TargetProficiency).OrderByDescending(s => s!.TargetProficiency - s!.CurrentProficiency).ToList();
        if (developingSkills.Count > 0)
        {
            sb.AppendLine("  <h3 class=\"subsection-title\">Skills in Development</h3>");
            sb.AppendLine("  <div class=\"developing-skills\">");
            foreach (var s in developingSkills)
            {
                var gap = s!.TargetProficiency - s.CurrentProficiency;
                sb.AppendLine($"    <div class=\"developing-skill\">");
                sb.AppendLine($"      <span>{E(s.Name)}</span>");
                sb.AppendLine($"      <span class=\"skill-gap\">{Skill.GetProficiencyName(s.CurrentProficiency)} \u2192 {Skill.GetProficiencyName(s.TargetProficiency)} (gap: {gap})</span>");
                sb.AppendLine($"    </div>");
            }
            sb.AppendLine("  </div>");
        }

        // Active goals remaining
        var activeGoals = linkedGoals.Where(g => g!.Status == GoalStatus.Active && g.ProgressPercent < 100).ToList();
        if (activeGoals.Count > 0)
        {
            sb.AppendLine("  <h3 class=\"subsection-title\">Active Goals</h3>");
            sb.AppendLine("  <div class=\"upcoming-list\">");
            foreach (var g in activeGoals.OrderByDescending(g => g!.ProgressPercent))
            {
                sb.AppendLine($"    <div class=\"upcoming-item\">");
                sb.AppendLine($"      <span class=\"upcoming-title\">{E(g!.Title)}</span>");
                sb.AppendLine($"      <span>{g.ProgressPercent}% complete</span>");
                sb.AppendLine($"    </div>");
            }
            sb.AppendLine("  </div>");
        }

        // Closing narrative
        if (primaryPlan?.Notes != null)
        {
            sb.AppendLine($"  <div class=\"narrative-block closing\">");
            sb.AppendLine($"    <p>{E(primaryPlan.Notes)}</p>");
            sb.AppendLine("  </div>");
        }
        sb.AppendLine("</section>");

        // Footer
        sb.AppendLine("<footer class=\"doc-footer\">");
        sb.AppendLine($"  <p>Generated by Self-Organizer on {now:MMMM d, yyyy} at {now:h:mm tt} UTC</p>");
        sb.AppendLine("</footer>");

        sb.AppendLine("</div>"); // .document
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendMetricCard(StringBuilder sb, string value, string label, string type)
    {
        sb.AppendLine($"    <div class=\"metric-card metric-{type}\">");
        sb.AppendLine($"      <div class=\"metric-value\">{value}</div>");
        sb.AppendLine($"      <div class=\"metric-label\">{label}</div>");
        sb.AppendLine($"    </div>");
    }

    private static string GetNarrativeStyles()
    {
        return @"
@import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700;900&family=DM+Sans:wght@300;400;500;600&display=swap');

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

:root {
  --bg: #1a1a1f;
  --bg-card: #222228;
  --bg-card-hover: #2a2a32;
  --text: #e8e6e3;
  --text-muted: #9a9a9a;
  --gold: #c8a96e;
  --gold-dim: #a08550;
  --gold-glow: rgba(200,169,110,0.15);
  --green: #4ade80;
  --blue: #60a5fa;
  --red: #f87171;
  --border: #333339;
  --font-serif: 'Playfair Display', Georgia, 'Times New Roman', serif;
  --font-sans: 'DM Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

body {
  background: var(--bg);
  color: var(--text);
  font-family: var(--font-sans);
  font-size: 15px;
  line-height: 1.7;
  -webkit-font-smoothing: antialiased;
}

.document {
  max-width: 900px;
  margin: 0 auto;
  padding: 40px 24px 80px;
}

/* ── Animations ── */
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(24px); }
  to { opacity: 1; transform: translateY(0); }
}
@keyframes pulse {
  0%, 100% { box-shadow: 0 0 0 0 var(--gold-glow); }
  50% { box-shadow: 0 0 0 8px transparent; }
}

/* ── Hero ── */
.hero {
  text-align: center;
  padding: 80px 0 60px;
  border-bottom: 1px solid var(--border);
  margin-bottom: 60px;
  animation: fadeInUp 0.8s ease-out;
}
.hero-eyebrow {
  text-transform: uppercase;
  letter-spacing: 4px;
  font-size: 11px;
  color: var(--gold);
  font-weight: 500;
  margin-bottom: 16px;
}
.hero-title {
  font-family: var(--font-serif);
  font-size: clamp(2rem, 5vw, 3.2rem);
  font-weight: 700;
  color: var(--text);
  line-height: 1.2;
  margin-bottom: 24px;
}
.hero-roles {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  font-size: 17px;
  margin-bottom: 32px;
}
.role-current { color: var(--text-muted); }
.role-arrow { color: var(--gold); font-size: 22px; }
.role-target { color: var(--gold); font-weight: 600; }
.hero-progress {
  max-width: 400px;
  margin: 0 auto 20px;
  display: flex;
  align-items: center;
  gap: 16px;
}
.progress-bar {
  flex: 1;
  height: 6px;
  background: var(--border);
  border-radius: 3px;
  overflow: hidden;
}
.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--gold-dim), var(--gold));
  border-radius: 3px;
  transition: width 0.6s ease;
}
.progress-label { font-size: 13px; color: var(--gold); font-weight: 500; white-space: nowrap; }
.hero-dates { color: var(--text-muted); font-size: 14px; margin-bottom: 8px; }
.hero-generated { color: var(--text-muted); font-size: 12px; opacity: 0.6; }

/* ── Sections ── */
.section {
  margin-bottom: 64px;
  animation: fadeInUp 0.6s ease-out both;
}
.section:nth-child(2) { animation-delay: 0.1s; }
.section:nth-child(3) { animation-delay: 0.15s; }
.section:nth-child(4) { animation-delay: 0.2s; }
.section:nth-child(5) { animation-delay: 0.25s; }
.section-title {
  font-family: var(--font-serif);
  font-size: 1.8rem;
  font-weight: 700;
  color: var(--text);
  margin-bottom: 32px;
  padding-bottom: 12px;
  border-bottom: 2px solid var(--gold);
  display: inline-block;
}
.subsection-title {
  font-family: var(--font-serif);
  font-size: 1.15rem;
  font-weight: 600;
  color: var(--gold);
  margin: 32px 0 16px;
}

/* ── Metrics Grid ── */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
  gap: 16px;
  margin-bottom: 32px;
}
.metric-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 24px 16px;
  text-align: center;
  transition: border-color 0.2s;
}
.metric-card:hover { border-color: var(--gold-dim); }
.metric-value {
  font-family: var(--font-serif);
  font-size: 1.6rem;
  font-weight: 700;
  color: var(--gold);
  margin-bottom: 4px;
}
.metric-label { font-size: 12px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 1px; }

/* ── Narrative Block ── */
.narrative-block {
  background: var(--bg-card);
  border-left: 3px solid var(--gold);
  padding: 24px 28px;
  border-radius: 0 8px 8px 0;
  margin: 24px 0;
  font-size: 15px;
  line-height: 1.8;
  color: var(--text-muted);
}
.narrative-block.closing { border-left-color: var(--green); }

/* ── Timeline ── */
.timeline { position: relative; padding-left: 32px; }
.timeline::before {
  content: '';
  position: absolute;
  left: 11px;
  top: 8px;
  bottom: 8px;
  width: 2px;
  background: var(--border);
}
.timeline-item { position: relative; margin-bottom: 28px; }
.timeline-marker {
  position: absolute;
  left: -32px;
  top: 6px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: var(--bg);
  border: 2px solid var(--border);
  z-index: 1;
}
.timeline-item.completed .timeline-marker {
  background: var(--green);
  border-color: var(--green);
}
.timeline-item.completed .timeline-marker::after {
  content: '\2713';
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 11px;
  color: #111;
  font-weight: 700;
}
.timeline-item.active .timeline-marker {
  background: var(--gold);
  border-color: var(--gold);
  animation: pulse 2s ease-in-out infinite;
}
.timeline-content {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 18px 22px;
}
.timeline-item.completed .timeline-content { border-color: rgba(74,222,128,0.2); }
.timeline-item.active .timeline-content { border-color: var(--gold-dim); }
.timeline-header { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.timeline-header h4 { font-family: var(--font-serif); font-size: 1rem; font-weight: 600; margin: 0; }
.timeline-date { font-size: 13px; color: var(--text-muted); margin-top: 4px; }
.timeline-desc { font-size: 14px; color: var(--text-muted); margin-top: 8px; }
.timeline-skills { display: flex; gap: 6px; flex-wrap: wrap; margin-top: 10px; }

/* ── Badges & Chips ── */
.badge {
  display: inline-block;
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 3px 10px;
  border-radius: 20px;
  white-space: nowrap;
}
.badge-completed { background: rgba(74,222,128,0.15); color: var(--green); }
.badge-active { background: var(--gold-glow); color: var(--gold); }
.badge-upcoming { background: rgba(96,165,250,0.15); color: var(--blue); }

.skill-chip, .project-chip, .goal-chip {
  display: inline-block;
  font-size: 11px;
  padding: 2px 10px;
  border-radius: 12px;
  border: 1px solid var(--border);
  color: var(--text-muted);
  background: transparent;
}
.skill-chip { border-color: var(--gold-dim); color: var(--gold); }
.project-chip { border-color: var(--blue); color: var(--blue); }
.goal-chip { border-color: var(--green); color: var(--green); }

/* ── Skills Grid ── */
.skills-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 14px;
  margin-bottom: 24px;
}
.skill-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 16px 18px;
}
.skill-header { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 10px; }
.skill-name { font-weight: 600; font-size: 14px; }
.skill-level { font-size: 11px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
.skill-bar { height: 5px; background: var(--border); border-radius: 3px; overflow: hidden; margin-bottom: 8px; }
.skill-bar-fill { height: 100%; border-radius: 3px; }
.skill-meta { display: flex; justify-content: space-between; font-size: 12px; color: var(--text-muted); }
.skill-delta.delta-up { color: var(--green); }
.skill-delta.delta-down { color: var(--red); }
.skill-delta.delta-flat { color: var(--text-muted); }

/* ── Callouts ── */
.callout {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 18px 22px;
  margin: 16px 0;
}
.callout h4 { font-family: var(--font-serif); font-size: 1rem; margin-bottom: 8px; }
.callout-strength { border-left: 3px solid var(--green); }
.callout-strength h4 { color: var(--green); }
.callout-opportunity { border-left: 3px solid var(--gold); }
.callout-opportunity h4 { color: var(--gold); }
.callout-wellbeing { border-left: 3px solid var(--blue); }
.callout-wellbeing h4 { color: var(--blue); }

/* ── Goals ── */
.goals-list { display: flex; flex-direction: column; gap: 16px; }
.goal-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 20px 24px;
}
.goal-card.completed { border-color: rgba(74,222,128,0.2); }
.goal-card.active { border-color: rgba(200,169,110,0.3); }
.goal-header { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 12px; }
.goal-header h4 { font-family: var(--font-serif); font-size: 1rem; font-weight: 600; margin: 0; }
.goal-progress { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; font-size: 13px; color: var(--text-muted); }
.goal-progress .progress-bar { flex: 1; }
.goal-desc { font-size: 14px; color: var(--text-muted); margin-bottom: 10px; }
.goal-links { display: flex; gap: 6px; flex-wrap: wrap; }
.reflection-quote {
  margin-top: 12px;
  padding: 12px 16px;
  border-left: 2px solid var(--gold-dim);
  font-style: italic;
  color: var(--text-muted);
  font-size: 14px;
}

/* ── Projects ── */
.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}
.project-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 20px 22px;
}
.project-card h4 { font-family: var(--font-serif); font-size: 1rem; font-weight: 600; margin-bottom: 8px; }
.project-card p { font-size: 14px; color: var(--text-muted); margin-bottom: 12px; }
.project-skills { display: flex; gap: 6px; flex-wrap: wrap; }

/* ── Habits ── */
.habits-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}
.habit-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 18px 20px;
}
.habit-card h4 { font-size: 15px; font-weight: 600; margin-bottom: 10px; }
.habit-rate { display: flex; align-items: center; gap: 12px; font-size: 13px; color: var(--text-muted); margin-bottom: 10px; }
.habit-rate .progress-bar { flex: 1; }
.habit-links { display: flex; gap: 6px; flex-wrap: wrap; }

/* ── Data Table ── */
.data-table {
  width: 100%;
  border-collapse: collapse;
  margin: 16px 0 32px;
  font-size: 13px;
}
.data-table th, .data-table td {
  padding: 10px 14px;
  text-align: left;
  border-bottom: 1px solid var(--border);
}
.data-table th {
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-size: 11px;
}
.data-table td:first-child { font-weight: 500; }
.prof-cell { text-align: center; }
.prof-1 { color: var(--red); }
.prof-2 { color: #fb923c; }
.prof-3 { color: var(--gold); }
.prof-4 { color: var(--blue); }
.prof-5 { color: var(--green); }

/* ── Reflections ── */
.reflections-list { display: flex; flex-direction: column; gap: 14px; }
.reflection-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 16px 20px;
}
.reflection-week { font-size: 12px; color: var(--gold); font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
.reflection-win { font-size: 14px; margin-bottom: 4px; }
.reflection-lesson { font-size: 14px; color: var(--text-muted); margin-bottom: 4px; }
.reflection-wellbeing { font-size: 12px; color: var(--text-muted); margin-top: 6px; }

/* ── Upcoming / Looking Forward ── */
.upcoming-list { display: flex; flex-direction: column; gap: 10px; margin-bottom: 24px; }
.upcoming-item {
  display: flex;
  align-items: center;
  gap: 16px;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 12px 18px;
  font-size: 14px;
}
.upcoming-date { font-size: 12px; color: var(--gold); font-weight: 600; min-width: 70px; }
.upcoming-title { flex: 1; }
.developing-skills { display: flex; flex-direction: column; gap: 8px; margin-bottom: 24px; }
.developing-skill {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 10px 18px;
  font-size: 14px;
}
.skill-gap { font-size: 12px; color: var(--text-muted); }

/* ── Footer ── */
.doc-footer {
  text-align: center;
  padding: 40px 0 20px;
  border-top: 1px solid var(--border);
  margin-top: 40px;
}
.doc-footer p { font-size: 12px; color: var(--text-muted); opacity: 0.5; }

/* ── Print Styles ── */
@media print {
  :root {
    --bg: #ffffff;
    --bg-card: #f9f9f9;
    --bg-card-hover: #f0f0f0;
    --text: #1a1a1f;
    --text-muted: #555;
    --gold: #8b6d2e;
    --gold-dim: #a08550;
    --gold-glow: rgba(139,109,46,0.1);
    --green: #16a34a;
    --blue: #2563eb;
    --red: #dc2626;
    --border: #ddd;
  }
  body { font-size: 12px; }
  .document { max-width: 100%; padding: 0; }
  .hero { padding: 40px 0 30px; }
  .section { margin-bottom: 36px; page-break-inside: avoid; }
  .timeline-item.active .timeline-marker { animation: none; }
  @keyframes fadeInUp { from { opacity: 1; transform: none; } to { opacity: 1; transform: none; } }
}

@media (max-width: 600px) {
  .document { padding: 20px 16px 40px; }
  .hero { padding: 40px 0 32px; }
  .hero-title { font-size: 1.6rem; }
  .metrics-grid { grid-template-columns: repeat(2, 1fr); }
  .skills-grid, .projects-grid, .habits-grid { grid-template-columns: 1fr; }
}
";
    }
}
