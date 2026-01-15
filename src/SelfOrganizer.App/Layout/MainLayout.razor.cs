using Microsoft.AspNetCore.Components;
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

    private DotNetObjectReference<MainLayout>? _dotNetRef;
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

            // Check onboarding status
            if (!_checkedOnboarding)
            {
                _checkedOnboarding = true;
                var prefs = (await PreferencesRepository.GetAllAsync()).FirstOrDefault();
                if (prefs == null || !prefs.OnboardingCompleted)
                {
                    _showOnboarding = true;
                    StateHasChanged();
                }
            }
        }
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
        NavigationManager.NavigateTo("/capture");
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
        NavigationManager.NavigateTo("/capture");
    }

    [JSInvokable]
    public void OnUndoShortcut()
    {
        // TODO: Implement undo functionality
        Console.WriteLine("Undo shortcut triggered");
    }

    [JSInvokable]
    public void OnRedoShortcut()
    {
        // TODO: Implement redo functionality
        Console.WriteLine("Redo shortcut triggered");
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

    public void Dispose()
    {
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
