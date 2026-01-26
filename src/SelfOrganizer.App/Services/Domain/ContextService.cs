using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class ContextService : IContextService
{
    private readonly IRepository<Context> _repository;
    private bool _initialized = false;

    // Default built-in contexts following GTD methodology (used as fallback)
    private static readonly (string Name, string Icon, string Color)[] DefaultContexts = new[]
    {
        ("think", "lightbulb", "#6c757d"),      // Planning, brainstorming
        ("do", "wrench", "#28a745"),            // Physical tasks, hands-on work
        ("email", "envelope-closed", "#0d6efd"), // Email-based tasks
        ("read", "book", "#fd7e14"),            // Reading, research
        ("call", "phone", "#dc3545"),           // Phone calls
        ("errand", "location", "#20c997"),      // Out and about
        ("computer", "laptop", "#6610f2"),      // Computer-based work
        ("home", "home", "#e83e8c"),            // Tasks at home
        ("work", "briefcase", "#17a2b8"),       // Tasks at work/office
        ("anywhere", "globe", "#ffc107"),       // Location-independent
    };

    // Work Mode Contexts (10) - Professional focus
    private static readonly (string Name, string Icon, string Color)[] WorkModeContexts = new[]
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
    };

    // Life Mode Contexts (10) - Personal focus
    private static readonly (string Name, string Icon, string Color)[] LifeModeContexts = new[]
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
    };

    // Balanced Mode Contexts (10) - Curated mix
    private static readonly (string Name, string Icon, string Color)[] BalancedModeContexts = new[]
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
    };

    public ContextService(IRepository<Context> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Context>> GetAllSortedByMruAsync()
    {
        await EnsureBuiltInContextsAsync();

        var contexts = await _repository.QueryAsync(c => c.IsActive);

        // Sort by: most recently used first, then by usage count, then by sort order
        return contexts
            .OrderByDescending(c => c.LastUsedAt ?? DateTime.MinValue)
            .ThenByDescending(c => c.UsageCount)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name);
    }

    public async Task<Context?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Context?> GetByNameAsync(string name)
    {
        var contexts = await _repository.QueryAsync(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return contexts.FirstOrDefault();
    }

    public async Task<Context> CreateAsync(string name, string? icon = null, string? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        // Check if context already exists
        var existing = await GetByNameAsync(name);
        if (existing != null)
        {
            throw new InvalidOperationException($"Context '{name}' already exists");
        }

        var allContexts = await _repository.GetAllAsync();
        var maxSortOrder = allContexts.Any() ? allContexts.Max(c => c.SortOrder) : 0;

        var context = new Context
        {
            Name = name.ToLowerInvariant().Trim(),
            Icon = icon,
            Color = color ?? GenerateColor(name),
            IsActive = true,
            SortOrder = maxSortOrder + 1,
            IsBuiltIn = false,
            UsageCount = 0
        };

        return await _repository.AddAsync(context);
    }

    public async Task<Context> UpdateAsync(Context context)
    {
        return await _repository.UpdateAsync(context);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var context = await _repository.GetByIdAsync(id);
        if (context == null)
            return false;

        // Cannot delete built-in contexts
        if (context.IsBuiltIn)
            return false;

        // Soft delete by marking inactive
        context.IsActive = false;
        await _repository.UpdateAsync(context);
        return true;
    }

    public async Task RecordUsageAsync(string contextName)
    {
        await EnsureBuiltInContextsAsync();

        var context = await GetByNameAsync(contextName);
        if (context == null)
        {
            // Create it on the fly if it doesn't exist
            context = await CreateAsync(contextName);
        }

        context.LastUsedAt = DateTime.UtcNow;
        context.UsageCount++;
        await _repository.UpdateAsync(context);
    }

    public async Task RecordUsageAsync(IEnumerable<string> contextNames)
    {
        foreach (var name in contextNames)
        {
            await RecordUsageAsync(name);
        }
    }

    public async Task EnsureBuiltInContextsAsync()
    {
        if (_initialized)
            return;

        var existingContexts = await _repository.GetAllAsync();

        if (!existingContexts.Any())
        {
            // First time - seed all default contexts
            for (int i = 0; i < DefaultContexts.Length; i++)
            {
                var (name, icon, color) = DefaultContexts[i];
                var context = new Context
                {
                    Name = name,
                    Icon = icon,
                    Color = color,
                    IsActive = true,
                    SortOrder = i,
                    IsBuiltIn = true,
                    UsageCount = 0
                };
                await _repository.AddAsync(context);
            }
        }
        else
        {
            // Check for any missing built-in contexts
            var existingNames = existingContexts.Select(c => c.Name.ToLowerInvariant()).ToHashSet();
            var sortOrder = existingContexts.Max(c => c.SortOrder) + 1;

            foreach (var (name, icon, color) in DefaultContexts)
            {
                if (!existingNames.Contains(name.ToLowerInvariant()))
                {
                    var context = new Context
                    {
                        Name = name,
                        Icon = icon,
                        Color = color,
                        IsActive = true,
                        SortOrder = sortOrder++,
                        IsBuiltIn = true,
                        UsageCount = 0
                    };
                    await _repository.AddAsync(context);
                }
            }
        }

        _initialized = true;
    }

    public async Task SeedContextsForModeAsync(AppMode mode)
    {
        var contextDefinitions = GetContextDefinitionsForMode(mode);

        foreach (var (name, icon, color) in contextDefinitions)
        {
            try
            {
                var existing = await GetByNameAsync(name);
                if (existing == null)
                {
                    var allContexts = await _repository.GetAllAsync();
                    var maxSortOrder = allContexts.Any() ? allContexts.Max(c => c.SortOrder) : 0;

                    var context = new Context
                    {
                        Name = name.ToLowerInvariant().Trim(),
                        Icon = icon,
                        Color = color,
                        IsActive = true,
                        SortOrder = maxSortOrder + 1,
                        IsBuiltIn = true,
                        UsageCount = 0
                    };
                    await _repository.AddAsync(context);
                }
            }
            catch
            {
                // Context may already exist, that's fine
            }
        }
    }

    public async Task ResetContextsForModeAsync(AppMode mode)
    {
        // Get all existing contexts
        var allContexts = await _repository.GetAllAsync();

        // Mark user-created contexts as inactive (soft delete)
        foreach (var context in allContexts.Where(c => !c.IsBuiltIn))
        {
            context.IsActive = false;
            await _repository.UpdateAsync(context);
        }

        // Seed the mode-specific contexts
        await SeedContextsForModeAsync(mode);
    }

    public IReadOnlyList<(string Name, string Icon, string Color)> GetContextDefinitionsForMode(AppMode mode)
    {
        return mode switch
        {
            AppMode.Work => WorkModeContexts.ToList(),
            AppMode.Life => LifeModeContexts.ToList(),
            AppMode.Balanced => BalancedModeContexts.ToList(),
            _ => DefaultContexts.ToList()
        };
    }

    private static string GenerateColor(string name)
    {
        // Generate a consistent color based on the name hash
        var hash = name.GetHashCode();
        var hue = Math.Abs(hash % 360);
        // HSL to hex with fixed saturation and lightness for nice colors
        return HslToHex(hue, 65, 45);
    }

    private static string HslToHex(int h, int s, int l)
    {
        double hNorm = h / 360.0;
        double sNorm = s / 100.0;
        double lNorm = l / 100.0;

        double r, g, b;

        if (s == 0)
        {
            r = g = b = lNorm;
        }
        else
        {
            double q = lNorm < 0.5 ? lNorm * (1 + sNorm) : lNorm + sNorm - lNorm * sNorm;
            double p = 2 * lNorm - q;
            r = HueToRgb(p, q, hNorm + 1.0 / 3);
            g = HueToRgb(p, q, hNorm);
            b = HueToRgb(p, q, hNorm - 1.0 / 3);
        }

        int rInt = (int)Math.Round(r * 255);
        int gInt = (int)Math.Round(g * 255);
        int bInt = (int)Math.Round(b * 255);

        return $"#{rInt:X2}{gInt:X2}{bInt:X2}";
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
