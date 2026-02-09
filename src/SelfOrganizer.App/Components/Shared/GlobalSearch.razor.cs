using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Components.Shared;

public partial class GlobalSearch : IDisposable
{
    [Inject]
    private ISearchService SearchService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private string _searchQuery = string.Empty;
    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                _ = DebounceSearchAsync();
            }
        }
    }

    private SearchResults? _searchResults;
    private IEnumerable<QuickAction> _quickActions = Enumerable.Empty<QuickAction>();
    private int _selectedIndex = 0;
    private bool _isSearching;
    private CancellationTokenSource? _debounceTokenSource;
    private ElementReference _searchInput;
    private const int DebounceDelayMs = 300;

    protected override void OnInitialized()
    {
        _quickActions = SearchService.GetQuickActions();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsVisible)
        {
            try
            {
                await _searchInput.FocusAsync();
            }
            catch
            {
                // Focus may fail if element not yet rendered
            }
        }
    }

    private async Task DebounceSearchAsync()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(DebounceDelayMs, _debounceTokenSource.Token);
            await PerformSearchAsync();
        }
        catch (TaskCanceledException)
        {
            // Debounce canceled, new search incoming
        }
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _searchResults = null;
            _selectedIndex = 0;
            StateHasChanged();
            return;
        }

        _isSearching = true;
        StateHasChanged();

        try
        {
            _searchResults = await SearchService.SearchAsync(_searchQuery);
            _selectedIndex = 0;
        }
        finally
        {
            _isSearching = false;
            StateHasChanged();
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            Close();
        }
    }

    private void HandleInputKeyDown(KeyboardEventArgs e)
    {
        var maxIndex = GetMaxIndex();

        switch (e.Key)
        {
            case "ArrowDown":
                _selectedIndex = (_selectedIndex + 1) % (maxIndex + 1);
                break;

            case "ArrowUp":
                _selectedIndex = _selectedIndex <= 0 ? maxIndex : _selectedIndex - 1;
                break;

            case "Enter":
                SelectCurrentItem();
                break;

            case "Escape":
                Close();
                break;
        }
    }

    private int GetMaxIndex()
    {
        if (_searchResults?.Results.Count > 0)
        {
            return _searchResults.Results.Count - 1;
        }
        else if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            return _quickActions.Count() - 1;
        }
        return 0;
    }

    private void SelectCurrentItem()
    {
        if (_searchResults?.Results.Count > 0 && _selectedIndex < _searchResults.Results.Count)
        {
            SelectResult(_searchResults.Results[_selectedIndex]);
        }
        else if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            var quickActionsList = _quickActions.ToList();
            if (_selectedIndex < quickActionsList.Count)
            {
                ExecuteQuickAction(quickActionsList[_selectedIndex]);
            }
        }
    }

    private void SelectResult(SearchResult result)
    {
        if (!string.IsNullOrEmpty(result.NavigationUrl))
        {
            NavigationManager.NavigateTo(result.NavigationUrl);
        }
        Close();
    }

    private void ExecuteQuickAction(QuickAction action)
    {
        var url = action.Id switch
        {
            "capture" => "capture",
            "inbox" => "inbox",
            "tasks" => "tasks",
            "projects" => "projects",
            "calendar" => "calendar",
            "review" => "review/daily",
            _ => ""
        };

        NavigationManager.NavigateTo(url);
        Close();
    }

    private void ClearSearch()
    {
        _searchQuery = string.Empty;
        _searchResults = null;
        _selectedIndex = 0;
    }

    private async Task CloseAsync()
    {
        _searchQuery = string.Empty;
        _searchResults = null;
        _selectedIndex = 0;

        await IsVisibleChanged.InvokeAsync(false);
        await OnClose.InvokeAsync();
    }

    private void Close()
    {
        // Fire-and-forget with proper error handling for sync callers
        _ = CloseAsync();
    }

    private static string GetTypeDisplayName(string type)
    {
        return type switch
        {
            "task" => "Tasks",
            "project" => "Projects",
            "event" => "Events",
            "capture" => "Inbox",
            "goal" => "Goals",
            _ => type
        };
    }

    public void Dispose()
    {
        _debounceTokenSource?.Cancel();
        _debounceTokenSource?.Dispose();
    }
}
