namespace SelfOrganizer.App.Services;

/// <summary>
/// Service for broadcasting keyboard navigation events to subscribed components.
/// Components that display lists can subscribe to receive j/k navigation events.
/// </summary>
public class KeyboardNavigationService
{
    /// <summary>
    /// Fired when j (down) or k (up) is pressed for list navigation
    /// </summary>
    public event Action<string>? OnNavigate;

    /// <summary>
    /// Fired when Enter is pressed to select/open the current item
    /// </summary>
    public event Action? OnSelect;

    /// <summary>
    /// Fired when Space is pressed to toggle completion on current item
    /// </summary>
    public event Action? OnToggle;

    /// <summary>
    /// Fired when 'e' is pressed to edit the current item
    /// </summary>
    public event Action? OnEdit;

    /// <summary>
    /// Fired when 'd' is pressed to delete the current item
    /// </summary>
    public event Action? OnDelete;

    public void Navigate(string direction)
    {
        OnNavigate?.Invoke(direction);
    }

    public void Select()
    {
        OnSelect?.Invoke();
    }

    public void Toggle()
    {
        OnToggle?.Invoke();
    }

    public void Edit()
    {
        OnEdit?.Invoke();
    }

    public void Delete()
    {
        OnDelete?.Invoke();
    }
}
