using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SelfOrganizer.App.Services;

public interface IImportService
{
    /// <summary>
    /// Import data from CSV content
    /// </summary>
    ImportResult<T> ImportFromCsv<T>(string csvContent) where T : new();

    /// <summary>
    /// Import data from JSON content
    /// </summary>
    ImportResult<T> ImportFromJson<T>(string jsonContent);

    /// <summary>
    /// Validate CSV headers against expected model properties
    /// </summary>
    ImportValidationResult ValidateCsvHeaders<T>(string csvContent, string[]? requiredColumns = null);

    /// <summary>
    /// Detect the file format from content
    /// </summary>
    ImportFormat DetectFormat(string content);

    /// <summary>
    /// Parse CSV content into rows (for preview)
    /// </summary>
    List<string[]> ParseCsvPreview(string csvContent, int maxRows = 5);
}

public enum ImportFormat
{
    Unknown,
    Csv,
    Json
}

public class ImportResult<T>
{
    public bool Success => !Errors.Any() || (SuccessfulRows > 0 && AllowPartialImport);
    public bool HasErrors => Errors.Any();
    public bool AllowPartialImport { get; set; }
    public List<T> ImportedItems { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows => TotalRows - SuccessfulRows;
    public string[] Headers { get; set; } = Array.Empty<string>();

    public string GetSummary()
    {
        if (!HasErrors)
            return $"Successfully imported {SuccessfulRows} items.";

        if (SuccessfulRows > 0)
            return $"Imported {SuccessfulRows} of {TotalRows} items. {Errors.Count} errors occurred.";

        return $"Import failed. {Errors.Count} errors found.";
    }
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Value { get; set; }
    public ImportErrorSeverity Severity { get; set; } = ImportErrorSeverity.Error;

    public string GetDisplayMessage()
    {
        var location = Column != null
            ? $"Row {RowNumber}, Column \"{Column}\""
            : $"Row {RowNumber}";

        var valueInfo = !string.IsNullOrEmpty(Value)
            ? $" (value: \"{TruncateValue(Value)}\")"
            : "";

        return $"{location}: {Message}{valueInfo}";
    }

    private static string TruncateValue(string value, int maxLength = 30)
    {
        if (value.Length <= maxLength)
            return value;
        return value[..(maxLength - 3)] + "...";
    }
}

public enum ImportErrorSeverity
{
    Warning,
    Error
}

public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public string[] FoundHeaders { get; set; } = Array.Empty<string>();
    public string[] ExpectedHeaders { get; set; } = Array.Empty<string>();
    public string[] MissingRequired { get; set; } = Array.Empty<string>();
    public string[] UnrecognizedHeaders { get; set; } = Array.Empty<string>();
    public List<string> Messages { get; set; } = new();
}

public class ImportService : IImportService
{
    public ImportResult<T> ImportFromCsv<T>(string csvContent) where T : new()
    {
        var result = new ImportResult<T>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "File is empty or contains no data."
            });
            return result;
        }

        try
        {
            var lines = ParseCsvLines(csvContent);
            if (lines.Count < 2)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = 0,
                    Message = "File must contain a header row and at least one data row."
                });
                return result;
            }

            // Parse header row
            var headers = ParseCsvRow(lines[0]);
            result.Headers = headers;

            // Get property map
            var propertyMap = BuildPropertyMap<T>(headers);

            result.TotalRows = lines.Count - 1;

            // Parse data rows
            for (int i = 1; i < lines.Count; i++)
            {
                var rowNumber = i + 1; // 1-based for user display
                var row = ParseCsvRow(lines[i]);

                if (row.All(string.IsNullOrWhiteSpace))
                    continue; // Skip empty rows

                try
                {
                    var item = ParseRow<T>(row, headers, propertyMap, rowNumber, result.Errors);
                    if (item != null)
                    {
                        result.ImportedItems.Add(item);
                        result.SuccessfulRows++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        Message = $"Failed to parse row: {ex.Message}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = $"Failed to parse CSV: {ex.Message}"
            });
        }

        return result;
    }

    public ImportResult<T> ImportFromJson<T>(string jsonContent)
    {
        var result = new ImportResult<T>();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = "File is empty or contains no data."
            });
            return result;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Try to parse as array first
            if (jsonContent.TrimStart().StartsWith('['))
            {
                var items = JsonSerializer.Deserialize<List<T>>(jsonContent, options);
                if (items != null)
                {
                    result.ImportedItems = items;
                    result.TotalRows = items.Count;
                    result.SuccessfulRows = items.Count;
                }
            }
            else
            {
                // Single object
                var item = JsonSerializer.Deserialize<T>(jsonContent, options);
                if (item != null)
                {
                    result.ImportedItems.Add(item);
                    result.TotalRows = 1;
                    result.SuccessfulRows = 1;
                }
            }
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = $"Invalid JSON format: {ex.Message}",
                Value = GetJsonErrorContext(jsonContent, ex)
            });
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError
            {
                RowNumber = 0,
                Message = $"Failed to parse JSON: {ex.Message}"
            });
        }

        return result;
    }

    public ImportValidationResult ValidateCsvHeaders<T>(string csvContent, string[]? requiredColumns = null)
    {
        var result = new ImportValidationResult();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Messages.Add("File is empty.");
            return result;
        }

        var lines = ParseCsvLines(csvContent);
        if (lines.Count == 0)
        {
            result.Messages.Add("No header row found.");
            return result;
        }

        var headers = ParseCsvRow(lines[0]);
        result.FoundHeaders = headers;

        // Get expected properties
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p => p.Name)
            .ToArray();
        result.ExpectedHeaders = properties;

        // Find unrecognized headers
        result.UnrecognizedHeaders = headers
            .Where(h => !properties.Any(p => p.Equals(h, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Check required columns
        if (requiredColumns != null)
        {
            result.MissingRequired = requiredColumns
                .Where(r => !headers.Any(h => h.Equals(r, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        result.IsValid = result.MissingRequired.Length == 0;

        if (result.UnrecognizedHeaders.Any())
        {
            result.Messages.Add($"Unrecognized columns will be ignored: {string.Join(", ", result.UnrecognizedHeaders)}");
        }

        if (result.MissingRequired.Any())
        {
            result.Messages.Add($"Missing required columns: {string.Join(", ", result.MissingRequired)}");
        }

        return result;
    }

    public ImportFormat DetectFormat(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ImportFormat.Unknown;

        var trimmed = content.Trim();

        // Check for JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                JsonDocument.Parse(trimmed);
                return ImportFormat.Json;
            }
            catch
            {
                // Not valid JSON
            }
        }

        // Check for CSV (has commas and newlines)
        if (trimmed.Contains(',') && (trimmed.Contains('\n') || trimmed.Contains('\r')))
        {
            return ImportFormat.Csv;
        }

        // Single line with commas could be CSV header
        if (trimmed.Contains(',') && !trimmed.Contains('{'))
        {
            return ImportFormat.Csv;
        }

        return ImportFormat.Unknown;
    }

    public List<string[]> ParseCsvPreview(string csvContent, int maxRows = 5)
    {
        var result = new List<string[]>();
        var lines = ParseCsvLines(csvContent);

        for (int i = 0; i < Math.Min(lines.Count, maxRows + 1); i++)
        {
            result.Add(ParseCsvRow(lines[i]));
        }

        return result;
    }

    private static List<string> ParseCsvLines(string content)
    {
        var lines = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in content)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                currentLine.Append(ch);
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
            }
            else
            {
                currentLine.Append(ch);
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    private static string[] ParseCsvRow(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    private static Dictionary<string, PropertyInfo> BuildPropertyMap<T>(string[] headers)
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name.ToLowerInvariant(), p => p);

        var map = new Dictionary<string, PropertyInfo>();

        foreach (var header in headers)
        {
            var normalizedHeader = header.ToLowerInvariant().Trim();
            if (properties.TryGetValue(normalizedHeader, out var prop))
            {
                map[header] = prop;
            }
        }

        return map;
    }

    private static T? ParseRow<T>(string[] values, string[] headers, Dictionary<string, PropertyInfo> propertyMap, int rowNumber, List<ImportError> errors) where T : new()
    {
        var item = new T();
        var hasErrors = false;

        for (int i = 0; i < Math.Min(values.Length, headers.Length); i++)
        {
            var header = headers[i];
            var value = values[i];

            if (!propertyMap.TryGetValue(header, out var property))
                continue;

            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType, header, rowNumber, errors);
                if (convertedValue != null || Nullable.GetUnderlyingType(property.PropertyType) != null)
                {
                    property.SetValue(item, convertedValue);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = header,
                    Message = $"Failed to convert value: {ex.Message}",
                    Value = value
                });
                hasErrors = true;
            }
        }

        return hasErrors ? default : item;
    }

    private static object? ConvertValue(string value, Type targetType, string column, int rowNumber, List<ImportError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType)
                return null;

            // Return default for value types
            return Activator.CreateInstance(targetType);
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            // Handle special types
            if (underlyingType == typeof(string))
                return value;

            if (underlyingType == typeof(int))
            {
                if (int.TryParse(value, out var intVal))
                    return intVal;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid number. Expected a whole number.",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(bool))
            {
                var lower = value.ToLowerInvariant();
                if (lower == "true" || lower == "1" || lower == "yes")
                    return true;
                if (lower == "false" || lower == "0" || lower == "no")
                    return false;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid boolean. Use 'true'/'false', 'yes'/'no', or '1'/'0'.",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var dateVal))
                    return dateVal;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid date. Use format YYYY-MM-DD (e.g., {DateTime.Now:yyyy-MM-dd}).",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(DateOnly))
            {
                if (DateOnly.TryParse(value, out var dateOnlyVal))
                    return dateOnlyVal;
                if (DateTime.TryParse(value, out var dtVal))
                    return DateOnly.FromDateTime(dtVal);
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid date. Use format YYYY-MM-DD.",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(TimeOnly))
            {
                if (TimeOnly.TryParse(value, out var timeVal))
                    return timeVal;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid time. Use format HH:MM (e.g., 09:30).",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(TimeSpan))
            {
                if (TimeSpan.TryParse(value, out var tsVal))
                    return tsVal;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid time span. Use format HH:MM.",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            if (underlyingType == typeof(Guid))
            {
                if (string.IsNullOrEmpty(value))
                    return Guid.Empty;
                if (Guid.TryParse(value, out var guidVal))
                    return guidVal;
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid ID format.",
                    Value = value,
                    Severity = ImportErrorSeverity.Warning
                });
                return Guid.Empty;
            }

            if (underlyingType.IsEnum)
            {
                if (Enum.TryParse(underlyingType, value, true, out var enumVal))
                    return enumVal;
                var validValues = string.Join(", ", Enum.GetNames(underlyingType));
                errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Column = column,
                    Message = $"Invalid value. Expected one of: {validValues}.",
                    Value = value,
                    Severity = ImportErrorSeverity.Error
                });
                return null;
            }

            // Handle List<string> (semicolon separated)
            if (underlyingType == typeof(List<string>))
            {
                return value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            // Handle List<Guid> (semicolon separated)
            if (underlyingType == typeof(List<Guid>))
            {
                var guids = new List<Guid>();
                foreach (var part in value.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Guid.TryParse(part.Trim(), out var g))
                        guids.Add(g);
                }
                return guids;
            }

            // Fallback to TypeConverter
            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromString(value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetJsonErrorContext(string json, JsonException ex)
    {
        if (ex.LineNumber.HasValue && ex.BytePositionInLine.HasValue)
        {
            var lines = json.Split('\n');
            if (ex.LineNumber.Value < lines.Length)
            {
                var line = lines[(int)ex.LineNumber.Value];
                var start = Math.Max(0, (int)ex.BytePositionInLine.Value - 20);
                var length = Math.Min(40, line.Length - start);
                return line.Substring(start, length);
            }
        }
        return "";
    }
}
