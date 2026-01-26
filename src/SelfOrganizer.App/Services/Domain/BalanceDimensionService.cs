using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class BalanceDimensionService : IBalanceDimensionService
{
    private readonly IRepository<UserPreferences> _preferencesRepository;
    private readonly IRepository<Goal> _goalRepository;
    private readonly IContextService _contextService;

    // Work Mode Dimensions (8) - Professional focus
    private static readonly BalanceDimension[] WorkDimensions = new[]
    {
        new BalanceDimension
        {
            Id = "career-growth",
            Name = "Career Growth",
            Description = "Advancing your career path, promotions, and professional development",
            Icon = "graph-up",
            Color = "#0d6efd",
            ApplicableModes = new[] { AppMode.Work, AppMode.Balanced },
            CategoryMappings = new[] { "Career" },
            KeywordMappings = new[] { "promotion", "career", "advance", "raise", "title", "role", "leadership" },
            SortOrder = 1
        },
        new BalanceDimension
        {
            Id = "technical-skills",
            Name = "Technical Skills",
            Description = "Developing expertise in your craft and staying current",
            Icon = "code-slash",
            Color = "#6610f2",
            ApplicableModes = new[] { AppMode.Work },
            CategoryMappings = new[] { "Learning" },
            KeywordMappings = new[] { "learn", "skill", "training", "certification", "course", "technology" },
            SortOrder = 2
        },
        new BalanceDimension
        {
            Id = "leadership",
            Name = "Leadership",
            Description = "Developing leadership capabilities and influence",
            Icon = "people",
            Color = "#fd7e14",
            ApplicableModes = new[] { AppMode.Work, AppMode.Balanced },
            CategoryMappings = new[] { "Career" },
            KeywordMappings = new[] { "lead", "manage", "mentor", "coach", "team", "influence", "strategy" },
            SortOrder = 3
        },
        new BalanceDimension
        {
            Id = "professional-network",
            Name = "Professional Network",
            Description = "Building and maintaining professional relationships",
            Icon = "share",
            Color = "#20c997",
            ApplicableModes = new[] { AppMode.Work },
            CategoryMappings = new[] { "Relationships" },
            KeywordMappings = new[] { "network", "connect", "colleague", "mentor", "sponsor", "linkedin" },
            SortOrder = 4
        },
        new BalanceDimension
        {
            Id = "productivity",
            Name = "Productivity",
            Description = "Getting things done efficiently and effectively",
            Icon = "lightning",
            Color = "#ffc107",
            ApplicableModes = new[] { AppMode.Work, AppMode.Balanced },
            CategoryMappings = new[] { "Personal" },
            KeywordMappings = new[] { "efficiency", "productive", "focus", "time", "workflow", "organize" },
            SortOrder = 5
        },
        new BalanceDimension
        {
            Id = "innovation",
            Name = "Innovation",
            Description = "Creating new ideas and improving processes",
            Icon = "lightbulb",
            Color = "#e83e8c",
            ApplicableModes = new[] { AppMode.Work },
            CategoryMappings = new[] { "Creative" },
            KeywordMappings = new[] { "innovate", "create", "improve", "idea", "experiment", "prototype" },
            SortOrder = 6
        },
        new BalanceDimension
        {
            Id = "work-quality",
            Name = "Work Quality",
            Description = "Delivering excellent results and exceeding expectations",
            Icon = "star",
            Color = "#28a745",
            ApplicableModes = new[] { AppMode.Work },
            CategoryMappings = new[] { "Career" },
            KeywordMappings = new[] { "quality", "excellence", "performance", "deliverable", "standard" },
            SortOrder = 7
        },
        new BalanceDimension
        {
            Id = "work-life-boundary",
            Name = "Work-Life Boundary",
            Description = "Maintaining healthy boundaries between work and personal life",
            Icon = "door-closed",
            Color = "#17a2b8",
            ApplicableModes = new[] { AppMode.Work, AppMode.Balanced },
            CategoryMappings = new[] { "Health", "Personal" },
            KeywordMappings = new[] { "boundary", "balance", "disconnect", "vacation", "weekend", "rest" },
            SortOrder = 8
        }
    };

    // Life Mode Dimensions (8) - Personal focus
    private static readonly BalanceDimension[] LifeDimensions = new[]
    {
        new BalanceDimension
        {
            Id = "work",
            Name = "Work",
            Description = "Career and professional responsibilities",
            Icon = "briefcase",
            Color = "#17a2b8",
            ApplicableModes = new[] { AppMode.Life },
            CategoryMappings = new[] { "Career" },
            KeywordMappings = new[] { "work", "job", "career", "profession", "office" },
            SortOrder = 1
        },
        new BalanceDimension
        {
            Id = "health",
            Name = "Health",
            Description = "Physical and mental wellbeing",
            Icon = "heart-pulse",
            Color = "#dc3545",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Health" },
            KeywordMappings = new[] { "health", "exercise", "fitness", "mental", "sleep", "diet", "wellness" },
            SortOrder = 2
        },
        new BalanceDimension
        {
            Id = "relationships",
            Name = "Relationships",
            Description = "Friends, romantic partner, and social connections",
            Icon = "heart",
            Color = "#e83e8c",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Relationships" },
            KeywordMappings = new[] { "friend", "partner", "relationship", "social", "dating", "love" },
            SortOrder = 3
        },
        new BalanceDimension
        {
            Id = "family",
            Name = "Family",
            Description = "Family relationships and responsibilities",
            Icon = "house-heart",
            Color = "#fd7e14",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Relationships", "Personal" },
            KeywordMappings = new[] { "family", "parent", "child", "sibling", "relative", "home" },
            SortOrder = 4
        },
        new BalanceDimension
        {
            Id = "finance",
            Name = "Finance",
            Description = "Financial health and security",
            Icon = "currency-dollar",
            Color = "#28a745",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Financial" },
            KeywordMappings = new[] { "money", "save", "invest", "budget", "debt", "retirement", "financial" },
            SortOrder = 5
        },
        new BalanceDimension
        {
            Id = "growth",
            Name = "Growth",
            Description = "Personal development and learning",
            Icon = "graph-up-arrow",
            Color = "#6610f2",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Learning", "Personal" },
            KeywordMappings = new[] { "learn", "grow", "develop", "read", "course", "skill", "education" },
            SortOrder = 6
        },
        new BalanceDimension
        {
            Id = "recreation",
            Name = "Recreation",
            Description = "Fun, hobbies, and leisure activities",
            Icon = "controller",
            Color = "#ffc107",
            ApplicableModes = new[] { AppMode.Life, AppMode.Balanced },
            CategoryMappings = new[] { "Creative", "Personal" },
            KeywordMappings = new[] { "hobby", "fun", "play", "game", "travel", "vacation", "leisure" },
            SortOrder = 7
        },
        new BalanceDimension
        {
            Id = "contribution",
            Name = "Contribution",
            Description = "Giving back to community and causes you care about",
            Icon = "hand-holding-heart",
            Color = "#20c997",
            ApplicableModes = new[] { AppMode.Life },
            CategoryMappings = new[] { "Personal", "Other" },
            KeywordMappings = new[] { "volunteer", "donate", "community", "charity", "help", "cause", "give" },
            SortOrder = 8
        }
    };

    // All unique dimensions for lookup
    private static readonly Dictionary<string, BalanceDimension> AllDimensionsById;

    static BalanceDimensionService()
    {
        AllDimensionsById = WorkDimensions
            .Concat(LifeDimensions)
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public BalanceDimensionService(
        IRepository<UserPreferences> preferencesRepository,
        IRepository<Goal> goalRepository,
        IContextService contextService)
    {
        _preferencesRepository = preferencesRepository;
        _goalRepository = goalRepository;
        _contextService = contextService;
    }

    public Task<IReadOnlyList<BalanceDimension>> GetDimensionsForModeAsync(AppMode mode)
    {
        var dimensions = mode switch
        {
            AppMode.Work => WorkDimensions.ToList(),
            AppMode.Life => LifeDimensions.ToList(),
            AppMode.Balanced => GetBalancedDimensions(),
            _ => LifeDimensions.ToList()
        };

        return Task.FromResult<IReadOnlyList<BalanceDimension>>(dimensions);
    }

    private static List<BalanceDimension> GetBalancedDimensions()
    {
        // Curated mix of 10 dimensions for Balanced mode
        var balancedIds = new[]
        {
            "career-growth", "health", "relationships", "family", "finance",
            "growth", "recreation", "leadership", "productivity", "work-life-boundary"
        };

        return balancedIds
            .Where(id => AllDimensionsById.ContainsKey(id))
            .Select(id => AllDimensionsById[id])
            .OrderBy(d => d.SortOrder)
            .ToList();
    }

    public async Task<AppMode> GetCurrentModeAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs?.AppMode ?? AppMode.Balanced;
    }

    public async Task SetModeAsync(AppMode mode, bool seedContexts = true)
    {
        var prefs = await GetOrCreatePreferencesAsync();

        // Save current ratings to mode-specific storage before switching
        if (prefs.LifeAreaRatings != null && prefs.LifeAreaRatings.Count > 0)
        {
            prefs.BalanceRatingsByMode ??= new Dictionary<string, Dictionary<string, int>>();
            prefs.BalanceRatingsByMode[prefs.AppMode.ToString()] = prefs.LifeAreaRatings;
        }

        // Switch mode
        prefs.AppMode = mode;
        prefs.AppModeSetAt = DateTime.UtcNow;

        // Restore ratings for new mode if they exist
        if (prefs.BalanceRatingsByMode?.TryGetValue(mode.ToString(), out var savedRatings) == true)
        {
            prefs.LifeAreaRatings = savedRatings;
        }
        else
        {
            // Initialize empty ratings for new mode
            prefs.LifeAreaRatings = new Dictionary<string, int>();
        }

        await _preferencesRepository.UpdateAsync(prefs);

        // Seed contexts for the new mode if requested
        if (seedContexts)
        {
            await SeedContextsForModeAsync(mode);
        }
    }

    public async Task<List<string>> SuggestDimensionsForGoalAsync(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        var suggestions = new List<string>();
        var mode = await GetCurrentModeAsync();
        var dimensions = await GetDimensionsForModeAsync(mode);

        // Match by category
        var categoryName = goal.Category.ToString();
        foreach (var dim in dimensions)
        {
            if (dim.CategoryMappings.Contains(categoryName))
            {
                if (!suggestions.Contains(dim.Id))
                    suggestions.Add(dim.Id);
            }
        }

        // Match by keywords in title and description
        var searchText = $"{goal.Title} {goal.Description}".ToLowerInvariant();
        foreach (var dim in dimensions)
        {
            if (dim.KeywordMappings.Any(kw => searchText.Contains(kw.ToLowerInvariant())))
            {
                if (!suggestions.Contains(dim.Id))
                    suggestions.Add(dim.Id);
            }
        }

        // Limit to top 3 suggestions
        return suggestions.Take(3).ToList();
    }

    public async Task<IReadOnlyList<Goal>> GetGoalsForDimensionAsync(string dimensionId)
    {
        var goals = await _goalRepository.GetAllAsync();

        return goals
            .Where(g => g.Status == GoalStatus.Active &&
                       (g.BalanceDimensionIds.Contains(dimensionId) ||
                        g.PrimaryBalanceDimensionId == dimensionId))
            .OrderByDescending(g => g.BalanceImpact)
            .ThenBy(g => g.TargetDate)
            .ToList();
    }

    public async Task<DimensionAnalysis> AnalyzeDimensionAsync(string dimensionId)
    {
        var goals = await _goalRepository.GetAllAsync();
        var linkedGoals = goals.Where(g =>
            g.BalanceDimensionIds.Contains(dimensionId) ||
            g.PrimaryBalanceDimensionId == dimensionId).ToList();

        var activeGoals = linkedGoals.Where(g => g.Status == GoalStatus.Active).ToList();
        var recentCompleted = linkedGoals.Where(g =>
            g.Status == GoalStatus.Completed &&
            g.CompletedAt.HasValue &&
            g.CompletedAt.Value > DateTime.UtcNow.AddDays(-90)).ToList();

        var prefs = await GetPreferencesAsync();
        var currentRating = prefs?.LifeAreaRatings?.GetValueOrDefault(dimensionId);

        // Determine if this dimension needs attention
        var needsAttention = currentRating.HasValue && currentRating.Value <= 4 && activeGoals.Count == 0;
        var suggestion = needsAttention
            ? "Consider setting a goal in this area to improve your balance."
            : null;

        return new DimensionAnalysis
        {
            DimensionId = dimensionId,
            ActiveGoalCount = activeGoals.Count,
            RecentCompletedCount = recentCompleted.Count,
            AverageProgress = activeGoals.Count > 0
                ? (int)activeGoals.Average(g => g.ProgressPercent)
                : 0,
            CurrentRating = currentRating,
            Trend = 0, // TODO: Calculate from historical data
            NeedsAttention = needsAttention,
            Suggestion = suggestion
        };
    }

    public async Task<IReadOnlyList<BalanceDimension>> GetEnabledDimensionsAsync()
    {
        var prefs = await GetPreferencesAsync();
        var mode = prefs?.AppMode ?? AppMode.Balanced;
        var allDimensions = await GetDimensionsForModeAsync(mode);

        if (prefs?.EnabledBalanceDimensions == null || prefs.EnabledBalanceDimensions.Count == 0)
        {
            return allDimensions;
        }

        return allDimensions
            .Where(d => prefs.EnabledBalanceDimensions.Contains(d.Id))
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetBalanceRatingsAsync()
    {
        var prefs = await GetPreferencesAsync();
        return prefs?.LifeAreaRatings ?? new Dictionary<string, int>();
    }

    public async Task SaveBalanceRatingsAsync(Dictionary<string, int> ratings)
    {
        ArgumentNullException.ThrowIfNull(ratings);
        var prefs = await GetOrCreatePreferencesAsync();
        prefs.LifeAreaRatings = ratings;
        prefs.LifeAreaAssessmentDate = DateTime.UtcNow;

        // Also save to mode-specific storage
        var mode = prefs.AppMode;
        prefs.BalanceRatingsByMode ??= new Dictionary<string, Dictionary<string, int>>();
        prefs.BalanceRatingsByMode[mode.ToString()] = ratings;

        await _preferencesRepository.UpdateAsync(prefs);
    }

    private async Task<UserPreferences?> GetPreferencesAsync()
    {
        var prefs = await _preferencesRepository.GetAllAsync();
        return prefs.FirstOrDefault();
    }

    private async Task<UserPreferences> GetOrCreatePreferencesAsync()
    {
        var prefs = await GetPreferencesAsync();
        if (prefs == null)
        {
            prefs = new UserPreferences
            {
                AppMode = AppMode.Balanced,
                AppModeSetAt = DateTime.UtcNow
            };
            prefs = await _preferencesRepository.AddAsync(prefs);
        }
        return prefs;
    }

    private async Task SeedContextsForModeAsync(AppMode mode)
    {
        // Get mode-specific contexts to seed
        var contextDefinitions = GetContextsForMode(mode);

        foreach (var (name, icon, color) in contextDefinitions)
        {
            try
            {
                var existing = await _contextService.GetByNameAsync(name);
                if (existing == null)
                {
                    await _contextService.CreateAsync(name, icon, color);
                }
            }
            catch
            {
                // Context may already exist, that's fine
            }
        }
    }

    private static (string Name, string Icon, string Color)[] GetContextsForMode(AppMode mode)
    {
        return mode switch
        {
            AppMode.Work => new[]
            {
                ("deep-work", "bullseye", "#0d6efd"),
                ("meetings", "people", "#6c757d"),
                ("1-on-1", "person", "#fd7e14"),
                ("planning", "calendar3", "#6610f2"),
                ("admin", "folder", "#20c997"),
                ("review", "check2-circle", "#28a745"),
                ("email", "envelope-closed", "#0d6efd"),
                ("calls", "phone", "#dc3545"),
                ("collaborate", "people-fill", "#e83e8c"),
                ("research", "search", "#17a2b8")
            },
            AppMode.Life => new[]
            {
                ("home", "home", "#e83e8c"),
                ("errands", "location", "#20c997"),
                ("computer", "laptop", "#6610f2"),
                ("phone", "phone", "#dc3545"),
                ("read", "book", "#fd7e14"),
                ("think", "lightbulb", "#6c757d"),
                ("weekend", "sun", "#ffc107"),
                ("evening", "moon", "#17a2b8"),
                ("outdoors", "tree", "#28a745"),
                ("anywhere", "globe", "#0d6efd")
            },
            AppMode.Balanced => new[]
            {
                ("work", "briefcase", "#17a2b8"),
                ("home", "home", "#e83e8c"),
                ("deep-work", "bullseye", "#0d6efd"),
                ("meetings", "people", "#6c757d"),
                ("errands", "location", "#20c997"),
                ("computer", "laptop", "#6610f2"),
                ("phone", "phone", "#dc3545"),
                ("email", "envelope-closed", "#0d6efd"),
                ("read", "book", "#fd7e14"),
                ("anywhere", "globe", "#ffc107")
            },
            _ => Array.Empty<(string, string, string)>()
        };
    }
}
