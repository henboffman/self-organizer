using Microsoft.JSInterop;

namespace SelfOrganizer.App.Services;

public interface IPlatformService
{
    Task<bool> IsMacAsync();
    Task<string> GetPlatformAsync();
    Task<string> GetModifierKeySymbolAsync();
    bool IsModifierKeyPressed(bool ctrlKey, bool metaKey);
}

public class PlatformService : IPlatformService
{
    private readonly IJSRuntime _jsRuntime;
    private bool? _isMacCached;

    public PlatformService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsMacAsync()
    {
        if (_isMacCached.HasValue)
            return _isMacCached.Value;

        try
        {
            _isMacCached = await _jsRuntime.InvokeAsync<bool>("platformInterop.isMac");
            return _isMacCached.Value;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetPlatformAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("platformInterop.getPlatform");
        }
        catch
        {
            return "unknown";
        }
    }

    public async Task<string> GetModifierKeySymbolAsync()
    {
        try
        {
            // Also cache isMac while we're at it
            if (!_isMacCached.HasValue)
            {
                _isMacCached = await _jsRuntime.InvokeAsync<bool>("platformInterop.isMac");
            }
            return await _jsRuntime.InvokeAsync<string>("platformInterop.getModifierKeySymbol");
        }
        catch
        {
            return "Ctrl";
        }
    }

    /// <summary>
    /// Check if the appropriate modifier key is pressed based on platform.
    /// On Mac, checks metaKey (Cmd). On other platforms, checks ctrlKey.
    /// </summary>
    public bool IsModifierKeyPressed(bool ctrlKey, bool metaKey)
    {
        // If we know it's Mac, use metaKey. Otherwise use ctrlKey.
        // Note: metaKey is true when Cmd is pressed on Mac
        if (_isMacCached == true)
            return metaKey;

        // On Windows/Linux, use Ctrl. Also fallback to Ctrl if platform not yet detected.
        return ctrlKey;
    }
}
