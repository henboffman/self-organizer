using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SelfOrganizer.App.Services;

namespace SelfOrganizer.App.Components.Shared;

public partial class ImportModal
{
    private const int MaxErrorsToShow = 20;
    private const int MaxPreviewRows = 5;

    private InputFile? _fileInput;
    private ImportState _state = ImportState.SelectFile;
    private bool _isDragOver;

    // File info
    private string _fileName = "";
    private string _fileSize = "";
    private string _fileContent = "";
    private ImportFormat _detectedFormat;

    // Validation/Preview
    private ImportValidationResult? _validationResult;
    private List<string[]> _previewData = new();

    // Results
    private bool _importSuccess;
    private bool _partialSuccess;
    private string _resultMessage = "";
    private List<ImportError> _errors = new();

    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public string EntityName { get; set; } = "Items";
    [Parameter] public bool ShowTemplateDownload { get; set; } = true;
    [Parameter] public string[]? RequiredColumns { get; set; }

    /// <summary>
    /// Callback when import is requested. The event args contain the file content and format.
    /// The parent component should perform the actual import and set the result.
    /// </summary>
    [Parameter] public EventCallback<ImportRequestEventArgs> OnImportRequested { get; set; }

    /// <summary>
    /// Callback to download a CSV template
    /// </summary>
    [Parameter] public EventCallback OnDownloadTemplate { get; set; }

    private bool CanImport => _state == ImportState.Preview &&
                              !string.IsNullOrEmpty(_fileContent) &&
                              (_validationResult?.IsValid ?? false);

    protected override void OnParametersSet()
    {
        if (!IsVisible)
        {
            ResetState();
        }
    }

    private void ResetState()
    {
        _state = ImportState.SelectFile;
        _isDragOver = false;
        _fileName = "";
        _fileSize = "";
        _fileContent = "";
        _validationResult = null;
        _previewData.Clear();
        _importSuccess = false;
        _partialSuccess = false;
        _resultMessage = "";
        _errors.Clear();
    }

    private void ResetToSelectFile()
    {
        _state = ImportState.SelectFile;
        _fileName = "";
        _fileSize = "";
        _fileContent = "";
        _validationResult = null;
        _previewData.Clear();
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null)
            return;

        await ProcessFile(file);
    }

    private async Task ProcessFile(IBrowserFile file)
    {
        _fileName = file.Name;
        _fileSize = FormatFileSize(file.Size);

        // Read file content
        using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
        using var reader = new StreamReader(stream);
        _fileContent = await reader.ReadToEndAsync();

        // Detect format
        _detectedFormat = ImportService.DetectFormat(_fileContent);

        if (_detectedFormat == ImportFormat.Unknown)
        {
            _errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "Could not detect file format. Please use a CSV or JSON file."
            });
            _state = ImportState.Complete;
            _importSuccess = false;
            _resultMessage = "Invalid file format";
            return;
        }

        // Validate and preview
        if (_detectedFormat == ImportFormat.Csv)
        {
            _previewData = ImportService.ParseCsvPreview(_fileContent, MaxPreviewRows);

            // Notify parent to validate headers
            var args = new ImportRequestEventArgs
            {
                Content = _fileContent,
                Format = _detectedFormat,
                FileName = _fileName,
                IsValidationOnly = true
            };
            await OnImportRequested.InvokeAsync(args);
            _validationResult = args.ValidationResult;
        }
        else
        {
            // JSON - just mark as valid
            _validationResult = new ImportValidationResult { IsValid = true };
        }

        _state = ImportState.Preview;
    }

    private void HandleDragOver()
    {
        _isDragOver = true;
    }

    private void HandleDragLeave()
    {
        _isDragOver = false;
    }

    private void HandleDrop()
    {
        _isDragOver = false;
        // File will be handled by InputFile's OnChange event
    }

    private async Task HandleImport()
    {
        _state = ImportState.Importing;
        StateHasChanged();

        var args = new ImportRequestEventArgs
        {
            Content = _fileContent,
            Format = _detectedFormat,
            FileName = _fileName,
            IsValidationOnly = false
        };

        await OnImportRequested.InvokeAsync(args);

        _importSuccess = args.Success && !args.Errors.Any();
        _partialSuccess = args.Success && args.Errors.Any();
        _errors = args.Errors;
        _resultMessage = args.ResultMessage ?? GetDefaultResultMessage(args);

        _state = ImportState.Complete;
    }

    private async Task HandleDownloadTemplate()
    {
        if (OnDownloadTemplate.HasDelegate)
        {
            await OnDownloadTemplate.InvokeAsync();
        }
    }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private string GetDefaultResultMessage(ImportRequestEventArgs args)
    {
        if (args.Success && !args.Errors.Any())
            return $"Successfully imported {args.ImportedCount} {EntityName.ToLower()}.";

        if (args.Success && args.Errors.Any())
            return $"Imported {args.ImportedCount} of {args.TotalCount} {EntityName.ToLower()}. {args.Errors.Count} errors occurred.";

        return $"Import failed. {args.Errors.Count} errors found.";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private static string TruncateCell(string value, int maxLength = 30)
    {
        if (value.Length <= maxLength)
            return value;
        return value[..(maxLength - 3)] + "...";
    }

    private enum ImportState
    {
        SelectFile,
        Preview,
        Importing,
        Complete
    }
}

public class ImportRequestEventArgs
{
    public string Content { get; set; } = "";
    public ImportFormat Format { get; set; }
    public string FileName { get; set; } = "";
    public bool IsValidationOnly { get; set; }

    // Set by parent component during validation
    public ImportValidationResult? ValidationResult { get; set; }

    // Set by parent component after import
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int TotalCount { get; set; }
    public string? ResultMessage { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}
