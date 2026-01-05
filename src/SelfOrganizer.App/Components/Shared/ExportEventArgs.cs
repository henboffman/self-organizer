namespace SelfOrganizer.App.Components.Shared;

public class ExportEventArgs
{
    public string EntityName { get; set; } = "";
    public string Format { get; set; } = "";
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemCount { get; set; }

    public void SetSuccess(int itemCount, string? message = null)
    {
        Success = true;
        ItemCount = itemCount;
        Message = message;
    }

    public void SetError(string errorMessage)
    {
        Success = false;
        ErrorMessage = errorMessage;
    }
}
