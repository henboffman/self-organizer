using Microsoft.JSInterop;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Service for managing application theme (light/dark mode)
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme
    /// </summary>
    Task<string> GetThemeAsync();

    /// <summary>
    /// Sets the theme
    /// </summary>
    Task SetThemeAsync(string theme);

    /// <summary>
    /// Toggles between light and dark theme
    /// </summary>
    Task<string> ToggleThemeAsync();

    /// <summary>
    /// Clears stored preference and reverts to system theme
    /// </summary>
    Task<string> UseSystemThemeAsync();

    /// <summary>
    /// Event fired when theme changes
    /// </summary>
    event Action<string>? ThemeChanged;
}

public class ThemeService : IThemeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<ThemeService>? _dotNetRef;
    private string _currentTheme = "light";

    public event Action<string>? ThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetThemeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeInterop.getTheme");
            return _currentTheme;
        }
        catch
        {
            return "light";
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("themeInterop.setTheme", theme);
            _currentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
        catch
        {
            // Silently fail if JS interop not available
        }
    }

    public async Task<string> ToggleThemeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeInterop.toggleTheme");
            ThemeChanged?.Invoke(_currentTheme);
            return _currentTheme;
        }
        catch
        {
            return _currentTheme;
        }
    }

    public async Task<string> UseSystemThemeAsync()
    {
        try
        {
            _currentTheme = await _jsRuntime.InvokeAsync<string>("themeInterop.clearThemePreference");
            ThemeChanged?.Invoke(_currentTheme);
            return _currentTheme;
        }
        catch
        {
            return _currentTheme;
        }
    }

    /// <summary>
    /// Called from JavaScript when system preference changes
    /// </summary>
    [JSInvokable]
    public void OnSystemThemeChanged(string theme)
    {
        _currentTheme = theme;
        ThemeChanged?.Invoke(theme);
    }

    public async Task InitializeWatcherAsync()
    {
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("themeInterop.watchSystemPreference", _dotNetRef);
        }
        catch
        {
            // Silently fail if JS interop not available
        }
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        await ValueTask.CompletedTask;
    }
}
