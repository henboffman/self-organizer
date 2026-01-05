using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Intelligence;

public class CategoryMatcherService : ICategoryMatcherService
{
    private readonly IRepository<CategoryDefinition> _repository;
    private List<CategoryDefinition>? _cachedDefinitions;

    private static readonly Dictionary<MeetingCategory, string[]> DefaultMatchTerms = new()
    {
        [MeetingCategory.OneOnOne] = new[] { "1:1", "1-on-1", "one on one", "check-in", "catch up" },
        [MeetingCategory.TeamMeeting] = new[] { "team", "standup", "sync", "all-hands", "staff meeting" },
        [MeetingCategory.ClientMeeting] = new[] { "client", "customer", "external", "partner" },
        [MeetingCategory.Interview] = new[] { "interview", "candidate", "hiring", "recruit" },
        [MeetingCategory.Presentation] = new[] { "present", "demo", "review", "showcase", "pitch" },
        [MeetingCategory.Workshop] = new[] { "workshop", "working session", "brainstorm", "ideation" },
        [MeetingCategory.Planning] = new[] { "planning", "roadmap", "strategy", "quarterly", "sprint" },
        [MeetingCategory.Training] = new[] { "training", "learning", "onboarding", "course" },
        [MeetingCategory.Focus] = new[] { "focus time", "no meetings", "blocked", "deep work" },
        [MeetingCategory.StatusUpdate] = new[] { "status", "update", "report", "weekly" },
        [MeetingCategory.Review] = new[] { "review", "retrospective", "retro", "feedback" },
        [MeetingCategory.Social] = new[] { "lunch", "coffee", "happy hour", "social", "team building" },
        [MeetingCategory.Break] = new[] { "break", "lunch break", "personal" },
        [MeetingCategory.BrainStorming] = new[] { "brainstorm", "ideation", "creative" }
    };

    public CategoryMatcherService(IRepository<CategoryDefinition> repository)
    {
        _repository = repository;
    }

    public MeetingCategory? MatchCategory(string title, string? description = null)
    {
        var matches = GetPossibleMatches(title + " " + (description ?? ""));
        var bestMatch = matches.OrderByDescending(m => m.Confidence).FirstOrDefault();
        return bestMatch?.Confidence >= 0.5 ? bestMatch.Category : null;
    }

    public IEnumerable<CategoryMatch> GetPossibleMatches(string text)
    {
        var lowerText = text.ToLowerInvariant();
        var matches = new List<CategoryMatch>();

        foreach (var (category, terms) in DefaultMatchTerms)
        {
            var matchCount = terms.Count(term => lowerText.Contains(term.ToLowerInvariant()));
            if (matchCount > 0)
            {
                var confidence = Math.Min(1.0, matchCount * 0.4);
                matches.Add(new CategoryMatch(category, confidence));
            }
        }

        return matches;
    }

    public async Task<IEnumerable<CategoryDefinition>> GetCategoryDefinitionsAsync()
    {
        if (_cachedDefinitions == null)
        {
            _cachedDefinitions = (await _repository.GetAllAsync()).ToList();

            // Seed default definitions if empty
            if (!_cachedDefinitions.Any())
            {
                await SeedDefaultDefinitionsAsync();
                _cachedDefinitions = (await _repository.GetAllAsync()).ToList();
            }
        }
        return _cachedDefinitions;
    }

    public async Task<CategoryDefinition> AddCategoryDefinitionAsync(CategoryDefinition definition)
    {
        var result = await _repository.AddAsync(definition);
        _cachedDefinitions = null;
        return result;
    }

    public async Task<CategoryDefinition> UpdateCategoryDefinitionAsync(CategoryDefinition definition)
    {
        var result = await _repository.UpdateAsync(definition);
        _cachedDefinitions = null;
        return result;
    }

    public async Task DeleteCategoryDefinitionAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        _cachedDefinitions = null;
    }

    private async Task SeedDefaultDefinitionsAsync()
    {
        var defaults = new List<CategoryDefinition>
        {
            new() { Name = "OneOnOne", Color = "#4CAF50", MatchTerms = DefaultMatchTerms[MeetingCategory.OneOnOne].ToList(), DefaultPrepMinutes = 5, DefaultDecompressMinutes = 5, DefaultEnergyRequired = 2 },
            new() { Name = "TeamMeeting", Color = "#2196F3", MatchTerms = DefaultMatchTerms[MeetingCategory.TeamMeeting].ToList(), DefaultPrepMinutes = 5, DefaultDecompressMinutes = 5, DefaultEnergyRequired = 2 },
            new() { Name = "ClientMeeting", Color = "#FF9800", MatchTerms = DefaultMatchTerms[MeetingCategory.ClientMeeting].ToList(), DefaultPrepMinutes = 15, DefaultDecompressMinutes = 10, DefaultEnergyRequired = 4, TypicallyRequiresFollowUp = true },
            new() { Name = "Interview", Color = "#9C27B0", MatchTerms = DefaultMatchTerms[MeetingCategory.Interview].ToList(), DefaultPrepMinutes = 20, DefaultDecompressMinutes = 15, DefaultEnergyRequired = 4, TypicallyRequiresFollowUp = true },
            new() { Name = "Presentation", Color = "#E91E63", MatchTerms = DefaultMatchTerms[MeetingCategory.Presentation].ToList(), DefaultPrepMinutes = 30, DefaultDecompressMinutes = 15, DefaultEnergyRequired = 5 },
            new() { Name = "Workshop", Color = "#00BCD4", MatchTerms = DefaultMatchTerms[MeetingCategory.Workshop].ToList(), DefaultPrepMinutes = 15, DefaultDecompressMinutes = 15, DefaultEnergyRequired = 4 },
            new() { Name = "Planning", Color = "#3F51B5", MatchTerms = DefaultMatchTerms[MeetingCategory.Planning].ToList(), DefaultPrepMinutes = 10, DefaultDecompressMinutes = 10, DefaultEnergyRequired = 3 },
            new() { Name = "Training", Color = "#009688", MatchTerms = DefaultMatchTerms[MeetingCategory.Training].ToList(), DefaultPrepMinutes = 10, DefaultDecompressMinutes = 10, DefaultEnergyRequired = 2 },
            new() { Name = "Focus", Color = "#607D8B", MatchTerms = DefaultMatchTerms[MeetingCategory.Focus].ToList(), DefaultPrepMinutes = 0, DefaultDecompressMinutes = 0, DefaultEnergyRequired = 3 }
        };

        foreach (var def in defaults)
        {
            await _repository.AddAsync(def);
        }
    }
}
