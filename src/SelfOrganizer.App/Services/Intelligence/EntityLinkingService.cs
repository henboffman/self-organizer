using System.Text.RegularExpressions;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

/// <summary>
/// Service for automatically linking calendar events to entities (projects, goals, tasks, ideas)
/// based on configurable pattern matching rules.
/// </summary>
public interface IEntityLinkingService
{
    /// <summary>
    /// Analyzes a calendar event and automatically links it to relevant entities based on rules.
    /// </summary>
    Task<EntityLinkResult> AnalyzeAndLinkEventAsync(CalendarEvent evt);

    /// <summary>
    /// Gets potential entity links for an event without persisting them.
    /// </summary>
    Task<IEnumerable<EntityLinkMatch>> GetPotentialLinksAsync(CalendarEvent evt);

    /// <summary>
    /// Batch analyze multiple events for entity linking.
    /// </summary>
    Task<IEnumerable<EntityLinkResult>> AnalyzeAndLinkEventsAsync(IEnumerable<CalendarEvent> events);

    /// <summary>
    /// Gets all entity link rules.
    /// </summary>
    Task<IEnumerable<EntityLinkRule>> GetRulesAsync();

    /// <summary>
    /// Creates a new entity link rule.
    /// </summary>
    Task<EntityLinkRule> CreateRuleAsync(EntityLinkRule rule);

    /// <summary>
    /// Updates an existing entity link rule.
    /// </summary>
    Task UpdateRuleAsync(EntityLinkRule rule);

    /// <summary>
    /// Deletes an entity link rule.
    /// </summary>
    Task DeleteRuleAsync(Guid ruleId);

    /// <summary>
    /// Creates a new rule based on an existing event and target entity.
    /// </summary>
    Task<EntityLinkRule> CreateRuleFromEventAsync(CalendarEvent evt, Guid targetEntityId, EntityLinkTargetType targetType, string? ruleName = null);

    /// <summary>
    /// Manually links an event to an entity without using rules.
    /// </summary>
    Task LinkEventToEntityAsync(Guid eventId, Guid entityId, EntityLinkTargetType targetType);

    /// <summary>
    /// Removes a link between an event and an entity.
    /// </summary>
    Task UnlinkEventFromEntityAsync(Guid eventId, Guid entityId, EntityLinkTargetType targetType);
}

/// <summary>
/// Result of entity link analysis for a single event.
/// </summary>
public class EntityLinkResult
{
    public Guid EventId { get; set; }
    public bool WasModified { get; set; }
    public List<EntityLinkMatch> AppliedLinks { get; set; } = new();
    public List<string> MatchedRuleNames { get; set; } = new();
}

/// <summary>
/// A potential or applied entity link match.
/// </summary>
public class EntityLinkMatch
{
    public EntityLinkTargetType TargetType { get; set; }
    public Guid TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public double Confidence { get; set; }
    public string? MatchedPattern { get; set; }
    public string? RuleName { get; set; }
}

public class EntityLinkingService : IEntityLinkingService
{
    private readonly IRepository<EntityLinkRule> _ruleRepository;
    private readonly IRepository<CalendarEvent> _eventRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Goal> _goalRepository;
    private readonly IRepository<TodoTask> _taskRepository;
    private readonly IRepository<Idea> _ideaRepository;

    private List<EntityLinkRule>? _cachedRules;

    public EntityLinkingService(
        IRepository<EntityLinkRule> ruleRepository,
        IRepository<CalendarEvent> eventRepository,
        IRepository<Project> projectRepository,
        IRepository<Goal> goalRepository,
        IRepository<TodoTask> taskRepository,
        IRepository<Idea> ideaRepository)
    {
        _ruleRepository = ruleRepository;
        _eventRepository = eventRepository;
        _projectRepository = projectRepository;
        _goalRepository = goalRepository;
        _taskRepository = taskRepository;
        _ideaRepository = ideaRepository;
    }

    public async Task<EntityLinkResult> AnalyzeAndLinkEventAsync(CalendarEvent evt)
    {
        var result = new EntityLinkResult { EventId = evt.Id };
        var potentialLinks = await GetPotentialLinksAsync(evt);
        var linksList = potentialLinks.ToList();

        if (!linksList.Any())
        {
            evt.LastLinkAnalysisAt = DateTime.UtcNow;
            await _eventRepository.UpdateAsync(evt);
            return result;
        }

        // Apply links by type
        var projectLinks = linksList.Where(l => l.TargetType == EntityLinkTargetType.Project).ToList();
        var goalLinks = linksList.Where(l => l.TargetType == EntityLinkTargetType.Goal).ToList();
        var taskLinks = linksList.Where(l => l.TargetType == EntityLinkTargetType.Task).ToList();
        var ideaLinks = linksList.Where(l => l.TargetType == EntityLinkTargetType.Idea).ToList();

        // Take highest confidence project (only one allowed)
        var bestProject = projectLinks.OrderByDescending(l => l.Confidence).FirstOrDefault();
        if (bestProject != null && evt.LinkedProjectId != bestProject.TargetEntityId)
        {
            evt.LinkedProjectId = bestProject.TargetEntityId;
            result.WasModified = true;
            result.AppliedLinks.Add(bestProject);
            if (bestProject.RuleName != null)
                result.MatchedRuleNames.Add(bestProject.RuleName);
        }

        // Add all goal links above threshold
        foreach (var goalLink in goalLinks.Where(l => l.Confidence >= 0.5))
        {
            if (!evt.LinkedGoalIds.Contains(goalLink.TargetEntityId))
            {
                evt.LinkedGoalIds.Add(goalLink.TargetEntityId);
                result.WasModified = true;
                result.AppliedLinks.Add(goalLink);
                if (goalLink.RuleName != null && !result.MatchedRuleNames.Contains(goalLink.RuleName))
                    result.MatchedRuleNames.Add(goalLink.RuleName);
            }
        }

        // Add all task links above threshold
        foreach (var taskLink in taskLinks.Where(l => l.Confidence >= 0.5))
        {
            if (!evt.LinkedTaskIds.Contains(taskLink.TargetEntityId))
            {
                evt.LinkedTaskIds.Add(taskLink.TargetEntityId);
                result.WasModified = true;
                result.AppliedLinks.Add(taskLink);
                if (taskLink.RuleName != null && !result.MatchedRuleNames.Contains(taskLink.RuleName))
                    result.MatchedRuleNames.Add(taskLink.RuleName);
            }
        }

        // Add all idea links above threshold
        foreach (var ideaLink in ideaLinks.Where(l => l.Confidence >= 0.5))
        {
            if (!evt.LinkedIdeaIds.Contains(ideaLink.TargetEntityId))
            {
                evt.LinkedIdeaIds.Add(ideaLink.TargetEntityId);
                result.WasModified = true;
                result.AppliedLinks.Add(ideaLink);
                if (ideaLink.RuleName != null && !result.MatchedRuleNames.Contains(ideaLink.RuleName))
                    result.MatchedRuleNames.Add(ideaLink.RuleName);
            }
        }

        if (result.WasModified)
        {
            evt.IsAutoLinked = true;
        }
        evt.LastLinkAnalysisAt = DateTime.UtcNow;
        await _eventRepository.UpdateAsync(evt);

        return result;
    }

    public async Task<IEnumerable<EntityLinkMatch>> GetPotentialLinksAsync(CalendarEvent evt)
    {
        var matches = new List<EntityLinkMatch>();
        var rules = await GetEnabledRulesAsync();

        // Build searchable text from event
        var titleText = evt.Title ?? string.Empty;
        var descriptionText = evt.Description ?? string.Empty;
        var attendeesText = string.Join(" ", evt.Attendees ?? Enumerable.Empty<string>());
        var tagsText = string.Join(" ", evt.Tags ?? Enumerable.Empty<string>());

        foreach (var rule in rules.OrderByDescending(r => r.Priority))
        {
            if (rule.TargetEntityId == null)
                continue;

            var textToSearch = BuildSearchText(rule, titleText, descriptionText, attendeesText, tagsText);
            var confidence = CalculateMatchConfidence(rule, textToSearch);

            if (confidence >= rule.MinConfidenceScore)
            {
                // Verify target entity still exists
                var entityExists = await VerifyEntityExistsAsync(rule.TargetEntityId.Value, rule.TargetType);
                if (entityExists)
                {
                    matches.Add(new EntityLinkMatch
                    {
                        TargetType = rule.TargetType,
                        TargetEntityId = rule.TargetEntityId.Value,
                        TargetEntityName = rule.TargetEntityName,
                        Confidence = confidence,
                        MatchedPattern = rule.Pattern,
                        RuleName = rule.Name
                    });
                }
            }
        }

        // Also try fuzzy matching against entity names for projects
        var projectMatches = await GetFuzzyProjectMatchesAsync(titleText, descriptionText);
        matches.AddRange(projectMatches.Where(pm => !matches.Any(m =>
            m.TargetType == EntityLinkTargetType.Project && m.TargetEntityId == pm.TargetEntityId)));

        return matches;
    }

    public async Task<IEnumerable<EntityLinkResult>> AnalyzeAndLinkEventsAsync(IEnumerable<CalendarEvent> events)
    {
        var results = new List<EntityLinkResult>();
        foreach (var evt in events)
        {
            var result = await AnalyzeAndLinkEventAsync(evt);
            results.Add(result);
        }
        return results;
    }

    public async Task<IEnumerable<EntityLinkRule>> GetRulesAsync()
    {
        return await _ruleRepository.GetAllAsync();
    }

    public async Task<EntityLinkRule> CreateRuleAsync(EntityLinkRule rule)
    {
        var result = await _ruleRepository.AddAsync(rule);
        _cachedRules = null;
        return result;
    }

    public async Task UpdateRuleAsync(EntityLinkRule rule)
    {
        await _ruleRepository.UpdateAsync(rule);
        _cachedRules = null;
    }

    public async Task DeleteRuleAsync(Guid ruleId)
    {
        await _ruleRepository.DeleteAsync(ruleId);
        _cachedRules = null;
    }

    public async Task<EntityLinkRule> CreateRuleFromEventAsync(
        CalendarEvent evt,
        Guid targetEntityId,
        EntityLinkTargetType targetType,
        string? ruleName = null)
    {
        // Extract a meaningful pattern from the event title
        var pattern = ExtractMeaningfulPattern(evt.Title);
        var targetName = await GetEntityNameAsync(targetEntityId, targetType);

        var rule = new EntityLinkRule
        {
            Name = ruleName ?? $"Auto: {pattern} -> {targetName}",
            RuleType = EntityLinkRuleType.StringMatch,
            Pattern = pattern,
            IsCaseSensitive = false,
            TargetType = targetType,
            TargetEntityId = targetEntityId,
            TargetEntityName = targetName,
            Priority = 10,
            IsEnabled = true,
            ApplyToTitle = true,
            ApplyToDescription = false,
            MinConfidenceScore = 0.6,
            Notes = $"Created from event: {evt.Title}"
        };

        return await CreateRuleAsync(rule);
    }

    public async Task LinkEventToEntityAsync(Guid eventId, Guid entityId, EntityLinkTargetType targetType)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null) return;

        switch (targetType)
        {
            case EntityLinkTargetType.Project:
                evt.LinkedProjectId = entityId;
                break;
            case EntityLinkTargetType.Goal:
                if (!evt.LinkedGoalIds.Contains(entityId))
                    evt.LinkedGoalIds.Add(entityId);
                break;
            case EntityLinkTargetType.Task:
                if (!evt.LinkedTaskIds.Contains(entityId))
                    evt.LinkedTaskIds.Add(entityId);
                break;
            case EntityLinkTargetType.Idea:
                if (!evt.LinkedIdeaIds.Contains(entityId))
                    evt.LinkedIdeaIds.Add(entityId);
                break;
        }

        evt.IsAutoLinked = false; // Manual link
        await _eventRepository.UpdateAsync(evt);
    }

    public async Task UnlinkEventFromEntityAsync(Guid eventId, Guid entityId, EntityLinkTargetType targetType)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null) return;

        switch (targetType)
        {
            case EntityLinkTargetType.Project:
                if (evt.LinkedProjectId == entityId)
                    evt.LinkedProjectId = null;
                break;
            case EntityLinkTargetType.Goal:
                evt.LinkedGoalIds.Remove(entityId);
                break;
            case EntityLinkTargetType.Task:
                evt.LinkedTaskIds.Remove(entityId);
                break;
            case EntityLinkTargetType.Idea:
                evt.LinkedIdeaIds.Remove(entityId);
                break;
        }

        await _eventRepository.UpdateAsync(evt);
    }

    private async Task<IEnumerable<EntityLinkRule>> GetEnabledRulesAsync()
    {
        if (_cachedRules == null)
        {
            var allRules = await _ruleRepository.GetAllAsync();
            _cachedRules = allRules.Where(r => r.IsEnabled).ToList();
        }
        return _cachedRules;
    }

    private static string BuildSearchText(EntityLinkRule rule, string title, string description, string attendees, string tags)
    {
        var parts = new List<string>();

        if (rule.ApplyToTitle)
            parts.Add(title);
        if (rule.ApplyToDescription)
            parts.Add(description);
        if (rule.ApplyToAttendees)
            parts.Add(attendees);
        if (rule.ApplyToTags)
            parts.Add(tags);

        return string.Join(" ", parts);
    }

    private static double CalculateMatchConfidence(EntityLinkRule rule, string textToSearch)
    {
        if (string.IsNullOrWhiteSpace(textToSearch) || string.IsNullOrWhiteSpace(rule.Pattern))
            return 0;

        var comparison = rule.IsCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return rule.RuleType switch
        {
            EntityLinkRuleType.StringMatch => CalculateStringMatchConfidence(rule.Pattern, textToSearch, comparison),
            EntityLinkRuleType.Regex => CalculateRegexConfidence(rule.Pattern, textToSearch, rule.IsCaseSensitive),
            EntityLinkRuleType.CategoryMatch => CalculateCategoryMatchConfidence(rule.Pattern, textToSearch),
            EntityLinkRuleType.AttendeeMatch => CalculateAttendeeMatchConfidence(rule.Pattern, textToSearch, comparison),
            _ => 0
        };
    }

    private static double CalculateStringMatchConfidence(string pattern, string text, StringComparison comparison)
    {
        // Exact match gets full confidence
        if (text.Equals(pattern, comparison))
            return 1.0;

        // Contains match
        if (text.Contains(pattern, comparison))
        {
            // Score based on how much of the text the pattern represents
            var ratio = (double)pattern.Length / text.Length;
            return Math.Min(0.9, 0.5 + ratio * 0.4);
        }

        // Word boundary match (pattern appears as a whole word)
        var wordPattern = @"\b" + Regex.Escape(pattern) + @"\b";
        var options = comparison == StringComparison.OrdinalIgnoreCase
            ? RegexOptions.IgnoreCase
            : RegexOptions.None;

        if (Regex.IsMatch(text, wordPattern, options))
            return 0.85;

        return 0;
    }

    private static double CalculateRegexConfidence(string pattern, string text, bool caseSensitive)
    {
        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(100));
            var matches = regex.Matches(text);

            if (matches.Count == 0)
                return 0;

            // More matches = higher confidence
            var matchLength = matches.Sum(m => m.Length);
            var ratio = (double)matchLength / text.Length;
            return Math.Min(0.95, 0.6 + ratio * 0.35);
        }
        catch
        {
            return 0;
        }
    }

    private static double CalculateCategoryMatchConfidence(string categoryPattern, string text)
    {
        // Category patterns are comma-separated category names to match
        var categories = categoryPattern.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim().ToLowerInvariant());

        var lowerText = text.ToLowerInvariant();
        var matchCount = categories.Count(c => lowerText.Contains(c));

        if (matchCount == 0)
            return 0;

        return Math.Min(0.9, 0.5 + matchCount * 0.2);
    }

    private static double CalculateAttendeeMatchConfidence(string pattern, string attendeesText, StringComparison comparison)
    {
        if (string.IsNullOrWhiteSpace(attendeesText))
            return 0;

        // Pattern could be email domain or name pattern
        if (pattern.StartsWith("@"))
        {
            // Email domain match
            var domain = pattern.ToLowerInvariant();
            if (attendeesText.ToLowerInvariant().Contains(domain))
                return 0.9;
        }
        else if (attendeesText.Contains(pattern, comparison))
        {
            return 0.85;
        }

        return 0;
    }

    private async Task<bool> VerifyEntityExistsAsync(Guid entityId, EntityLinkTargetType targetType)
    {
        return targetType switch
        {
            EntityLinkTargetType.Project => await _projectRepository.GetByIdAsync(entityId) != null,
            EntityLinkTargetType.Goal => await _goalRepository.GetByIdAsync(entityId) != null,
            EntityLinkTargetType.Task => await _taskRepository.GetByIdAsync(entityId) != null,
            EntityLinkTargetType.Idea => await _ideaRepository.GetByIdAsync(entityId) != null,
            _ => false
        };
    }

    private async Task<string?> GetEntityNameAsync(Guid entityId, EntityLinkTargetType targetType)
    {
        return targetType switch
        {
            EntityLinkTargetType.Project => (await _projectRepository.GetByIdAsync(entityId))?.Name,
            EntityLinkTargetType.Goal => (await _goalRepository.GetByIdAsync(entityId))?.Title,
            EntityLinkTargetType.Task => (await _taskRepository.GetByIdAsync(entityId))?.Title,
            EntityLinkTargetType.Idea => (await _ideaRepository.GetByIdAsync(entityId))?.Title,
            _ => null
        };
    }

    private async Task<List<EntityLinkMatch>> GetFuzzyProjectMatchesAsync(string title, string description)
    {
        var matches = new List<EntityLinkMatch>();
        var projects = await _projectRepository.GetAllAsync();
        var searchText = (title + " " + description).ToLowerInvariant();

        foreach (var project in projects.Where(p => p.Status != ProjectStatus.Completed))
        {
            var projectName = project.Name.ToLowerInvariant();
            double confidence = 0;

            // Check if project name appears in title (high confidence)
            if (title.ToLowerInvariant().Contains(projectName))
            {
                confidence = 0.8;
            }
            // Check if project name appears in description (lower confidence)
            else if (description?.ToLowerInvariant().Contains(projectName) == true)
            {
                confidence = 0.6;
            }
            // Check for partial word matches
            else
            {
                var projectWords = projectName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var matchingWords = projectWords.Count(w => w.Length > 3 && searchText.Contains(w));
                if (matchingWords > 0)
                {
                    confidence = Math.Min(0.7, 0.3 + matchingWords * 0.2);
                }
            }

            if (confidence >= 0.5)
            {
                matches.Add(new EntityLinkMatch
                {
                    TargetType = EntityLinkTargetType.Project,
                    TargetEntityId = project.Id,
                    TargetEntityName = project.Name,
                    Confidence = confidence,
                    MatchedPattern = "Fuzzy name match",
                    RuleName = null
                });
            }
        }

        return matches;
    }

    private static string ExtractMeaningfulPattern(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Remove common meeting prefixes/suffixes
        var cleanedTitle = title;
        var removePatterns = new[] { "meeting", "sync", "call", "check-in", "standup", "review" };

        foreach (var pattern in removePatterns)
        {
            cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{pattern}\b", "", RegexOptions.IgnoreCase).Trim();
        }

        // If we have meaningful content left, use it
        if (cleanedTitle.Length >= 3)
            return cleanedTitle.Trim();

        // Fall back to first few words of original title
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(3));
    }
}
