using Microsoft.AspNetCore.Components;
using SelfOrganizer.App.Services;

namespace SelfOrganizer.App.Components.Shared;

public partial class ThemeToggle : IDisposable
{
    [Inject]
    private IThemeService ThemeService { get; set; } = default!;

    private bool _isDark;

    protected override async Task OnInitializedAsync()
    {
        var theme = await ThemeService.GetThemeAsync();
        _isDark = theme == "dark";
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private async Task ToggleTheme()
    {
        var newTheme = await ThemeService.ToggleThemeAsync();
        _isDark = newTheme == "dark";
    }

    private void OnThemeChanged(string theme)
    {
        _isDark = theme == "dark";
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
    }
}
