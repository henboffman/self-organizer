using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Pages.Career;

public partial class CareerDashboard
{
    private bool _isLoading = true;
    private bool _showDummyData;

    private List<CareerPlan> _realPlans = new();
    private List<CareerPlan> _displayPlans = new();
    private List<CareerPlan> _timelinePlans = new();
    private List<Skill> _timelineSkills = new();
    private List<GrowthSnapshot> _snapshots = new();
    private List<GrowthContextSummary> _journeyPeriods = new();
    private bool _isCapturing;
    private bool _isExporting;

    private int _activePlansCount;
    private int _skillsInProgressCount;
    private int _activeGoalsCount;
    private int _upcomingMilestonesCount;

    private Guid? _expandedPlanId;
    private CareerExportData? _demoData;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        _isLoading = false;
    }

    private async Task LoadDataAsync()
    {
        _realPlans = (await CareerPlanService.GetAllAsync()).ToList();

        var goals = (await GoalService.GetActiveGoalsAsync()).ToList();
        var skills = (await SkillService.GetActiveSkillsAsync()).ToList();

        _activeGoalsCount = goals.Count;
        _skillsInProgressCount = skills.Count(s => s.Type == SkillType.Want && s.IsActive);

        // Load growth journey data
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var journeyStart = now.AddMonths(-12);
        _snapshots = (await GrowthContextService.GetSnapshotsInRangeAsync(journeyStart.AddMonths(-6), now)).ToList();
        _journeyPeriods = await GrowthContextService.GetJourneyAsync(journeyStart, now);

        RefreshDisplay(goals, skills);
    }

    private void RefreshDisplay(List<Goal>? goals = null, List<Skill>? skills = null)
    {
        var plans = _showDummyData && _demoData != null
            ? _realPlans.Concat(_demoData.Plans).ToList()
            : _realPlans;

        _displayPlans = plans.OrderByDescending(p => p.Status == CareerPlanStatus.Active)
            .ThenByDescending(p => p.ModifiedAt)
            .ToList();

        _activePlansCount = plans.Count(p => p.Status == CareerPlanStatus.Active);

        var allMilestones = plans.SelectMany(p => p.Milestones).ToList();
        _upcomingMilestonesCount = allMilestones.Count(m =>
            m.Status is MilestoneStatus.NotStarted or MilestoneStatus.InProgress
            && m.TargetDate.HasValue
            && m.TargetDate.Value >= DateTime.UtcNow
            && m.TargetDate.Value <= DateTime.UtcNow.AddMonths(6));

        _timelinePlans = plans;
        _timelineSkills = _showDummyData && _demoData != null
            ? (skills ?? new List<Skill>()).Concat(_demoData.Skills).ToList()
            : (skills ?? new List<Skill>());

        if (_showDummyData && _demoData != null)
        {
            _activeGoalsCount += _demoData.Goals.Count(g => g.Status == GoalStatus.Active);
            _skillsInProgressCount += _demoData.Skills.Count(s => s.Type == SkillType.Want && s.IsActive);
            _snapshots = _snapshots.Concat(_demoData.GrowthSnapshots).OrderBy(s => s.SnapshotDate).ToList();
            _journeyPeriods = GenerateDemoJourneyPeriods();
        }
    }

    private void TogglePlanExpanded(CareerPlan plan)
    {
        _expandedPlanId = _expandedPlanId == plan.Id ? null : plan.Id;
    }

    private bool IsDemoPlan(CareerPlan plan) =>
        _showDummyData && !_realPlans.Any(rp => rp.Id == plan.Id);

    private async Task ToggleDummyData()
    {
        _showDummyData = !_showDummyData;
        _demoData = _showDummyData ? GenerateDemoData() : null;

        var goals = (await GoalService.GetActiveGoalsAsync()).ToList();
        var skills = (await SkillService.GetActiveSkillsAsync()).ToList();

        // Reload real snapshots/journey so we can merge fresh
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var journeyStart = now.AddMonths(-12);
        _snapshots = (await GrowthContextService.GetSnapshotsInRangeAsync(journeyStart.AddMonths(-6), now)).ToList();
        _journeyPeriods = await GrowthContextService.GetJourneyAsync(journeyStart, now);

        _activeGoalsCount = goals.Count;
        _skillsInProgressCount = skills.Count(s => s.Type == SkillType.Want && s.IsActive);

        RefreshDisplay(goals, skills);
    }

    private async Task CaptureSnapshot()
    {
        _isCapturing = true;
        StateHasChanged();

        await GrowthContextService.CaptureSnapshotAsync();
        await LoadDataAsync();

        _isCapturing = false;
        StateHasChanged();
    }

    private async Task ExportCareerPlan()
    {
        _isExporting = true;
        StateHasChanged();

        try
        {
            string html;
            if (_showDummyData && _demoData != null)
            {
                html = await GrowthContextService.ExportCareerPlanHtmlAsync(_demoData);
            }
            else
            {
                html = await GrowthContextService.ExportCareerPlanHtmlAsync();
            }

            var filename = $"career-plan-{DateTime.Now:yyyy-MM-dd}.html";
            await ExportService.DownloadFileAsync(filename, html, "text/html");
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private void HandleSnapshotSelected(GrowthSnapshot snapshot)
    {
        // Snapshot selected from timeline - could scroll to it in journey
    }

    private void HandlePeriodSelected(GrowthContextSummary period)
    {
        // Period selected from journey grid
    }

    // ── Deterministic GUIDs for cross-referencing demo entities ──

    private static Guid DemoGuid(int seed) =>
        new(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    // Skills
    private static readonly Guid SkillSystemDesign = DemoGuid(1);
    private static readonly Guid SkillCloudArch = DemoGuid(2);
    private static readonly Guid SkillTechWriting = DemoGuid(3);
    private static readonly Guid SkillLeadership = DemoGuid(4);
    private static readonly Guid SkillKubernetes = DemoGuid(5);

    // Goals
    private static readonly Guid GoalAwsCert = DemoGuid(10);
    private static readonly Guid GoalPlatformRedesign = DemoGuid(11);
    private static readonly Guid GoalBlogSeries = DemoGuid(12);
    private static readonly Guid GoalMentoring = DemoGuid(13);

    // Projects
    private static readonly Guid ProjPlatformRedesign = DemoGuid(20);
    private static readonly Guid ProjBlogSeries = DemoGuid(21);
    private static readonly Guid ProjAwsCertPrep = DemoGuid(22);

    // Habits
    private static readonly Guid HabitStudy = DemoGuid(30);
    private static readonly Guid HabitWrite = DemoGuid(31);
    private static readonly Guid HabitMentor = DemoGuid(32);

    // Career Plan
    private static readonly Guid PlanStaffEngineer = DemoGuid(100);

    private static CareerExportData GenerateDemoData()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // ── Skills ──
        var skills = new List<Skill>
        {
            new()
            {
                Id = SkillSystemDesign, Name = "System Design",
                Category = SkillCategory.Technical, Type = SkillType.Want,
                CurrentProficiency = 3, TargetProficiency = 5, IsActive = true,
                StartDate = now.AddMonths(-18), TargetDate = now.AddMonths(12),
                Color = "#3b82f6"
            },
            new()
            {
                Id = SkillCloudArch, Name = "Cloud Architecture",
                Category = SkillCategory.Technical, Type = SkillType.Want,
                CurrentProficiency = 2, TargetProficiency = 4, IsActive = true,
                StartDate = now.AddMonths(-12), TargetDate = now.AddMonths(18),
                Color = "#8b5cf6"
            },
            new()
            {
                Id = SkillTechWriting, Name = "Technical Writing",
                Category = SkillCategory.Creative, Type = SkillType.Want,
                CurrentProficiency = 2, TargetProficiency = 4, IsActive = true,
                StartDate = now.AddMonths(-6), TargetDate = now.AddMonths(24),
                Color = "#06b6d4"
            },
            new()
            {
                Id = SkillLeadership, Name = "Leadership & Mentoring",
                Category = SkillCategory.SoftSkills, Type = SkillType.Want,
                CurrentProficiency = 2, TargetProficiency = 4, IsActive = true,
                StartDate = now.AddMonths(-12), TargetDate = now.AddMonths(20),
                Color = "#10b981"
            },
            new()
            {
                Id = SkillKubernetes, Name = "Kubernetes",
                Category = SkillCategory.ToolsSoftware, Type = SkillType.Want,
                CurrentProficiency = 1, TargetProficiency = 3, IsActive = true,
                StartDate = now.AddMonths(-3), TargetDate = now.AddMonths(15),
                Color = "#f59e0b"
            }
        };
        foreach (var s in skills) { s.IsSampleData = true; s.CreatedAt = s.StartDate ?? now; s.ModifiedAt = now; }

        // ── Goals ──
        var goals = new List<Goal>
        {
            new()
            {
                Id = GoalAwsCert, Title = "Earn AWS Solutions Architect Certification",
                Description = "Pass the AWS SA Professional exam to validate cloud architecture skills.",
                Status = GoalStatus.Completed, Category = GoalCategory.Career,
                ProgressPercent = 100, Priority = 1,
                StartDate = now.AddMonths(-8), TargetDate = now.AddMonths(-2),
                CompletedAt = now.AddMonths(-2),
                CompletionReflection = "The structured study plan and daily 30-minute habit made all the difference. Earned on the first attempt with a score of 842/1000.",
                LinkedSkillIds = new List<Guid> { SkillCloudArch },
                LinkedProjectIds = new List<Guid> { ProjAwsCertPrep },
                LinkedHabitIds = new List<Guid> { HabitStudy },
                IsSampleData = true, CreatedAt = now.AddMonths(-8), ModifiedAt = now.AddMonths(-2)
            },
            new()
            {
                Id = GoalPlatformRedesign, Title = "Lead platform redesign initiative",
                Description = "Drive the next-gen platform architecture from RFC through implementation, coordinating across 3 teams.",
                Status = GoalStatus.Active, Category = GoalCategory.Career,
                ProgressPercent = 60, Priority = 1,
                StartDate = now.AddMonths(-6), TargetDate = now.AddMonths(6),
                LinkedSkillIds = new List<Guid> { SkillSystemDesign, SkillCloudArch },
                LinkedProjectIds = new List<Guid> { ProjPlatformRedesign },
                IsSampleData = true, CreatedAt = now.AddMonths(-6), ModifiedAt = now
            },
            new()
            {
                Id = GoalBlogSeries, Title = "Publish technical blog series",
                Description = "Write and publish 6 blog posts on distributed systems patterns to build external visibility.",
                Status = GoalStatus.Active, Category = GoalCategory.Creative,
                ProgressPercent = 33, Priority = 2,
                StartDate = now.AddMonths(-4), TargetDate = now.AddMonths(8),
                LinkedSkillIds = new List<Guid> { SkillTechWriting, SkillSystemDesign },
                LinkedProjectIds = new List<Guid> { ProjBlogSeries },
                LinkedHabitIds = new List<Guid> { HabitWrite },
                IsSampleData = true, CreatedAt = now.AddMonths(-4), ModifiedAt = now
            },
            new()
            {
                Id = GoalMentoring, Title = "Build mentoring program",
                Description = "Establish a structured mentoring program for 2 junior engineers, meeting weekly.",
                Status = GoalStatus.Active, Category = GoalCategory.Career,
                ProgressPercent = 20, Priority = 2,
                StartDate = now.AddMonths(-2), TargetDate = now.AddMonths(10),
                LinkedSkillIds = new List<Guid> { SkillLeadership },
                LinkedHabitIds = new List<Guid> { HabitMentor },
                IsSampleData = true, CreatedAt = now.AddMonths(-2), ModifiedAt = now
            }
        };

        // ── Projects ──
        var projects = new List<Project>
        {
            new()
            {
                Id = ProjPlatformRedesign, Name = "Platform Redesign",
                Description = "Next-gen platform architecture with event-driven microservices and improved observability.",
                Status = ProjectStatus.Active, Priority = 1,
                LinkedSkillIds = new List<Guid> { SkillSystemDesign, SkillCloudArch, SkillKubernetes },
                IsSampleData = true, CreatedAt = now.AddMonths(-6), ModifiedAt = now
            },
            new()
            {
                Id = ProjBlogSeries, Name = "Blog Series",
                Description = "6-part series on distributed systems: consensus, partitioning, caching, observability, resilience, and deployment.",
                Status = ProjectStatus.Active, Priority = 2,
                LinkedSkillIds = new List<Guid> { SkillTechWriting, SkillSystemDesign },
                IsSampleData = true, CreatedAt = now.AddMonths(-4), ModifiedAt = now
            },
            new()
            {
                Id = ProjAwsCertPrep, Name = "AWS Cert Prep",
                Description = "Study materials, practice exams, and hands-on labs for the AWS SA Professional certification.",
                Status = ProjectStatus.Completed, Priority = 1,
                CompletedAt = now.AddMonths(-2),
                CompletionReflection = "Completed 4 practice exams and 12 hands-on labs. The consistent daily study habit was key.",
                LinkedSkillIds = new List<Guid> { SkillCloudArch },
                IsSampleData = true, CreatedAt = now.AddMonths(-8), ModifiedAt = now.AddMonths(-2)
            }
        };

        // ── Habits ──
        var habits = new List<Habit>
        {
            new()
            {
                Id = HabitStudy, Name = "Study 30 min daily",
                Description = "Dedicated study time for certifications and technical depth.",
                Frequency = HabitFrequency.Daily, TargetCount = 1, IsActive = true,
                Category = "Learning", StartDate = now.AddMonths(-8),
                LinkedGoalIds = new List<Guid> { GoalAwsCert, GoalPlatformRedesign },
                LinkedSkillIds = new List<Guid> { SkillCloudArch, SkillSystemDesign },
                IsSampleData = true, CreatedAt = now.AddMonths(-8), ModifiedAt = now
            },
            new()
            {
                Id = HabitWrite, Name = "Write 500 words",
                Description = "Write at least 500 words of blog content or technical documentation.",
                Frequency = HabitFrequency.Weekdays, TargetCount = 1, IsActive = true,
                Category = "Creative", StartDate = now.AddMonths(-4),
                LinkedGoalIds = new List<Guid> { GoalBlogSeries },
                LinkedSkillIds = new List<Guid> { SkillTechWriting },
                IsSampleData = true, CreatedAt = now.AddMonths(-4), ModifiedAt = now
            },
            new()
            {
                Id = HabitMentor, Name = "Weekly 1:1 mentoring",
                Description = "Weekly 1:1 session with mentees to review progress and provide guidance.",
                Frequency = HabitFrequency.Weekly, TargetCount = 1, IsActive = true,
                Category = "Leadership", StartDate = now.AddMonths(-2),
                LinkedGoalIds = new List<Guid> { GoalMentoring },
                LinkedSkillIds = new List<Guid> { SkillLeadership },
                IsSampleData = true, CreatedAt = now.AddMonths(-2), ModifiedAt = now
            }
        };

        // ── Habit Logs (~90, 30 days x 3 habits, ~75% completion) ──
        var habitLogs = new List<HabitLog>();
        var rng = new Random(42); // deterministic
        for (var day = 0; day < 30; day++)
        {
            var date = today.AddDays(-day);
            foreach (var habit in habits)
            {
                // Skip weekends for weekday habit
                if (habit.Frequency == HabitFrequency.Weekdays && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;
                // Weekly habits only on Wednesdays
                if (habit.Frequency == HabitFrequency.Weekly && date.DayOfWeek != DayOfWeek.Wednesday)
                    continue;

                var completed = rng.NextDouble() < 0.75;
                habitLogs.Add(new HabitLog
                {
                    Id = Guid.NewGuid(),
                    HabitId = habit.Id,
                    Date = date,
                    Completed = completed,
                    CompletedAt = completed ? date.ToDateTime(new TimeOnly(8, 0)).AddMinutes(rng.Next(0, 480)) : null,
                    IsSampleData = true,
                    CreatedAt = date.ToDateTime(TimeOnly.MinValue),
                    ModifiedAt = date.ToDateTime(TimeOnly.MinValue)
                });
            }
        }

        // ── Focus Sessions (~20, last 3 months, 25-50 min each) ──
        var focusSessions = new List<FocusSessionLog>();
        for (var i = 0; i < 20; i++)
        {
            var daysAgo = rng.Next(1, 90);
            var duration = rng.Next(25, 51);
            var startedAt = now.AddDays(-daysAgo).Date.AddHours(rng.Next(8, 18));
            focusSessions.Add(new FocusSessionLog
            {
                Id = Guid.NewGuid(),
                TaskTitle = i switch
                {
                    < 5 => "Study AWS SA practice exam",
                    < 10 => "Write blog post draft",
                    < 15 => "Design platform RFC section",
                    _ => "Review mentee PRs"
                },
                DurationMinutes = duration,
                StartedAt = startedAt,
                EndedAt = startedAt.AddMinutes(duration),
                FocusRating = rng.Next(3, 6),
                OriginalEstimatedMinutes = duration,
                ActualElapsedSeconds = duration * 60,
                IsSampleData = true,
                CreatedAt = startedAt,
                ModifiedAt = startedAt
            });
        }

        // ── Weekly Snapshots (6, last 6 weeks) ──
        var weeklySnapshots = new List<WeeklySnapshot>();
        var wins = new[] {
            "Shipped platform redesign Phase 1",
            "Published first blog post - great reception",
            "Mentee landed their first PR solo",
            "Passed AWS practice exam with 90%",
            "Finished RFC draft for event-driven arch",
            "Got positive skip-level feedback on leadership growth"
        };
        var lessons = new[] {
            "Need to block more time for deep work",
            "Writing regularly makes ideas clearer",
            "Delegation is a skill that needs practice",
            "Hands-on labs teach more than reading",
            "Getting feedback early saves rework",
            "Balance is about saying no to good things"
        };
        for (var w = 0; w < 6; w++)
        {
            var weekStart = today.AddDays(-(w * 7 + (int)today.DayOfWeek - 1));
            if (weekStart.DayOfWeek != DayOfWeek.Monday)
                weekStart = weekStart.AddDays(-(int)weekStart.DayOfWeek + 1);

            weeklySnapshots.Add(new WeeklySnapshot
            {
                Id = Guid.NewGuid(),
                WeekStart = weekStart,
                ReviewCompleted = true,
                TotalTasksCompleted = rng.Next(8, 20),
                TotalMinutesWorked = rng.Next(1800, 2600),
                BiggestWin = wins[w],
                LessonsLearned = lessons[w],
                AverageEnergyLevel = rng.Next(3, 6),
                AverageMoodLevel = rng.Next(3, 6),
                WorkLifeBalanceRating = rng.Next(3, 6),
                HabitsCompletedCount = rng.Next(12, 20),
                CompletedAt = weekStart.ToDateTime(new TimeOnly(17, 0)).AddDays(6),
                IsSampleData = true,
                CreatedAt = weekStart.ToDateTime(TimeOnly.MinValue),
                ModifiedAt = weekStart.ToDateTime(TimeOnly.MinValue).AddDays(6)
            });
        }

        // ── Growth Snapshots (4: -9mo, -6mo, -3mo, now) showing skill progression ──
        var snapshotDates = new[] { today.AddMonths(-9), today.AddMonths(-6), today.AddMonths(-3), today };
        var skillProgressions = new Dictionary<Guid, int[]>
        {
            [SkillSystemDesign] = new[] { 1, 2, 2, 3 },
            [SkillCloudArch] = new[] { 1, 1, 2, 2 },
            [SkillTechWriting] = new[] { 1, 1, 2, 2 },
            [SkillLeadership] = new[] { 1, 1, 1, 2 },
            [SkillKubernetes] = new[] { 0, 0, 1, 1 } // 0 = not yet tracked
        };
        var goalProgressions = new Dictionary<Guid, int[]>
        {
            [GoalAwsCert] = new[] { 10, 40, 80, 100 },
            [GoalPlatformRedesign] = new[] { 0, 15, 35, 60 },
            [GoalBlogSeries] = new[] { 0, 0, 10, 33 },
            [GoalMentoring] = new[] { 0, 0, 0, 20 }
        };
        var skillNameMap = skills.ToDictionary(s => s.Id, s => s.Name);
        var goalTitleMap = goals.ToDictionary(g => g.Id, g => g.Title);

        var growthSnapshots = new List<GrowthSnapshot>();
        for (var i = 0; i < snapshotDates.Length; i++)
        {
            var snap = new GrowthSnapshot
            {
                Id = Guid.NewGuid(),
                SnapshotDate = snapshotDates[i],
                Trigger = SnapshotTrigger.Manual,
                Notes = i switch
                {
                    0 => "Starting the journey toward Staff Engineer",
                    1 => "Good momentum on AWS prep",
                    2 => "Shifting focus to platform redesign and writing",
                    _ => "Strong quarter - cert earned, blog started, mentoring underway"
                },
                IsSampleData = true,
                CreatedAt = snapshotDates[i].ToDateTime(TimeOnly.MinValue),
                ModifiedAt = snapshotDates[i].ToDateTime(TimeOnly.MinValue)
            };

            foreach (var (skillId, profs) in skillProgressions)
            {
                if (profs[i] > 0) // only include if skill was being tracked
                {
                    snap.SkillProficiencies[skillId] = profs[i];
                    snap.SkillNames[skillId] = skillNameMap[skillId];
                }
            }

            foreach (var (goalId, progs) in goalProgressions)
            {
                if (progs[i] > 0) // only include if goal existed
                {
                    snap.GoalProgress[goalId] = progs[i];
                    snap.GoalTitles[goalId] = goalTitleMap[goalId];
                }
            }

            snap.BalanceRatings = new Dictionary<string, int>
            {
                ["Career"] = Math.Min(5, 3 + i),
                ["Health"] = rng.Next(3, 5),
                ["Relationships"] = rng.Next(3, 5)
            };

            growthSnapshots.Add(snap);
        }

        // ── Tasks (15: 10 completed, 5 active) ──
        var tasks = new List<TodoTask>();
        var completedTaskTitles = new[]
        {
            "Complete AWS SA practice exam #1", "Complete AWS SA practice exam #2",
            "Write blog post: consensus algorithms", "Review platform RFC with team lead",
            "Set up mentoring session structure", "Deploy prototype to staging",
            "Research event-driven patterns", "Write blog post: partitioning strategies",
            "Create platform monitoring dashboard", "Prepare tech talk slides"
        };
        var activeTaskTitles = new[]
        {
            "Write blog post: caching patterns", "Design service mesh topology",
            "Prepare mentee growth plan Q2", "Review Kubernetes networking docs",
            "Draft platform migration plan"
        };

        for (var i = 0; i < completedTaskTitles.Length; i++)
        {
            var daysAgo = rng.Next(5, 90);
            var completedAt = now.AddDays(-daysAgo);
            tasks.Add(new TodoTask
            {
                Id = Guid.NewGuid(),
                Title = completedTaskTitles[i],
                Status = TodoTaskStatus.Completed,
                CompletedAt = completedAt,
                Contexts = new List<string> { i < 4 ? "@work" : i < 7 ? "@home" : "@work" },
                GoalIds = i switch
                {
                    < 2 => new List<Guid> { GoalAwsCert },
                    < 4 or 7 => new List<Guid> { GoalBlogSeries },
                    4 => new List<Guid> { GoalMentoring },
                    _ => new List<Guid> { GoalPlatformRedesign }
                },
                SkillIds = i switch
                {
                    < 2 => new List<Guid> { SkillCloudArch },
                    < 4 or 7 => new List<Guid> { SkillTechWriting },
                    4 => new List<Guid> { SkillLeadership },
                    _ => new List<Guid> { SkillSystemDesign }
                },
                Priority = i < 3 ? 1 : 2,
                IsSampleData = true,
                CreatedAt = completedAt.AddDays(-rng.Next(1, 14)),
                ModifiedAt = completedAt
            });
        }

        for (var i = 0; i < activeTaskTitles.Length; i++)
        {
            tasks.Add(new TodoTask
            {
                Id = Guid.NewGuid(),
                Title = activeTaskTitles[i],
                Status = TodoTaskStatus.NextAction,
                Contexts = new List<string> { i % 2 == 0 ? "@work" : "@home" },
                GoalIds = i switch
                {
                    0 => new List<Guid> { GoalBlogSeries },
                    1 => new List<Guid> { GoalPlatformRedesign },
                    2 => new List<Guid> { GoalMentoring },
                    3 => new List<Guid> { GoalPlatformRedesign },
                    _ => new List<Guid> { GoalPlatformRedesign }
                },
                SkillIds = i switch
                {
                    0 => new List<Guid> { SkillTechWriting },
                    1 or 4 => new List<Guid> { SkillSystemDesign },
                    2 => new List<Guid> { SkillLeadership },
                    _ => new List<Guid> { SkillKubernetes }
                },
                Priority = 2,
                IsSampleData = true,
                CreatedAt = now.AddDays(-rng.Next(1, 14)),
                ModifiedAt = now
            });
        }

        // ── Career Plan ──
        var plan = new CareerPlan
        {
            Id = PlanStaffEngineer,
            Title = "Path to Staff Engineer",
            Description = "A structured plan to grow from Senior Engineer to Staff Engineer through technical leadership, system design mastery, and organizational impact.",
            CurrentRole = "Senior Software Engineer",
            TargetRole = "Staff Engineer",
            Status = CareerPlanStatus.Active,
            StartDate = now.AddYears(-2),
            TargetDate = now.AddYears(3),
            Icon = "oi oi-map",
            Notes = "Focus areas: depth in system design, breadth in cloud architecture, and visible technical leadership through blogging and mentoring.",
            LinkedGoalIds = new List<Guid> { GoalAwsCert, GoalPlatformRedesign, GoalBlogSeries, GoalMentoring },
            LinkedSkillIds = new List<Guid> { SkillSystemDesign, SkillCloudArch, SkillTechWriting, SkillLeadership, SkillKubernetes },
            LinkedProjectIds = new List<Guid> { ProjPlatformRedesign, ProjBlogSeries, ProjAwsCertPrep },
            LinkedHabitIds = new List<Guid> { HabitStudy, HabitWrite, HabitMentor },
            Milestones = new List<CareerMilestone>
            {
                new()
                {
                    Id = Guid.NewGuid(), Title = "Lead system redesign project",
                    Category = MilestoneCategory.Project, Status = MilestoneStatus.Completed,
                    TargetDate = now.AddMonths(-18), CompletedDate = now.AddMonths(-16), SortOrder = 0,
                    LinkedSkillIds = new List<Guid> { SkillSystemDesign }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "AWS Solutions Architect certification",
                    Category = MilestoneCategory.Certification, Status = MilestoneStatus.Completed,
                    TargetDate = now.AddMonths(-12), CompletedDate = now.AddMonths(-10), SortOrder = 1,
                    LinkedSkillIds = new List<Guid> { SkillCloudArch }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Mentor 2 junior engineers",
                    Category = MilestoneCategory.Leadership, Status = MilestoneStatus.Completed,
                    TargetDate = now.AddMonths(-6), CompletedDate = now.AddMonths(-5), SortOrder = 2,
                    LinkedSkillIds = new List<Guid> { SkillLeadership }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Present at internal tech conference",
                    Category = MilestoneCategory.Networking, Status = MilestoneStatus.InProgress,
                    TargetDate = now.AddMonths(1), SortOrder = 3,
                    LinkedSkillIds = new List<Guid> { SkillTechWriting, SkillLeadership }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Design RFC for new platform architecture",
                    Category = MilestoneCategory.Skill, Status = MilestoneStatus.InProgress,
                    TargetDate = now.AddMonths(3), SortOrder = 4,
                    LinkedSkillIds = new List<Guid> { SkillSystemDesign, SkillCloudArch }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Lead cross-team initiative",
                    Category = MilestoneCategory.Leadership, Status = MilestoneStatus.NotStarted,
                    TargetDate = now.AddMonths(8), SortOrder = 5,
                    LinkedSkillIds = new List<Guid> { SkillLeadership }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Publish technical blog series",
                    Category = MilestoneCategory.Networking, Status = MilestoneStatus.NotStarted,
                    TargetDate = now.AddMonths(12), SortOrder = 6,
                    LinkedSkillIds = new List<Guid> { SkillTechWriting }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Kubernetes CKA certification",
                    Category = MilestoneCategory.Certification, Status = MilestoneStatus.NotStarted,
                    TargetDate = now.AddMonths(15), SortOrder = 7,
                    LinkedSkillIds = new List<Guid> { SkillKubernetes }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Drive org-wide architecture decision",
                    Category = MilestoneCategory.Role, Status = MilestoneStatus.NotStarted,
                    TargetDate = now.AddMonths(24), SortOrder = 8,
                    LinkedSkillIds = new List<Guid> { SkillSystemDesign, SkillLeadership }
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Promotion to Staff Engineer",
                    Category = MilestoneCategory.Role, Status = MilestoneStatus.NotStarted,
                    TargetDate = now.AddMonths(30), SortOrder = 9
                }
            },
            IsSampleData = true,
            CreatedAt = now.AddYears(-2),
            ModifiedAt = now
        };

        return new CareerExportData(
            Plans: new List<CareerPlan> { plan },
            Skills: skills,
            Goals: goals,
            Projects: projects,
            Habits: habits,
            HabitLogs: habitLogs,
            FocusSessions: focusSessions,
            WeeklySnapshots: weeklySnapshots,
            GrowthSnapshots: growthSnapshots,
            Tasks: tasks
        );
    }

    private List<GrowthContextSummary> GenerateDemoJourneyPeriods()
    {
        if (_demoData == null) return _journeyPeriods;

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = new List<GrowthContextSummary>();

        // Generate quarterly periods covering the last 12 months
        for (var q = 3; q >= 0; q--)
        {
            var periodEnd = now.AddMonths(-q * 3);
            var periodStart = periodEnd.AddMonths(-3);
            if (periodStart < now.AddMonths(-12)) periodStart = now.AddMonths(-12);

            var periodStartDt = periodStart.ToDateTime(TimeOnly.MinValue);
            var periodEndDt = periodEnd.ToDateTime(TimeOnly.MaxValue);

            // Find the closest demo snapshot for this period
            var closestSnapshot = _demoData.GrowthSnapshots
                .Where(s => s.SnapshotDate >= periodStart && s.SnapshotDate <= periodEnd)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefault();

            var previousSnapshot = closestSnapshot != null
                ? _demoData.GrowthSnapshots
                    .Where(s => s.SnapshotDate < closestSnapshot.SnapshotDate)
                    .OrderByDescending(s => s.SnapshotDate)
                    .FirstOrDefault()
                : null;

            var summary = new GrowthContextSummary
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                HasSnapshot = closestSnapshot != null,
                SnapshotId = closestSnapshot?.Id,
                SnapshotNotes = closestSnapshot?.Notes
            };

            if (closestSnapshot != null)
            {
                foreach (var (skillId, proficiency) in closestSnapshot.SkillProficiencies)
                {
                    var name = closestSnapshot.SkillNames.GetValueOrDefault(skillId, "Unknown");
                    int? previous = previousSnapshot?.SkillProficiencies.GetValueOrDefault(skillId);
                    summary.Skills.Add(new SkillDataPoint
                    {
                        SkillId = skillId,
                        Name = name,
                        Proficiency = proficiency,
                        PreviousProficiency = previous is null or 0 ? null : previous
                    });
                }

                foreach (var (goalId, progress) in closestSnapshot.GoalProgress)
                {
                    var title = closestSnapshot.GoalTitles.GetValueOrDefault(goalId, "Unknown");
                    int? previous = previousSnapshot?.GoalProgress.GetValueOrDefault(goalId);
                    summary.Goals.Add(new GoalDataPoint
                    {
                        GoalId = goalId,
                        Title = title,
                        ProgressPercent = progress,
                        PreviousProgressPercent = previous is null or 0 ? null : previous
                    });
                }

                summary.BalanceRatings = new Dictionary<string, int>(closestSnapshot.BalanceRatings);
            }

            // Tasks completed in period
            var completedTasks = _demoData.Tasks
                .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= periodStartDt && t.CompletedAt.Value <= periodEndDt)
                .ToList();
            summary.TasksCompleted = completedTasks.Count;
            summary.TasksByContext = completedTasks
                .SelectMany(t => t.Contexts).GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            // Habits
            var periodLogs = _demoData.HabitLogs.Where(l => l.Date >= periodStart && l.Date <= periodEnd).ToList();
            summary.HabitCompletionCount = periodLogs.Count(l => l.Completed);
            summary.HabitCompletionRate = periodLogs.Count > 0 ? (double)summary.HabitCompletionCount / periodLogs.Count : 0;

            // Focus
            var periodFocus = _demoData.FocusSessions.Where(f => f.StartedAt >= periodStartDt && f.StartedAt <= periodEndDt).ToList();
            summary.FocusSessionCount = periodFocus.Count;
            summary.FocusMinutes = periodFocus.Sum(f => f.DurationMinutes);

            // Weekly
            var periodWeekly = _demoData.WeeklySnapshots
                .Where(w => w.WeekStart >= periodStart && w.WeekStart <= periodEnd && w.ReviewCompleted).ToList();
            if (periodWeekly.Count > 0)
            {
                var energyVals = periodWeekly.Where(w => w.AverageEnergyLevel.HasValue).Select(w => (double)w.AverageEnergyLevel!.Value).ToList();
                var moodVals = periodWeekly.Where(w => w.AverageMoodLevel.HasValue).Select(w => (double)w.AverageMoodLevel!.Value).ToList();
                var wlbVals = periodWeekly.Where(w => w.WorkLifeBalanceRating.HasValue).Select(w => (double)w.WorkLifeBalanceRating!.Value).ToList();
                summary.AverageEnergy = energyVals.Count > 0 ? Math.Round(energyVals.Average(), 1) : null;
                summary.AverageMood = moodVals.Count > 0 ? Math.Round(moodVals.Average(), 1) : null;
                summary.AverageWorkLifeBalance = wlbVals.Count > 0 ? Math.Round(wlbVals.Average(), 1) : null;
            }

            // Milestones
            var completedMilestones = _demoData.Plans
                .SelectMany(p => p.Milestones)
                .Where(m => m.CompletedDate.HasValue && m.CompletedDate.Value >= periodStartDt && m.CompletedDate.Value <= periodEndDt)
                .ToList();
            summary.MilestonesCompletedInPeriod = completedMilestones.Count;
            summary.MilestonesCompletedNames = completedMilestones.Select(m => m.Title).ToList();

            // Active projects
            summary.ActiveProjectNames = _demoData.Projects.Where(p => p.Status == ProjectStatus.Active).Select(p => p.Name).ToList();

            periods.Add(summary);
        }

        // Merge with real periods (prefer demo periods for overlapping ranges, append non-overlapping real ones)
        return periods;
    }
}
