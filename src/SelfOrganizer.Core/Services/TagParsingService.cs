using System.Text.RegularExpressions;

namespace SelfOrganizer.Core.Services;

/// <summary>
/// Service for parsing and extracting tags from text.
/// Tags are identified by the # prefix (e.g., #project, #urgent).
/// All tag operations are case-insensitive.
/// </summary>
public static class TagParsingService
{
    // Regex pattern for tags: # followed by word characters (letters, numbers, underscores, hyphens)
    private static readonly Regex TagPattern = new(@"#([\w\-]+)", RegexOptions.Compiled);

    /// <summary>
    /// Extracts all tags from the given text.
    /// Tags are normalized to lowercase for case-insensitive matching.
    /// </summary>
    /// <param name="text">The text to parse for tags</param>
    /// <returns>A list of unique tags (without the # prefix), normalized to lowercase</returns>
    public static List<string> ExtractTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var matches = TagPattern.Matches(text);
        return matches
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Removes tag syntax from text, leaving just the tag word.
    /// For display purposes - converts "#project" to "project".
    /// </summary>
    /// <param name="text">The text containing tags</param>
    /// <returns>Text with # symbols removed from tags</returns>
    public static string RemoveTagSyntax(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        return TagPattern.Replace(text, "$1");
    }

    /// <summary>
    /// Gets the text with tags removed entirely.
    /// Useful for getting clean title/description without tags.
    /// </summary>
    /// <param name="text">The text containing tags</param>
    /// <returns>Text with all tags removed and cleaned up</returns>
    public static string GetTextWithoutTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var result = TagPattern.Replace(text, "");
        // Clean up extra whitespace
        result = Regex.Replace(result, @"\s+", " ").Trim();
        return result;
    }

    /// <summary>
    /// Parses text and returns both the cleaned text and extracted tags.
    /// This is the primary method for processing user input.
    /// </summary>
    /// <param name="text">The raw text from user input</param>
    /// <returns>A tuple of (cleanedText, tags)</returns>
    public static (string CleanedText, List<string> Tags) ParseTextAndTags(string? text)
    {
        var tags = ExtractTags(text);
        var cleanedText = GetTextWithoutTags(text);
        return (cleanedText, tags);
    }

    /// <summary>
    /// Merges new tags with existing tags, maintaining case-insensitive uniqueness.
    /// </summary>
    /// <param name="existingTags">The current list of tags</param>
    /// <param name="newTags">Tags to add</param>
    /// <returns>Combined list of unique tags</returns>
    public static List<string> MergeTags(IEnumerable<string>? existingTags, IEnumerable<string>? newTags)
    {
        var existing = existingTags?.Select(t => t.ToLowerInvariant()) ?? Enumerable.Empty<string>();
        var added = newTags?.Select(t => t.ToLowerInvariant()) ?? Enumerable.Empty<string>();

        return existing.Union(added).Distinct().ToList();
    }

    /// <summary>
    /// Checks if a tag matches another tag (case-insensitive).
    /// </summary>
    public static bool TagsMatch(string tag1, string tag2)
    {
        return string.Equals(tag1, tag2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a list of tags contains a specific tag (case-insensitive).
    /// </summary>
    public static bool ContainsTag(IEnumerable<string>? tags, string tag)
    {
        if (tags == null || string.IsNullOrWhiteSpace(tag))
            return false;

        return tags.Any(t => TagsMatch(t, tag));
    }

    /// <summary>
    /// Finds all tag occurrences in text with their positions.
    /// Useful for rendering tags with special styling.
    /// </summary>
    /// <param name="text">The text to search</param>
    /// <returns>List of tag matches with position information</returns>
    public static List<TagMatch> FindTagMatches(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<TagMatch>();

        var matches = TagPattern.Matches(text);
        return matches.Select(m => new TagMatch
        {
            FullMatch = m.Value,
            TagName = m.Groups[1].Value.ToLowerInvariant(),
            StartIndex = m.Index,
            Length = m.Length
        }).ToList();
    }
}

/// <summary>
/// Represents a tag match found in text.
/// </summary>
public class TagMatch
{
    /// <summary>
    /// The full matched text including the # symbol (e.g., "#project")
    /// </summary>
    public string FullMatch { get; set; } = string.Empty;

    /// <summary>
    /// The tag name without the # symbol, normalized to lowercase (e.g., "project")
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// The starting index of the match in the original text
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// The length of the full match
    /// </summary>
    public int Length { get; set; }
}
