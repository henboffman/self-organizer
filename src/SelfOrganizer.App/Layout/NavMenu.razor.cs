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
    private IIdeaService IdeaService { get; set; } = default!;

    [Inject]
    private IDataChangeNotificationService DataChangeNotification { get; set; } = default!;

    private bool collapseNavMenu = true;

    // Count fields for sidebar badges
    private int _inboxCount = 0;
    private int _totalTasksCount = 0;
    private int _projectsCount = 0;
    private int _todayEventsCount = 0;
    private int _goalsCount = 0;
    private int _ideasCount = 0;

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
            await InvokeAsync(async () =>
            {
                await RefreshCounts();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed while handling data change
        }
        catch (Exception)
        {
            // Log error but don't crash - data will be stale until next refresh
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
            var ideasTask = IdeaService.GetActiveCountAsync();

            await Task.WhenAll(capturesTask, inboxTasksTask, nextActionsTask, waitingForTask, scheduledTask, projectsTask, eventsTask, goalsTask, ideasTask);

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
            _ideasCount = await ideasTask;
        }
        catch
        {
            // Reset to 0 on error
            _inboxCount = 0;
            _totalTasksCount = 0;
            _projectsCount = 0;
            _todayEventsCount = 0;
            _goalsCount = 0;
            _ideasCount = 0;
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
