using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace SelfOrganizer.App.Services;

public interface IExportService
{
    /// <summary>
    /// Export data to CSV format
    /// </summary>
    Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string[]? columns = null);

    /// <summary>
    /// Export data to JSON format
    /// </summary>
    Task<string> ExportToJsonAsync<T>(IEnumerable<T> data, bool prettyPrint = true);

    /// <summary>
    /// Generate a blank CSV template with headers only
    /// </summary>
    Task<string> GenerateCsvTemplateAsync<T>(string[]? columns = null, T? exampleRow = default);

    /// <summary>
    /// Trigger a file download in the browser
    /// </summary>
    Task<ExportResult> DownloadFileAsync(string filename, string content, string contentType);

    /// <summary>
    /// Export and download as CSV
    /// </summary>
    Task<ExportResult> ExportAndDownloadCsvAsync<T>(IEnumerable<T> data, string filenamePrefix, string[]? columns = null);

    /// <summary>
    /// Export and download as JSON
    /// </summary>
    Task<ExportResult> ExportAndDownloadJsonAsync<T>(IEnumerable<T> data, string filenamePrefix);

    /// <summary>
    /// Download a CSV template
    /// </summary>
    Task<ExportResult> DownloadTemplateAsync<T>(string filenamePrefix, string[]? columns = null, T? exampleRow = default);
}

public class ExportResult
{
    public bool Success { get; set; }
    public string? Filename { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemCount { get; set; }

    public static ExportResult Succeeded(string filename, int itemCount) => new()
    {
        Success = true,
        Filename = filename,
        ItemCount = itemCount
    };

    public static ExportResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

public class ExportService : IExportService
{
    private readonly IJSRuntime _jsRuntime;

    public ExportService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Task<string> ExportToCsvAsync<T>(IEnumerable<T> data, string[]? columns = null)
    {
        var items = data.ToList();
        if (!items.Any())
            return Task.FromResult(string.Empty);

        var properties = GetExportProperties<T>(columns);
        var sb = new StringBuilder();

        // Write header row
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));

        // Write data rows
        foreach (var item in items)
        {
            var values = properties.Select(p => FormatCsvValue(p.GetValue(item)));
            sb.AppendLine(string.Join(",", values));
        }

        return Task.FromResult(sb.ToString());
    }

    public Task<string> ExportToJsonAsync<T>(IEnumerable<T> data, bool prettyPrint = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(data, options);
        return Task.FromResult(json);
    }

    public Task<string> GenerateCsvTemplateAsync<T>(string[]? columns = null, T? exampleRow = default)
    {
        var properties = GetExportProperties<T>(columns);
        var sb = new StringBuilder();

        // Write header row
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));

        // Write example row if provided
        if (exampleRow != null)
        {
            var values = properties.Select(p => FormatCsvValue(p.GetValue(exampleRow)));
            sb.AppendLine(string.Join(",", values));
        }
        else
        {
            // Write placeholder values based on type
            var placeholders = properties.Select(p => GetPlaceholderValue(p));
            sb.AppendLine(string.Join(",", placeholders));
        }

        return Task.FromResult(sb.ToString());
    }

    public async Task<ExportResult> DownloadFileAsync(string filename, string content, string contentType)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<JsonElement>("fileInterop.downloadText", filename, contentType, content);

            if (result.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                return ExportResult.Succeeded(filename, 0);
            }

            var error = result.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : "Unknown error";
            return ExportResult.Failed($"Download failed: {error}");
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"Download failed: {ex.Message}");
        }
    }

    public async Task<ExportResult> ExportAndDownloadCsvAsync<T>(IEnumerable<T> data, string filenamePrefix, string[]? columns = null)
    {
        try
        {
            var items = data.ToList();
            var csv = await ExportToCsvAsync(items, columns);

            if (string.IsNullOrEmpty(csv))
            {
                return ExportResult.Failed("No data to export");
            }

            var filename = $"{filenamePrefix}-{DateTime.Now:yyyy-MM-dd}.csv";
            var result = await DownloadFileAsync(filename, csv, "text/csv");
            result.ItemCount = items.Count;
            return result;
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    public async Task<ExportResult> ExportAndDownloadJsonAsync<T>(IEnumerable<T> data, string filenamePrefix)
    {
        try
        {
            var items = data.ToList();
            var json = await ExportToJsonAsync(items);

            var filename = $"{filenamePrefix}-{DateTime.Now:yyyy-MM-dd}.json";
            var result = await DownloadFileAsync(filename, json, "application/json");
            result.ItemCount = items.Count;
            return result;
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    public async Task<ExportResult> DownloadTemplateAsync<T>(string filenamePrefix, string[]? columns = null, T? exampleRow = default)
    {
        try
        {
            var template = await GenerateCsvTemplateAsync(columns, exampleRow);
            var filename = $"{filenamePrefix}-template.csv";
            return await DownloadFileAsync(filename, template, "text/csv");
        }
        catch (Exception ex)
        {
            return ExportResult.Failed($"Template download failed: {ex.Message}");
        }
    }

    private static List<PropertyInfo> GetExportProperties<T>(string[]? columns)
    {
        var type = typeof(T);
        var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        if (columns == null || columns.Length == 0)
        {
            // Exclude common internal properties
            var excludedProps = new[] { "Id", "CreatedAt", "ModifiedAt" };
            return allProperties.Where(p => !excludedProps.Contains(p.Name)).ToList();
        }

        // Return only requested columns in order
        return columns
            .Select(c => allProperties.FirstOrDefault(p => p.Name.Equals(c, StringComparison.OrdinalIgnoreCase)))
            .Where(p => p != null)
            .Cast<PropertyInfo>()
            .ToList();
    }

    private static string FormatCsvValue(object? value)
    {
        if (value == null)
            return "";

        return value switch
        {
            DateTime dt => EscapeCsvField(dt.ToString("yyyy-MM-dd HH:mm:ss")),
            DateOnly d => EscapeCsvField(d.ToString("yyyy-MM-dd")),
            TimeOnly t => EscapeCsvField(t.ToString("HH:mm")),
            TimeSpan ts => EscapeCsvField(ts.ToString(@"hh\:mm")),
            bool b => b ? "true" : "false",
            IEnumerable<string> list => EscapeCsvField(string.Join(";", list)),
            IEnumerable<Guid> guids => EscapeCsvField(string.Join(";", guids)),
            Enum e => EscapeCsvField(e.ToString()),
            _ => EscapeCsvField(value.ToString() ?? "")
        };
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Escape if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private static string GetPlaceholderValue(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(string))
            return EscapeCsvField($"Example {property.Name}");
        if (type == typeof(int))
            return "1";
        if (type == typeof(bool))
            return "false";
        if (type == typeof(DateTime))
            return DateTime.Now.ToString("yyyy-MM-dd");
        if (type == typeof(DateOnly))
            return DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        if (type == typeof(TimeOnly))
            return "09:00";
        if (type == typeof(TimeSpan))
            return "09:00";
        if (type == typeof(Guid))
            return "";
        if (type.IsEnum)
            return Enum.GetNames(type).FirstOrDefault() ?? "";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return "";

        return "";
    }
}
