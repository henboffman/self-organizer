using System.Text.RegularExpressions;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.Sequence;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Core.Services;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Entity extraction service using Microsoft.Recognizers.Text for NER
/// and pattern-based extraction for additional entities.
/// Extracts: dates, times, durations, numbers, URLs, emails, phone numbers,
/// mentions, hashtags, people names, organizations, and key phrases.
/// </summary>
public class EntityExtractionService : IEntityExtractionService
{
    private readonly ITaskService? _taskService;
    private readonly ICalendarService? _calendarService;
    private readonly IRepository<Project>? _projectRepository;
    private const string Culture = "en-us";

    // Common English words to exclude from proper noun detection
    private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "I", "A", "AN", "THE", "AND", "OR", "BUT", "IN", "ON", "AT", "TO", "FOR",
        "OF", "WITH", "BY", "FROM", "AS", "IS", "IT", "BE", "ARE", "WAS", "WERE",
        "AM", "PM", "VS", "ETC", "IE", "EG", "OK", "NO", "YES", "IF", "SO", "DO",
        "US", "WE", "HE", "SHE", "ME", "MY", "UP", "GO", "ALL", "NEW", "OLD",
        "WILL", "CAN", "MAY", "SHOULD", "WOULD", "COULD", "MUST", "HAS", "HAD",
        "HAVE", "BEEN", "BEING", "JUST", "NOW", "THEN", "HERE", "THERE", "WHEN",
        "WHERE", "WHY", "HOW", "WHAT", "WHICH", "WHO", "WHOM", "WHOSE", "THIS",
        "THAT", "THESE", "THOSE", "SOME", "ANY", "EACH", "EVERY", "BOTH", "FEW",
        "MORE", "MOST", "OTHER", "SUCH", "ONLY", "OWN", "SAME", "THAN", "TOO",
        "VERY", "ALSO", "STILL", "EVEN", "BACK", "WELL", "MUCH", "MANY", "MAKE",
        "MADE", "TAKE", "TOOK", "GET", "GOT", "GIVE", "GAVE", "THINK", "KNOW",
        "SEE", "SAW", "COME", "CAME", "WANT", "USE", "FIND", "TELL", "ASK", "WORK",
        "SEEM", "FEEL", "TRY", "LEAVE", "CALL", "NEED", "KEEP", "LET", "BEGIN",
        "SEEM", "HELP", "SHOW", "HEAR", "PLAY", "RUN", "MOVE", "LIVE", "BELIEVE",
        "HOLD", "BRING", "HAPPEN", "WRITE", "PROVIDE", "SIT", "STAND", "LOSE",
        "PAY", "MEET", "INCLUDE", "CONTINUE", "SET", "LEARN", "CHANGE", "LEAD",
        "UNDERSTAND", "WATCH", "FOLLOW", "STOP", "CREATE", "SPEAK", "READ",
        "ALLOW", "ADD", "SPEND", "GROW", "OPEN", "WALK", "WIN", "OFFER", "REMEMBER",
        "LOVE", "CONSIDER", "APPEAR", "BUY", "WAIT", "SERVE", "DIE", "SEND", "EXPECT",
        "BUILD", "STAY", "FALL", "CUT", "REACH", "KILL", "REMAIN", "SUGGEST",
        "RAISE", "PASS", "SELL", "REQUIRE", "REPORT", "DECIDE", "PULL",
        // Days and months (often capitalized but not proper nouns)
        "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY",
        "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST",
        "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER", "TODAY", "TOMORROW", "YESTERDAY"
    };

    // Common title prefixes that indicate a person
    private static readonly string[] TitlePrefixes = { "Mr", "Mrs", "Ms", "Miss", "Dr", "Prof", "Sir", "Madam" };

    // Default constructor for basic extraction without services
    public EntityExtractionService() { }

    // Constructor with services for retroactive processing
    public EntityExtractionService(
        ITaskService taskService,
        ICalendarService calendarService,
        IRepository<Project> projectRepository)
    {
        _taskService = taskService;
        _calendarService = calendarService;
        _projectRepository = projectRepository;
    }

    public ExtractionResult ExtractEntities(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ExtractionResult();

        var result = new ExtractionResult
        {
            // Use Microsoft Recognizers for structured entities
            Dates = ExtractDates(text),
            Times = ExtractTimes(text),
            Durations = ExtractDurations(text),
            Numbers = ExtractNumbers(text),
            Urls = ExtractUrls(text),
            Emails = ExtractEmails(text),
            PhoneNumbers = ExtractPhoneNumbers(text),
            IpAddresses = ExtractIpAddresses(text),

            // Use pattern-based extraction for other entities
            Mentions = ExtractMentions(text),
            Hashtags = ExtractHashtags(text),
            People = ExtractPeople(text),
            Organizations = ExtractOrganizations(text),
            Acronyms = ExtractAcronyms(text),
            KeyPhrases = ExtractKeyPhrases(text),
            MoneyAmounts = ExtractMoneyAmounts(text),
            RepeatedTerms = ExtractRepeatedTerms(text)
        };

        return result;
    }

    #region Microsoft Recognizers-based extraction

    public IReadOnlyList<ExtractedEntity> ExtractDates(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = DateTimeRecognizer.RecognizeDateTime(text, Culture);
            foreach (var result in results)
            {
                if (result.TypeName.Contains("date") && !result.TypeName.Contains("time"))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = EntityType.Date,
                        Value = result.Text,
                        Start = result.Start,
                        End = result.Start + result.Text.Length,
                        Resolution = GetResolutionValue(result.Resolution)
                    });
                }
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractTimes(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = DateTimeRecognizer.RecognizeDateTime(text, Culture);
            foreach (var result in results)
            {
                if (result.TypeName.Contains("time") && !result.TypeName.Contains("date"))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = EntityType.Time,
                        Value = result.Text,
                        Start = result.Start,
                        End = result.Start + result.Text.Length,
                        Resolution = GetResolutionValue(result.Resolution)
                    });
                }
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractDurations(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = DateTimeRecognizer.RecognizeDateTime(text, Culture);
            foreach (var result in results)
            {
                if (result.TypeName.Contains("duration"))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = EntityType.Duration,
                        Value = result.Text,
                        Start = result.Start,
                        End = result.Start + result.Text.Length,
                        Resolution = GetResolutionValue(result.Resolution)
                    });
                }
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractNumbers(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = NumberRecognizer.RecognizeNumber(text, Culture);
            foreach (var result in results)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Number,
                    Value = result.Text,
                    Start = result.Start,
                    End = result.Start + result.Text.Length,
                    Resolution = result.Resolution?.FirstOrDefault().Value?.ToString()
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractUrls(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = SequenceRecognizer.RecognizeURL(text, Culture);
            foreach (var result in results)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Url,
                    Value = result.Text,
                    Start = result.Start,
                    End = result.Start + result.Text.Length
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractEmails(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = SequenceRecognizer.RecognizeEmail(text, Culture);
            foreach (var result in results)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Email,
                    Value = result.Text,
                    Start = result.Start,
                    End = result.Start + result.Text.Length
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractPhoneNumbers(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = SequenceRecognizer.RecognizePhoneNumber(text, Culture);
            foreach (var result in results)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.PhoneNumber,
                    Value = result.Text,
                    Start = result.Start,
                    End = result.Start + result.Text.Length
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractIpAddresses(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            var results = SequenceRecognizer.RecognizeIpAddress(text, Culture);
            foreach (var result in results)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.IpAddress,
                    Value = result.Text,
                    Start = result.Start,
                    End = result.Start + result.Text.Length
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    public IReadOnlyList<ExtractedEntity> ExtractMoneyAmounts(string text)
    {
        var entities = new List<ExtractedEntity>();
        try
        {
            // Use regex for money patterns since NumberWithUnit isn't always reliable
            var moneyPattern = new Regex(@"[\$€£¥]\s*\d+(?:[,\.]\d+)*(?:\s*(?:k|m|b|thousand|million|billion))?|\d+(?:[,\.]\d+)*\s*(?:dollars?|euros?|pounds?|USD|EUR|GBP)", RegexOptions.IgnoreCase);
            foreach (Match match in moneyPattern.Matches(text))
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Money,
                    Value = match.Value,
                    Start = match.Index,
                    End = match.Index + match.Length
                });
            }
        }
        catch { /* Ignore recognition errors */ }
        return entities;
    }

    #endregion

    #region Pattern-based extraction

    public IReadOnlyList<string> ExtractMentions(string text)
    {
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
        var hashtagPattern = new Regex(@"#(\w+)", RegexOptions.Compiled);
        var hashtags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in hashtagPattern.Matches(text))
        {
            hashtags.Add(match.Groups[1].Value);
        }

        return hashtags.OrderBy(h => h).ToList();
    }

    public IReadOnlyList<ExtractedEntity> ExtractPeople(string text)
    {
        var people = new List<ExtractedEntity>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: Title + Name (Mr. John Smith, Dr. Jane Doe)
        var titlePattern = new Regex($@"\b({string.Join("|", TitlePrefixes)})\.?\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\b");
        foreach (Match match in titlePattern.Matches(text))
        {
            var name = match.Groups[2].Value;
            if (!seen.Contains(name))
            {
                seen.Add(name);
                people.Add(new ExtractedEntity
                {
                    Type = EntityType.Person,
                    Value = match.Value.Trim(),
                    Start = match.Index,
                    End = match.Index + match.Length
                });
            }
        }

        // Pattern 2: Two consecutive capitalized words not at sentence start (likely names)
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        foreach (var sentence in sentences)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < words.Length - 1; i++)
            {
                var word1 = words[i].Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')');
                var word2 = words[i + 1].Trim(',', '.', '!', '?', ':', ';', '"', '\'', '(', ')');

                if (IsCapitalizedName(word1) && IsCapitalizedName(word2) &&
                    !CommonWords.Contains(word1) && !CommonWords.Contains(word2))
                {
                    var fullName = $"{word1} {word2}";
                    if (!seen.Contains(fullName))
                    {
                        seen.Add(fullName);
                        people.Add(new ExtractedEntity
                        {
                            Type = EntityType.Person,
                            Value = fullName,
                            Start = text.IndexOf(fullName, StringComparison.Ordinal),
                            End = text.IndexOf(fullName, StringComparison.Ordinal) + fullName.Length
                        });
                    }
                }
            }
        }

        // Pattern 3: "with [Name]", "from [Name]", "by [Name]", "to [Name]"
        var prepPattern = new Regex(@"\b(?:with|from|by|to|for|and|meeting|call|email|contact)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\b", RegexOptions.IgnoreCase);
        foreach (Match match in prepPattern.Matches(text))
        {
            var name = match.Groups[1].Value;
            if (IsCapitalizedName(name.Split(' ')[0]) && !CommonWords.Contains(name.Split(' ')[0]))
            {
                if (!seen.Contains(name))
                {
                    seen.Add(name);
                    people.Add(new ExtractedEntity
                    {
                        Type = EntityType.Person,
                        Value = name,
                        Start = match.Groups[1].Index,
                        End = match.Groups[1].Index + name.Length
                    });
                }
            }
        }

        return people;
    }

    public IReadOnlyList<ExtractedEntity> ExtractOrganizations(string text)
    {
        var orgs = new List<ExtractedEntity>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: Acronyms that look like org names (3+ caps)
        var acronymPattern = new Regex(@"\b([A-Z]{3,})\b");
        foreach (Match match in acronymPattern.Matches(text))
        {
            var acronym = match.Groups[1].Value;
            if (!CommonWords.Contains(acronym) && acronym.Length <= 6)
            {
                if (!seen.Contains(acronym))
                {
                    seen.Add(acronym);
                    orgs.Add(new ExtractedEntity
                    {
                        Type = EntityType.Organization,
                        Value = acronym,
                        Start = match.Index,
                        End = match.Index + match.Length
                    });
                }
            }
        }

        // Pattern 2: Words ending in Inc, Corp, LLC, Ltd, Company, etc.
        var corpPattern = new Regex(@"\b([A-Z][a-zA-Z]*(?:\s+[A-Z][a-zA-Z]*)*)\s+(?:Inc\.?|Corp\.?|LLC|Ltd\.?|Company|Co\.?|Group|Foundation|Association|Institute|University|College)\b", RegexOptions.IgnoreCase);
        foreach (Match match in corpPattern.Matches(text))
        {
            var org = match.Value.Trim();
            if (!seen.Contains(org))
            {
                seen.Add(org);
                orgs.Add(new ExtractedEntity
                {
                    Type = EntityType.Organization,
                    Value = org,
                    Start = match.Index,
                    End = match.Index + match.Length
                });
            }
        }

        // Pattern 3: "at [Organization]", "from [Organization]", "with [Organization]"
        var atPattern = new Regex(@"\b(?:at|from|with|for|join(?:ing)?|left|leaving)\s+(?:the\s+)?([A-Z][a-zA-Z]*(?:\s+[A-Z][a-zA-Z]*){0,3})\b");
        foreach (Match match in atPattern.Matches(text))
        {
            var org = match.Groups[1].Value;
            // Check if it looks like an org (multiple caps words or known pattern)
            if (org.Split(' ').All(w => char.IsUpper(w[0])) && !CommonWords.Contains(org.Split(' ')[0]))
            {
                if (!seen.Contains(org))
                {
                    seen.Add(org);
                    orgs.Add(new ExtractedEntity
                    {
                        Type = EntityType.Organization,
                        Value = org,
                        Start = match.Groups[1].Index,
                        End = match.Groups[1].Index + org.Length
                    });
                }
            }
        }

        return orgs;
    }

    public IReadOnlyList<string> ExtractAcronyms(string text)
    {
        var acronyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var acronymPattern = new Regex(@"\b([A-Z][A-Z0-9]{1,5})\b", RegexOptions.Compiled);

        foreach (Match match in acronymPattern.Matches(text))
        {
            var candidate = match.Groups[1].Value;
            if (!CommonWords.Contains(candidate) && candidate.Length >= 2)
            {
                acronyms.Add(candidate);
            }
        }

        return acronyms.OrderBy(a => a).ToList();
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

        // Look for phrases in parentheses
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

    #endregion

    #region Helper methods

    private bool IsCapitalizedName(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2)
            return false;
        return char.IsUpper(word[0]) && word.Skip(1).All(c => char.IsLower(c) || !char.IsLetter(c));
    }

    private string? GetResolutionValue(SortedDictionary<string, object>? resolution)
    {
        if (resolution == null || !resolution.Any())
            return null;

        if (resolution.TryGetValue("values", out var values) && values is List<Dictionary<string, string>> valueList)
        {
            if (valueList.Any() && valueList[0].TryGetValue("value", out var val))
                return val;
        }

        return resolution.FirstOrDefault().Value?.ToString();
    }

    #endregion

    #region Tag suggestion

    /// <summary>
    /// Suggests tags based on extracted entities
    /// </summary>
    public IReadOnlyList<string> SuggestTags(ExtractionResult extraction)
    {
        var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add hashtags (already formatted as tags)
        foreach (var hashtag in extraction.Hashtags)
        {
            suggestions.Add(hashtag);
        }

        // Add acronyms as potential tags
        foreach (var acronym in extraction.Acronyms.Take(5))
        {
            suggestions.Add(acronym);
        }

        // Add organization names as tags
        foreach (var org in extraction.Organizations.Take(3))
        {
            suggestions.Add(org.Value.Replace(" ", ""));
        }

        // Add people names as tags (useful for tracking who tasks involve)
        foreach (var person in extraction.People.Take(3))
        {
            suggestions.Add(person.Value.Replace(" ", ""));
        }

        return suggestions.Take(10).ToList();
    }

    #endregion

    #region Retroactive tagging methods

    /// <summary>
    /// Extracts and updates tags for a single task based on its title, description, and notes.
    /// Uses full entity extraction to find tags automatically.
    /// </summary>
    public async Task<List<string>> ExtractAndUpdateTaskTagsAsync(TodoTask task)
    {
        if (_taskService == null)
            throw new InvalidOperationException("TaskService not available. Use constructor with services.");

        // Combine all text fields
        var fullText = $"{task.Title} {task.Description} {task.Notes}";

        // Extract entities
        var extraction = ExtractEntities(fullText);

        // Get suggested tags from entities
        var suggestedTags = SuggestTags(extraction);

        // Also extract explicit hashtags from text
        var titleTags = TagParsingService.ExtractTags(task.Title);
        var descriptionTags = TagParsingService.ExtractTags(task.Description);
        var notesTags = TagParsingService.ExtractTags(task.Notes);

        // Merge all tags
        var allTags = TagParsingService.MergeTags(task.Tags,
            TagParsingService.MergeTags(suggestedTags.ToList(),
                TagParsingService.MergeTags(titleTags,
                    TagParsingService.MergeTags(descriptionTags, notesTags))));

        // Only update if there are new tags
        if (allTags.Count != task.Tags.Count || !allTags.All(t => task.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            task.Tags = allTags;
            await _taskService.UpdateAsync(task);
        }

        return allTags;
    }

    /// <summary>
    /// Extracts and updates tags for a single event based on its title and description.
    /// Uses full entity extraction to find tags automatically.
    /// </summary>
    public async Task<List<string>> ExtractAndUpdateEventTagsAsync(CalendarEvent calendarEvent)
    {
        if (_calendarService == null)
            throw new InvalidOperationException("CalendarService not available. Use constructor with services.");

        // Combine all text fields
        var fullText = $"{calendarEvent.Title} {calendarEvent.Description}";

        // Extract entities
        var extraction = ExtractEntities(fullText);

        // Get suggested tags from entities
        var suggestedTags = SuggestTags(extraction);

        // Also extract explicit hashtags from text
        var titleTags = TagParsingService.ExtractTags(calendarEvent.Title);
        var descriptionTags = TagParsingService.ExtractTags(calendarEvent.Description);

        // Merge all tags
        var allTags = TagParsingService.MergeTags(calendarEvent.Tags,
            TagParsingService.MergeTags(suggestedTags.ToList(),
                TagParsingService.MergeTags(titleTags, descriptionTags)));

        // Only update if there are new tags
        if (allTags.Count != calendarEvent.Tags.Count ||
            !allTags.All(t => calendarEvent.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
        {
            calendarEvent.Tags = allTags;
            await _calendarService.UpdateAsync(calendarEvent);
        }

        return allTags;
    }

    /// <summary>
    /// Processes all tasks and events retroactively to extract entities and tags.
    /// </summary>
    public async Task<RetroactiveTaggingResult> ProcessAllEntitiesAsync()
    {
        if (_taskService == null || _calendarService == null)
            throw new InvalidOperationException("Services not available. Use constructor with services.");

        var result = new RetroactiveTaggingResult();
        var allTagsFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allEntitiesFound = new List<ExtractedEntity>();

        // Process all tasks
        var allTasks = await _taskService.GetAllAsync();
        foreach (var task in allTasks)
        {
            result.TasksProcessed++;
            var originalTagCount = task.Tags.Count;

            // Extract entities from task text
            var fullText = $"{task.Title} {task.Description} {task.Notes}";
            var extraction = ExtractEntities(fullText);

            // Collect entities
            allEntitiesFound.AddRange(extraction.People);
            allEntitiesFound.AddRange(extraction.Organizations);
            allEntitiesFound.AddRange(extraction.Dates);
            allEntitiesFound.AddRange(extraction.Urls);
            allEntitiesFound.AddRange(extraction.Emails);
            allEntitiesFound.AddRange(extraction.PhoneNumbers);

            var tags = await ExtractAndUpdateTaskTagsAsync(task);

            if (tags.Count > originalTagCount)
            {
                result.TasksUpdated++;
            }

            foreach (var tag in tags)
            {
                allTagsFound.Add(tag);
            }
        }

        // Process all events
        var allEvents = await _calendarService.GetEventsForRangeAsync(DateTime.MinValue, DateTime.MaxValue);
        foreach (var evt in allEvents)
        {
            result.EventsProcessed++;
            var originalTagCount = evt.Tags.Count;

            // Extract entities from event text
            var fullText = $"{evt.Title} {evt.Description}";
            var extraction = ExtractEntities(fullText);

            // Collect entities
            allEntitiesFound.AddRange(extraction.People);
            allEntitiesFound.AddRange(extraction.Organizations);
            allEntitiesFound.AddRange(extraction.Dates);
            allEntitiesFound.AddRange(extraction.Urls);
            allEntitiesFound.AddRange(extraction.Emails);
            allEntitiesFound.AddRange(extraction.PhoneNumbers);

            var tags = await ExtractAndUpdateEventTagsAsync(evt);

            if (tags.Count > originalTagCount)
            {
                result.EventsUpdated++;
            }

            foreach (var tag in tags)
            {
                allTagsFound.Add(tag);
            }
        }

        result.TotalTagsFound = allTagsFound.Count;
        result.TotalEntitiesFound = allEntitiesFound.Count;
        result.PeopleFound = allEntitiesFound.Count(e => e.Type == EntityType.Person);
        result.OrganizationsFound = allEntitiesFound.Count(e => e.Type == EntityType.Organization);
        result.DatesFound = allEntitiesFound.Count(e => e.Type == EntityType.Date);
        result.UrlsFound = allEntitiesFound.Count(e => e.Type == EntityType.Url);

        return result;
    }

    /// <summary>
    /// Gets all unique tags across all tasks and events with counts.
    /// </summary>
    public async Task<List<TagInfo>> GetAllTagsAsync()
    {
        if (_taskService == null || _calendarService == null || _projectRepository == null)
            throw new InvalidOperationException("Services not available. Use constructor with services.");

        var tagCounts = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);

        // Count tags from tasks
        var allTasks = await _taskService.GetAllAsync();
        foreach (var task in allTasks)
        {
            var fullText = $"{task.Title} {task.Description} {task.Notes}";
            var extraction = ExtractEntities(fullText);
            var suggestedTags = SuggestTags(extraction);

            var allTaskTags = TagParsingService.MergeTags(
                task.Tags,
                TagParsingService.MergeTags(
                    suggestedTags.ToList(),
                    TagParsingService.MergeTags(
                        TagParsingService.ExtractTags(task.Title),
                        TagParsingService.MergeTags(
                            TagParsingService.ExtractTags(task.Description),
                            TagParsingService.ExtractTags(task.Notes)))));

            foreach (var tag in allTaskTags)
            {
                if (!tagCounts.TryGetValue(tag, out var info))
                {
                    info = new TagInfo { Name = tag };
                    tagCounts[tag] = info;
                }
                info.TaskCount++;

                var taskDate = task.ModifiedAt > task.CreatedAt ? task.ModifiedAt : task.CreatedAt;
                if (!info.LastUsed.HasValue || taskDate > info.LastUsed)
                {
                    info.LastUsed = taskDate;
                }
            }
        }

        // Count tags from events
        var allEvents = await _calendarService.GetEventsForRangeAsync(DateTime.MinValue, DateTime.MaxValue);
        foreach (var evt in allEvents)
        {
            var fullText = $"{evt.Title} {evt.Description}";
            var extraction = ExtractEntities(fullText);
            var suggestedTags = SuggestTags(extraction);

            var allEventTags = TagParsingService.MergeTags(
                evt.Tags,
                TagParsingService.MergeTags(
                    suggestedTags.ToList(),
                    TagParsingService.MergeTags(
                        TagParsingService.ExtractTags(evt.Title),
                        TagParsingService.ExtractTags(evt.Description))));

            foreach (var tag in allEventTags)
            {
                if (!tagCounts.TryGetValue(tag, out var info))
                {
                    info = new TagInfo { Name = tag };
                    tagCounts[tag] = info;
                }
                info.EventCount++;

                if (!info.LastUsed.HasValue || evt.StartTime > info.LastUsed)
                {
                    info.LastUsed = evt.StartTime;
                }
            }
        }

        // Count tags from projects
        var allProjects = await _projectRepository.GetAllAsync();
        foreach (var project in allProjects)
        {
            var allProjectTags = TagParsingService.MergeTags(
                project.Tags,
                TagParsingService.MergeTags(
                    TagParsingService.ExtractTags(project.Name),
                    TagParsingService.ExtractTags(project.Description)));

            foreach (var tag in allProjectTags)
            {
                if (!tagCounts.TryGetValue(tag, out var info))
                {
                    info = new TagInfo { Name = tag };
                    tagCounts[tag] = info;
                }
                info.ProjectCount++;

                var projectDate = project.ModifiedAt > project.CreatedAt ? project.ModifiedAt : project.CreatedAt;
                if (!info.LastUsed.HasValue || projectDate > info.LastUsed)
                {
                    info.LastUsed = projectDate;
                }
            }
        }

        return tagCounts.Values
            .OrderByDescending(t => t.TotalCount)
            .ThenByDescending(t => t.LastUsed)
            .ToList();
    }

    #endregion
}

#region Interfaces and Models

/// <summary>
/// Interface for entity extraction service
/// </summary>
public interface IEntityExtractionService
{
    ExtractionResult ExtractEntities(string? text);
    IReadOnlyList<ExtractedEntity> ExtractDates(string text);
    IReadOnlyList<ExtractedEntity> ExtractTimes(string text);
    IReadOnlyList<ExtractedEntity> ExtractDurations(string text);
    IReadOnlyList<ExtractedEntity> ExtractNumbers(string text);
    IReadOnlyList<ExtractedEntity> ExtractUrls(string text);
    IReadOnlyList<ExtractedEntity> ExtractEmails(string text);
    IReadOnlyList<ExtractedEntity> ExtractPhoneNumbers(string text);
    IReadOnlyList<ExtractedEntity> ExtractIpAddresses(string text);
    IReadOnlyList<ExtractedEntity> ExtractMoneyAmounts(string text);
    IReadOnlyList<string> ExtractMentions(string text);
    IReadOnlyList<string> ExtractHashtags(string text);
    IReadOnlyList<ExtractedEntity> ExtractPeople(string text);
    IReadOnlyList<ExtractedEntity> ExtractOrganizations(string text);
    IReadOnlyList<string> ExtractAcronyms(string text);
    IReadOnlyList<string> ExtractKeyPhrases(string text);
    IReadOnlyList<string> SuggestTags(ExtractionResult extraction);

    // Retroactive tagging methods
    Task<List<string>> ExtractAndUpdateTaskTagsAsync(TodoTask task);
    Task<List<string>> ExtractAndUpdateEventTagsAsync(CalendarEvent calendarEvent);
    Task<RetroactiveTaggingResult> ProcessAllEntitiesAsync();
    Task<List<TagInfo>> GetAllTagsAsync();
}

/// <summary>
/// Types of entities that can be extracted
/// </summary>
public enum EntityType
{
    Person,
    Organization,
    Location,
    Date,
    Time,
    Duration,
    Number,
    Money,
    Url,
    Email,
    PhoneNumber,
    IpAddress,
    Hashtag,
    Mention,
    Acronym,
    KeyPhrase
}

/// <summary>
/// A single extracted entity with position information
/// </summary>
public class ExtractedEntity
{
    public EntityType Type { get; set; }
    public string Value { get; set; } = "";
    public int Start { get; set; }
    public int End { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>
/// Result of entity extraction containing all found entities
/// </summary>
public class ExtractionResult
{
    public IReadOnlyList<ExtractedEntity> Dates { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Times { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Durations { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Numbers { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Urls { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Emails { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> PhoneNumbers { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> IpAddresses { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> MoneyAmounts { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> People { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<ExtractedEntity> Organizations { get; set; } = Array.Empty<ExtractedEntity>();
    public IReadOnlyList<string> Mentions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Hashtags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Acronyms { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> KeyPhrases { get; set; } = Array.Empty<string>();
    public IReadOnlyList<TermFrequency> RepeatedTerms { get; set; } = Array.Empty<TermFrequency>();

    // Backward compatibility: ProperNouns are now People entities
    public IReadOnlyList<string> ProperNouns => People.Select(p => p.Value).ToList();

    public bool HasEntities =>
        Dates.Any() || Times.Any() || Durations.Any() || Numbers.Any() ||
        Urls.Any() || Emails.Any() || PhoneNumbers.Any() || IpAddresses.Any() ||
        MoneyAmounts.Any() || People.Any() || Organizations.Any() ||
        Mentions.Any() || Hashtags.Any() || Acronyms.Any() || KeyPhrases.Any();

    public int TotalEntityCount =>
        Dates.Count + Times.Count + Durations.Count + Numbers.Count +
        Urls.Count + Emails.Count + PhoneNumbers.Count + IpAddresses.Count +
        MoneyAmounts.Count + People.Count + Organizations.Count +
        Mentions.Count + Hashtags.Count + Acronyms.Count + KeyPhrases.Count;
}

/// <summary>
/// A term and its frequency count
/// </summary>
public record TermFrequency(string Term, int Count);

/// <summary>
/// Result of retroactive tag/entity extraction
/// </summary>
public class RetroactiveTaggingResult
{
    public int TasksProcessed { get; set; }
    public int TasksUpdated { get; set; }
    public int EventsProcessed { get; set; }
    public int EventsUpdated { get; set; }
    public int TotalTagsFound { get; set; }
    public int TotalEntitiesFound { get; set; }
    public int PeopleFound { get; set; }
    public int OrganizationsFound { get; set; }
    public int DatesFound { get; set; }
    public int UrlsFound { get; set; }
}

/// <summary>
/// Information about a tag and its usage
/// </summary>
public class TagInfo
{
    public string Name { get; set; } = "";
    public int TaskCount { get; set; }
    public int EventCount { get; set; }
    public int ProjectCount { get; set; }
    public int TotalCount => TaskCount + EventCount + ProjectCount;
    public DateTime? LastUsed { get; set; }
}

#endregion
