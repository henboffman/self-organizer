using Microsoft.AspNetCore.Components;
using SelfOrganizer.App.Services;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Layout;

public partial class NavMenu : IDisposable
{
    [Inject]
    private ICaptureService CaptureService { get; set; } = default!;

    [Inject]
    private ITaskService TaskService { get; set; } = default!;

    [Inject]
    private IProjectService ProjectService { get; set; } = default!;

    [Inject]
    private ICalendarService CalendarService { get; set; } = default!;

    [Inject]
    private IGoalService GoalService { get; set; } = default!;

    [Inject]
    private IDataChangeNotificationService DataChangeNotification { get; set; } = default!;

    private bool collapseNavMenu = true;

    // Count fields for sidebar badges
    private int _inboxCount = 0;
    private int _totalTasksCount = 0;
    private int _projectsCount = 0;
    private int _todayEventsCount = 0;
    private int _goalsCount = 0;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to data change notifications
        DataChangeNotification.OnDataChanged += HandleDataChanged;
        await RefreshCounts();
    }

    private async void HandleDataChanged()
    {
        try
        {
            await RefreshCounts();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            // Log error - async void methods swallow exceptions so we need to handle them here
            Console.Error.WriteLine($"Error refreshing nav menu counts: {ex.Message}");
        }
    }

    private async Task RefreshCounts()
    {
        try
        {
            // Load all counts in parallel for better performance
            var capturesTask = CaptureService.GetUnprocessedCountAsync();
            var inboxTasksTask = TaskService.GetByStatusAsync(TodoTaskStatus.Inbox);
            var nextActionsTask = TaskService.GetByStatusAsync(TodoTaskStatus.NextAction);
            var waitingForTask = TaskService.GetWaitingForAsync();
            var scheduledTask = TaskService.GetScheduledAsync();
            var projectsTask = ProjectService.GetActiveAsync();
            var eventsTask = CalendarService.GetEventsForDateAsync(DateOnly.FromDateTime(DateTime.Today));
            var goalsTask = GoalService.GetActiveGoalsAsync();

            await Task.WhenAll(capturesTask, inboxTasksTask, nextActionsTask, waitingForTask, scheduledTask, projectsTask, eventsTask, goalsTask);

            // Inbox count = unprocessed captures + tasks with Inbox status
            var capturesCount = await capturesTask;
            var inboxTasksCount = (await inboxTasksTask).Count();
            _inboxCount = capturesCount + inboxTasksCount;

            // Total active tasks (excluding completed and inbox)
            var nextActions = (await nextActionsTask).Count();
            var waitingFor = (await waitingForTask).Count();
            var scheduled = (await scheduledTask).Count();
            _totalTasksCount = nextActions + waitingFor + scheduled;

            _projectsCount = (await projectsTask).Count();
            _todayEventsCount = (await eventsTask).Count();
            _goalsCount = (await goalsTask).Count();
        }
        catch
        {
            // Reset to 0 on error
            _inboxCount = 0;
            _totalTasksCount = 0;
            _projectsCount = 0;
            _todayEventsCount = 0;
            _goalsCount = 0;
        }
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    public void Dispose()
    {
        DataChangeNotification.OnDataChanged -= HandleDataChanged;
    }
}
