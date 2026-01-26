using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SelfOrganizer.App.Components.Shared;

public partial class Modal
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Modal size: "modal-sm", "modal-lg", "modal-xl", or empty for default
    /// </summary>
    [Parameter]
    public string Size { get; set; } = "";

    /// <summary>
    /// Handle keyboard events for accessibility - close modal on Escape key
    /// </summary>
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape" && OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }
}
