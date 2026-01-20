using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

/// <summary>
/// Service for managing sample/demo data created during onboarding
/// </summary>
public class SampleDataService : ISampleDataService
{
    private readonly IRepository<Goal> _goalRepository;
    private readonly IRepository<TodoTask> _taskRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    public SampleDataService(
        IRepository<Goal> goalRepository,
        IRepository<TodoTask> taskRepository,
        IRepository<Project> projectRepository,
        IRepository<UserPreferences> preferencesRepository)
    {
        _goalRepository = goalRepository;
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _preferencesRepository = preferencesRepository;
    }

    public async Task SeedSampleDataAsync()
    {
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
        if (prefs?.SampleDataSeeded == true)
            return; // Already seeded

        // 1. Create sample project
        var project = new Project
        {
            Name = "Explore Self-Organizer",
            Description = "A sample project to help you learn how Self-Organizer works. Complete these tasks to become familiar with the app's features.",
            DesiredOutcome = "Feel comfortable using the app and understand core features",
            Status = ProjectStatus.Active,
            Category = "Learning",
            Color = "#6366f1", // Indigo
            Priority = 1,
            IsSampleData = true
        };
        project = await _projectRepository.AddAsync(project);

        // 2. Create sample goal
        var goal = new Goal
        {
            Title = "Get to know Self-Organizer",
            Description = "Learn the core features and workflows to boost your productivity with GTD methodology.",
            DesiredOutcome = "Master capturing, organizing, and reviewing tasks",
            SuccessCriteria = "Complete all onboarding tasks and feel confident using the app",
            Status = GoalStatus.Active,
            Category = GoalCategory.Learning,
            Timeframe = GoalTimeframe.Week,
            Priority = 1,
            StartDate = DateTime.UtcNow,
            TargetDate = DateTime.UtcNow.AddDays(7),
            IsSampleData = true
        };
        goal.LinkedProjectIds.Add(project.Id);
        goal = await _goalRepository.AddAsync(goal);

        // 3. Create sample tasks (linked to project and goal)
        var sampleTasks = new[]
        {
            new TodoTask
            {
                Title = "Try the Quick Capture feature",
                Description = "Click the '+ Quick Capture' button in the top-right corner of any page, or use Cmd+Enter (Ctrl+Enter on Windows) to instantly capture a thought. You can also find 'Quick Capture' in the sidebar under CAPTURE.",
                Status = TodoTaskStatus.NextAction,
                Priority = 1,
                EstimatedMinutes = 2,
                ProjectId = project.Id,
                Category = "Learning",
                IsSampleData = true
            },
            new TodoTask
            {
                Title = "Process an item from your Inbox",
                Description = "Click 'Inbox' in the sidebar under CAPTURE. Practice the GTD workflow: Is it actionable? What's the next physical action? Use the action buttons to organize items.",
                Status = TodoTaskStatus.NextAction,
                Priority = 2,
                EstimatedMinutes = 5,
                ProjectId = project.Id,
                Category = "Learning",
                IsSampleData = true
            },
            new TodoTask
            {
                Title = "Explore the Goals section",
                Description = "Click 'Goals' in the sidebar under WORK. See how this sample goal tracks progress based on linked tasks. Click the '+ New Goal' button to create your own!",
                Status = TodoTaskStatus.NextAction,
                Priority = 2,
                EstimatedMinutes = 5,
                ProjectId = project.Id,
                Category = "Learning",
                IsSampleData = true
            },
            new TodoTask
            {
                Title = "Check out Life Balance in the sidebar",
                Description = "Click 'Life Balance' in the sidebar under MORE. Rate yourself in each life dimension to see your balance wheel and get personalized insights.",
                Status = TodoTaskStatus.NextAction,
                Priority = 3,
                EstimatedMinutes = 10,
                ProjectId = project.Id,
                Category = "Learning",
                IsSampleData = true
            },
            new TodoTask
            {
                Title = "Hide this sample data when ready",
                Description = "Return to the Dashboard (click 'Self Organizer' logo or press 'g' then 'h'). Look for the 'Sample data visible' toggle near the top. Click 'Hide' when you're ready to work with your own data!",
                Status = TodoTaskStatus.NextAction,
                Priority = 3,
                EstimatedMinutes = 1,
                ProjectId = project.Id,
                Category = "Learning",
                IsSampleData = true
            }
        };

        foreach (var task in sampleTasks)
        {
            task.GoalIds.Add(goal.Id);
            var created = await _taskRepository.AddAsync(task);
            goal.LinkedTaskIds.Add(created.Id);
        }

        // Update goal with linked task IDs
        await _goalRepository.UpdateAsync(goal);

        // Mark as seeded
        if (prefs != null)
        {
            prefs.SampleDataSeeded = true;
            await _preferencesRepository.UpdateAsync(prefs);
        }
    }

    public async Task DeleteAllSampleDataAsync()
    {
        // Delete sample tasks
        var tasks = await _taskRepository.QueryAsync(t => t.IsSampleData);
        foreach (var task in tasks)
            await _taskRepository.DeleteAsync(task.Id);

        // Delete sample projects
        var projects = await _projectRepository.QueryAsync(p => p.IsSampleData);
        foreach (var project in projects)
            await _projectRepository.DeleteAsync(project.Id);

        // Delete sample goals
        var goals = await _goalRepository.QueryAsync(g => g.IsSampleData);
        foreach (var goal in goals)
            await _goalRepository.DeleteAsync(goal.Id);

        // Reset preferences
        var prefs = (await _preferencesRepository.GetAllAsync()).FirstOrDefault();
        if (prefs != null)
        {
            prefs.SampleDataSeeded = false;
            prefs.ShowSampleData = true;
            await _preferencesRepository.UpdateAsync(prefs);
        }
    }

    public async Task<bool> HasSampleDataAsync()
    {
        var count = await _taskRepository.CountAsync(t => t.IsSampleData);
        return count > 0;
    }
}
