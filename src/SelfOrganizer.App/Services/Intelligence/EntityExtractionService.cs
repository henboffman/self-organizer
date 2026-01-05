using System.Text.RegularExpressions;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Lightweight entity extraction service using pure C# regex and heuristics.
/// No external ML libraries - optimized for consumer-grade hardware.
/// </summary>
public class EntityExtractionService : IEntityExtractionService
{
    // Common English words to exclude from acronym detection
    private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "I", "A", "AN", "THE", "AND", "OR", "BUT", "IN", "ON", "AT", "TO", "FOR",
        "OF", "WITH", "BY", "FROM", "AS", "IS", "IT", "BE", "ARE", "WAS", "WERE",
        "AM", "PM", "VS", "ETC", "IE", "EG", "OK", "NO", "YES", "IF", "SO", "DO",
        "US", "WE", "HE", "SHE", "ME", "MY", "UP", "GO", "ALL", "NEW", "OLD"
    };

    // Common acronym patterns in business/tech contexts
    private static readonly HashSet<string> KnownAcronyms = new(StringComparer.OrdinalIgnoreCase)
    {
        "API", "SDK", "UI", "UX", "QA", "QC", "PR", "MR", "CI", "CD", "MVP", "POC",
        "KPI", "OKR", "ROI", "SLA", "EOD", "EOW", "EOM", "WIP", "TBD", "TBA", "ASAP",
        "FYI", "IMO", "IMHO", "CEO", "CTO", "CFO", "COO", "VP", "SVP", "EVP", "GM",
        "HR", "IT", "PM", "PO", "SM", "BA", "QE", "SRE", "DBA", "AWS", "GCP", "SQL",
        "HTML", "CSS", "JS", "TS", "REST", "CRUD", "JSON", "XML", "CSV", "PDF"
    };

    public ExtractionResult ExtractEntities(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ExtractionResult();

        var result = new ExtractionResult
        {
            Acronyms = ExtractAcronyms(text),
            ProperNouns = ExtractProperNouns(text),
            Mentions = ExtractMentions(text),
            Hashtags = ExtractHashtags(text),
            KeyPhrases = ExtractKeyPhrases(text),
            RepeatedTerms = ExtractRepeatedTerms(text)
        };

        return result;
    }

    public IReadOnlyList<string> ExtractAcronyms(string text)
    {
        var acronyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern: 2-6 uppercase letters, optionally with numbers (e.g., "S3", "EC2")
        var acronymPattern = new Regex(@"\b([A-Z][A-Z0-9]{1,5})\b", RegexOptions.Compiled);

        foreach (Match match in acronymPattern.Matches(text))
        {
            var candidate = match.Groups[1].Value;

            // Skip common words
            if (CommonWords.Contains(candidate))
                continue;

            // Include known acronyms
            if (KnownAcronyms.Contains(candidate))
            {
                acronyms.Add(candidate);
                continue;
            }

            // Include if it looks like an acronym (all caps, 2+ chars, not a common word)
            if (candidate.Length >= 2 && candidate.All(c => char.IsUpper(c) || char.IsDigit(c)))
            {
                acronyms.Add(candidate);
            }
        }

        return acronyms.OrderBy(a => a).ToList();
    }

    public IReadOnlyList<string> ExtractProperNouns(string text)
    {
        var properNouns = new HashSet<string>(StringComparer.Ordinal);

        // Split into sentences to identify sentence starts
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");

        foreach (var sentence in sentences)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i].Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')');

                if (string.IsNullOrEmpty(word) || word.Length < 2)
                    continue;

                // Skip first word of sentence (might be capitalized just because it's first)
                if (i == 0)
                    continue;

                // Check if word starts with uppercase and rest is lowercase (proper noun pattern)
                if (char.IsUpper(word[0]) && word.Skip(1).All(c => char.IsLower(c) || !char.IsLetter(c)))
                {
                    // Skip common words that happen to be capitalized
                    if (!CommonWords.Contains(word))
                    {
                        properNouns.Add(word);
                    }
                }

                // Also detect multi-word proper nouns (e.g., "New York", "John Smith")
                if (i < words.Length - 1)
                {
                    var nextWord = words[i + 1].Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')');
                    if (!string.IsNullOrEmpty(nextWord) &&
                        char.IsUpper(word[0]) && char.IsUpper(nextWord[0]) &&
                        !CommonWords.Contains(word) && !CommonWords.Contains(nextWord))
                    {
                        properNouns.Add($"{word} {nextWord}");
                    }
                }
            }
        }

        return properNouns.OrderBy(n => n).ToList();
    }

    public IReadOnlyList<string> ExtractMentions(string text)
    {
        // Pattern: @username style mentions
        var mentionPattern = new Regex(@"@(\w+)", RegexOptions.Compiled);
        var mentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in mentionPattern.Matches(text))
        {
            mentions.Add(match.Groups[1].Value);
        }

        return mentions.OrderBy(m => m).ToList();
    }

    public IReadOnlyList<string> ExtractHashtags(string text)
    {
        // Pattern: #hashtag style tags
        var hashtagPattern = new Regex(@"#(\w+)", RegexOptions.Compiled);
        var hashtags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in hashtagPattern.Matches(text))
        {
            hashtags.Add(match.Groups[1].Value);
        }

        return hashtags.OrderBy(h => h).ToList();
    }

    public IReadOnlyList<string> ExtractKeyPhrases(string text)
    {
        var keyPhrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Look for quoted phrases
        var quotedPattern = new Regex(@"""([^""]+)""", RegexOptions.Compiled);
        foreach (Match match in quotedPattern.Matches(text))
        {
            var phrase = match.Groups[1].Value.Trim();
            if (phrase.Length >= 3 && phrase.Split(' ').Length <= 5)
            {
                keyPhrases.Add(phrase);
            }
        }

        // Look for phrases in parentheses (often important context)
        var parenPattern = new Regex(@"\(([^)]+)\)", RegexOptions.Compiled);
        foreach (Match match in parenPattern.Matches(text))
        {
            var phrase = match.Groups[1].Value.Trim();
            if (phrase.Length >= 3 && phrase.Split(' ').Length <= 5)
            {
                keyPhrases.Add(phrase);
            }
        }

        // Look for colon-prefixed items (e.g., "Project: Alpha")
        var colonPattern = new Regex(@"(\w+):\s*([^\s,;]+(?:\s+[^\s,;]+)?)", RegexOptions.Compiled);
        foreach (Match match in colonPattern.Matches(text))
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            if (value.Length >= 2)
            {
                keyPhrases.Add($"{key}: {value}");
            }
        }

        return keyPhrases.OrderBy(p => p).ToList();
    }

    public IReadOnlyList<TermFrequency> ExtractRepeatedTerms(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "it", "be", "are", "was",
            "were", "been", "being", "have", "has", "had", "do", "does", "did",
            "will", "would", "could", "should", "may", "might", "must", "shall",
            "can", "need", "this", "that", "these", "those", "i", "you", "he",
            "she", "we", "they", "what", "which", "who", "when", "where", "why",
            "how", "all", "each", "every", "both", "few", "more", "most", "other",
            "some", "such", "no", "nor", "not", "only", "own", "same", "so",
            "than", "too", "very", "just", "also", "now", "here", "there"
        };

        // Tokenize and count
        var wordPattern = new Regex(@"\b[a-zA-Z]{3,}\b", RegexOptions.Compiled);
        var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in wordPattern.Matches(text))
        {
            var word = match.Value.ToLowerInvariant();
            if (!stopWords.Contains(word))
            {
                wordCounts[word] = wordCounts.GetValueOrDefault(word, 0) + 1;
            }
        }

        // Return terms that appear more than once
        return wordCounts
            .Where(kv => kv.Value > 1)
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => new TermFrequency(kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>
    /// Suggests tags based on extracted entities
    /// </summary>
    public IReadOnlyList<string> SuggestTags(ExtractionResult extraction)
    {
        var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add acronyms as potential tags
        foreach (var acronym in extraction.Acronyms.Take(5))
        {
            suggestions.Add(acronym);
        }

        // Add hashtags (already formatted as tags)
        foreach (var hashtag in extraction.Hashtags)
        {
            suggestions.Add(hashtag);
        }

        // Add high-frequency terms
        foreach (var term in extraction.RepeatedTerms.Take(3))
        {
            suggestions.Add(term.Term);
        }

        return suggestions.Take(10).ToList();
    }
}

/// <summary>
/// Interface for entity extraction service
/// </summary>
public interface IEntityExtractionService
{
    ExtractionResult ExtractEntities(string? text);
    IReadOnlyList<string> ExtractAcronyms(string text);
    IReadOnlyList<string> ExtractProperNouns(string text);
    IReadOnlyList<string> ExtractMentions(string text);
    IReadOnlyList<string> ExtractHashtags(string text);
    IReadOnlyList<string> ExtractKeyPhrases(string text);
    IReadOnlyList<TermFrequency> ExtractRepeatedTerms(string text);
    IReadOnlyList<string> SuggestTags(ExtractionResult extraction);
}

/// <summary>
/// Result of entity extraction
/// </summary>
public class ExtractionResult
{
    public IReadOnlyList<string> Acronyms { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ProperNouns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Mentions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Hashtags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> KeyPhrases { get; set; } = Array.Empty<string>();
    public IReadOnlyList<TermFrequency> RepeatedTerms { get; set; } = Array.Empty<TermFrequency>();

    public bool HasEntities =>
        Acronyms.Any() || ProperNouns.Any() || Mentions.Any() ||
        Hashtags.Any() || KeyPhrases.Any() || RepeatedTerms.Any();
}

/// <summary>
/// A term and its frequency count
/// </summary>
public record TermFrequency(string Term, int Count);
