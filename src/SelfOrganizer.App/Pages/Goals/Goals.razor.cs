using Microsoft.AspNetCore.Components;
using SelfOrganizer.App.Services;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Pages.Goals;

public partial class Goals : IDisposable
{
    private List<Goal> _allGoals = new();
    private List<Goal> _filteredGoals = new();
    private bool _isLoading = true;
    private string _statusFilter = "active";

    // Linked tasks for each goal
    private Dictionary<Guid, List<TodoTask>> _goalTasks = new();

    // Counts for badges
    private int _allCount;
    private int _activeCount;
    private int _onHoldCount;
    private int _completedCount;

    // Modal state
    private bool _showDetailModal = false;
    private bool _showDeleteConfirm = false;
    private bool _showCompleteConfirm = false;
    private bool _showBulkImportModal = false;

    // View state
    private Goal? _viewingGoal;
    private Goal? _deletingGoal;
    private Goal? _completingGoal;

    protected override async Task OnInitializedAsync()
    {
        DataChangeNotification.OnDataChanged += HandleDataChanged;
        await LoadGoals();
    }

    private async void HandleDataChanged()
    {
        await InvokeAsync(async () =>
        {
            await LoadGoals();
            StateHasChanged();
        });
    }

    private async Task LoadGoals()
    {
        _isLoading = true;
        try
        {
            _allGoals = (await GoalService.GetAllAsync())
                .Where(g => g.Status != GoalStatus.Archived)
                .ToList();

            // Load linked tasks for each goal
            _goalTasks.Clear();
            foreach (var goal in _allGoals)
            {
                var tasks = (await GoalService.GetLinkedTasksAsync(goal.Id)).ToList();
                _goalTasks[goal.Id] = tasks;
            }

            // Update counts
            _allCount = _allGoals.Count;
            _activeCount = _allGoals.Count(g => g.Status == GoalStatus.Active);
            _onHoldCount = _allGoals.Count(g => g.Status == GoalStatus.OnHold);
            _completedCount = _allGoals.Count(g => g.Status == GoalStatus.Completed);

            ApplyFilter();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private List<TodoTask> GetLinkedTasks(Guid goalId)
    {
        return _goalTasks.TryGetValue(goalId, out var tasks) ? tasks : new List<TodoTask>();
    }

    private void FilterByStatus(string status)
    {
        _statusFilter = status;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredGoals = _statusFilter switch
        {
            "active" => _allGoals.Where(g => g.Status == GoalStatus.Active).ToList(),
            "onhold" => _allGoals.Where(g => g.Status == GoalStatus.OnHold).ToList(),
            "completed" => _allGoals.Where(g => g.Status == GoalStatus.Completed).ToList(),
            _ => _allGoals.ToList()
        };
    }

    private string GetStatusLabel() => _statusFilter switch
    {
        "active" => "active",
        "onhold" => "on hold",
        "completed" => "completed",
        _ => ""
    };

    private string GetEmptyMessage() => _statusFilter switch
    {
        "active" => "Set meaningful goals to guide your work and track progress.",
        "onhold" => "No goals are currently on hold.",
        "completed" => "You haven't completed any goals yet. Keep working!",
        _ => "No goals found."
    };

    // Navigation Methods
    private void NavigateToNewGoal()
    {
        NavigationManager.NavigateTo("/goals/new");
    }

    private void NavigateToEditGoal(Guid goalId)
    {
        NavigationManager.NavigateTo($"/goals/{goalId}/edit");
    }

    private void ShowDetailModal(Goal goal)
    {
        _viewingGoal = goal;
        _showDetailModal = true;
    }

    private void CloseDetailModal()
    {
        _showDetailModal = false;
        _viewingGoal = null;
    }

    private async Task OnGoalUpdatedFromDetail()
    {
        // Reload goals when AI updates a goal from the detail view
        await LoadGoals();
        StateHasChanged();
    }

    // CRUD Operations
    private void CompleteGoal(Goal goal)
    {
        _completingGoal = goal;
        _showCompleteConfirm = true;
    }

    private async Task ConfirmComplete()
    {
        if (_completingGoal != null)
        {
            await GoalService.CompleteAsync(_completingGoal.Id);
            _completingGoal = null;
        }
        _showCompleteConfirm = false;
        DataChangeNotification.NotifyDataChanged();
        await LoadGoals();
    }

    private void DeleteGoal(Goal goal)
    {
        _deletingGoal = goal;
        _showDeleteConfirm = true;
    }

    private async Task ConfirmDelete()
    {
        if (_deletingGoal != null)
        {
            await GoalService.DeleteAsync(_deletingGoal.Id);
            _deletingGoal = null;
        }
        _showDeleteConfirm = false;
        DataChangeNotification.NotifyDataChanged();
        await LoadGoals();
    }

    // Bulk Import
    private void ShowBulkImportModal()
    {
        _showBulkImportModal = true;
    }

    private void CloseBulkImportModal()
    {
        _showBulkImportModal = false;
    }

    private async Task HandleBulkImport(List<Goal> goals)
    {
        foreach (var goal in goals)
        {
            await GoalService.CreateAsync(goal);
        }

        DataChangeNotification.NotifyDataChanged();
        await LoadGoals();
    }

    public void Dispose()
    {
        DataChangeNotification.OnDataChanged -= HandleDataChanged;
    }
}
