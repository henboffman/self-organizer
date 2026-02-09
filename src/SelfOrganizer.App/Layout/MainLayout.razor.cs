using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SelfOrganizer.App.Services;
using SelfOrganizer.App.Services.Data;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Core.Interfaces;

namespace SelfOrganizer.App.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    private IIndexedDbService DbService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private KeyboardNavigationService KeyboardNavigation { get; set; } = default!;

    [Inject]
    private ICommandHistory CommandHistory { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private ErrorBoundary? _errorBoundary;
    private bool _showOnboarding = false;
    private bool _checkedOnboarding = false;
    private bool _showGlobalSearch = false;
    private bool _showKeyboardHelp = false;
    private bool _showQuickAdd = false;

    protected override async Task OnInitializedAsync()
    {
        await DbService.InitializeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize keyboard shortcuts
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("keyboardShortcuts.init", _dotNetRef);

            // Check auth state so the user's name appears in the top bar
            EntraAuthService.OnAuthStateChanged += HandleAuthStateChanged;
            await EntraAuthService.CheckSessionAsync();

            // Check onboarding status
            if (!_checkedOnboarding)
            {
                _checkedOnboarding = true;
                var prefs = (await PreferencesRepository.GetAllAsync()).FirstOrDefault();
                if (prefs == null || !prefs.OnboardingCompleted)
                {
                    // Before showing the wizard, check if the user already has real data
                    // in IndexedDB (e.g., migrated from a previous version). If so, skip
                    // onboarding to avoid overwriting their data with sample tasks.
                    var existingTaskCount = await DbService.CountAsync("tasks");
                    var existingProjectCount = await DbService.CountAsync("projects");
                    if (existingTaskCount == 0 && existingProjectCount == 0)
                    {
                        _showOnboarding = true;
                        StateHasChanged();
                    }
                    else
                    {
                        // User has data — mark onboarding as completed so we don't check again
                        if (prefs == null)
                        {
                            prefs = new UserPreferences { OnboardingCompleted = true };
                            await PreferencesRepository.AddAsync(prefs);
                        }
                        else
                        {
                            prefs.OnboardingCompleted = true;
                            await PreferencesRepository.UpdateAsync(prefs);
                        }
                    }
                }
            }
        }
    }

    private void HandleAuthStateChanged(AuthState _)
    {
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnGlobalSearchShortcut()
    {
        _showGlobalSearch = true;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnQuickCaptureShortcut()
    {
        NavigationManager.NavigateTo("capture");
    }

    [JSInvokable]
    public void OnHelpShortcut()
    {
        _showKeyboardHelp = !_showKeyboardHelp;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnEscapeShortcut()
    {
        // Close any open overlays
        if (_showQuickAdd)
        {
            _showQuickAdd = false;
            StateHasChanged();
        }
        else if (_showKeyboardHelp)
        {
            _showKeyboardHelp = false;
            StateHasChanged();
        }
        else if (_showGlobalSearch)
        {
            _showGlobalSearch = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnNavigateShortcut(string route)
    {
        NavigationManager.NavigateTo(route);
    }

    [JSInvokable]
    public void OnNewItemShortcut()
    {
        // Navigate to quick capture for new items
        NavigationManager.NavigateTo("capture");
    }

    [JSInvokable]
    public async Task OnUndoShortcut()
    {
        if (!CommandHistory.CanUndo)
        {
            ToastService.ShowInfo("Nothing to undo");
            return;
        }

        try
        {
            var description = CommandHistory.NextUndoDescription;
            await CommandHistory.UndoAsync();
            ToastService.ShowSuccess($"Undone: {description}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to undo: {ex.Message}");
        }
    }

    [JSInvokable]
    public async Task OnRedoShortcut()
    {
        if (!CommandHistory.CanRedo)
        {
            ToastService.ShowInfo("Nothing to redo");
            return;
        }

        try
        {
            var description = CommandHistory.NextRedoDescription;
            await CommandHistory.RedoAsync();
            ToastService.ShowSuccess($"Redone: {description}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to redo: {ex.Message}");
        }
    }

    [JSInvokable]
    public void OnQuickAddShortcut()
    {
        _showQuickAdd = true;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnListNavigateShortcut(string direction)
    {
        KeyboardNavigation.Navigate(direction);
    }

    [JSInvokable]
    public void OnListSelectShortcut()
    {
        KeyboardNavigation.Select();
    }

    [JSInvokable]
    public void OnListToggleShortcut()
    {
        KeyboardNavigation.Toggle();
    }

    [JSInvokable]
    public void OnListEditShortcut()
    {
        KeyboardNavigation.Edit();
    }

    [JSInvokable]
    public void OnListDeleteShortcut()
    {
        KeyboardNavigation.Delete();
    }

    private void OpenGlobalSearch()
    {
        _showGlobalSearch = true;
    }

    private void OnGlobalSearchClosed()
    {
        _showGlobalSearch = false;
    }

    private void OnOnboardingCompleted()
    {
        _showOnboarding = false;
    }

    private void OnKeyboardHelpClosed()
    {
        _showKeyboardHelp = false;
    }

    private void OnQuickAddClosed()
    {
        _showQuickAdd = false;
    }

    private void RecoverFromError()
    {
        // Reset the error boundary to allow re-rendering
        _errorBoundary?.Recover();
    }

    public void Dispose()
    {
        EntraAuthService.OnAuthStateChanged -= HandleAuthStateChanged;
        _dotNetRef?.Dispose();
        try
        {
            // Fire and forget cleanup
            _ = JSRuntime.InvokeVoidAsync("keyboardShortcuts.dispose");
        }
        catch
        {
            // Ignore errors during disposal
        }
    }
}
