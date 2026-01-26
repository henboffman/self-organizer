using SelfOrganizer.Core.Interfaces;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Service providing predefined icons for projects, goals, and tasks
/// </summary>
public class IconLibraryService : IIconLibraryService
{
    private readonly List<IconDefinition> _allIcons;

    public IconLibraryService()
    {
        _allIcons = InitializeIcons();
    }

    private static List<IconDefinition> InitializeIcons()
    {
        var icons = new List<IconDefinition>();

        // Project icons (Open Iconic)
        icons.AddRange(new[]
        {
            new IconDefinition("oi-folder", "Folder", "Projects"),
            new IconDefinition("oi-briefcase", "Briefcase", "Projects"),
            new IconDefinition("oi-code", "Code", "Projects"),
            new IconDefinition("oi-home", "Home", "Projects"),
            new IconDefinition("oi-globe", "Globe", "Projects"),
            new IconDefinition("oi-people", "People", "Projects"),
            new IconDefinition("oi-wrench", "Wrench", "Projects"),
            new IconDefinition("oi-book", "Book", "Projects"),
            new IconDefinition("oi-laptop", "Laptop", "Projects"),
            new IconDefinition("oi-beaker", "Beaker", "Projects"),
            new IconDefinition("oi-puzzle-piece", "Puzzle", "Projects"),
            new IconDefinition("oi-lightbulb", "Lightbulb", "Projects"),
            new IconDefinition("oi-rocket", "Rocket", "Projects"),
            new IconDefinition("oi-heart", "Heart", "Projects"),
            new IconDefinition("oi-star", "Star", "Projects"),
            new IconDefinition("oi-flag", "Flag", "Projects"),
        });

        // Goal icons (Open Iconic)
        icons.AddRange(new[]
        {
            new IconDefinition("oi-target", "Target", "Goals"),
            new IconDefinition("oi-graph", "Graph", "Goals"),
            new IconDefinition("oi-bolt", "Bolt", "Goals"),
            new IconDefinition("oi-sun", "Sun", "Goals"),
            new IconDefinition("oi-moon", "Moon", "Goals"),
            new IconDefinition("oi-badge", "Badge", "Goals"),
            new IconDefinition("oi-fire", "Fire", "Goals"),
            new IconDefinition("oi-dashboard", "Dashboard", "Goals"),
            new IconDefinition("oi-pulse", "Pulse", "Goals"),
            new IconDefinition("oi-compass", "Compass", "Goals"),
            new IconDefinition("oi-map", "Map", "Goals"),
            new IconDefinition("oi-aperture", "Aperture", "Goals"),
        });

        // Communication icons
        icons.AddRange(new[]
        {
            new IconDefinition("oi-envelope-closed", "Email", "Communication"),
            new IconDefinition("oi-phone", "Phone", "Communication"),
            new IconDefinition("oi-comment-square", "Message", "Communication"),
            new IconDefinition("oi-video", "Video", "Communication"),
        });

        // Emoji icons
        icons.AddRange(new[]
        {
            // Work
            new IconDefinition("📧", "Email", "Emoji", true),
            new IconDefinition("📞", "Phone", "Emoji", true),
            new IconDefinition("👥", "Meeting", "Emoji", true),
            new IconDefinition("💬", "Message", "Emoji", true),
            new IconDefinition("💻", "Computer", "Emoji", true),
            new IconDefinition("✍️", "Writing", "Emoji", true),
            new IconDefinition("🎨", "Design", "Emoji", true),
            new IconDefinition("🔍", "Review", "Emoji", true),
            new IconDefinition("📚", "Research", "Emoji", true),
            new IconDefinition("📋", "Planning", "Emoji", true),
            new IconDefinition("📁", "Admin", "Emoji", true),

            // Personal
            new IconDefinition("🛒", "Shopping", "Emoji", true),
            new IconDefinition("🏥", "Health", "Emoji", true),
            new IconDefinition("🏃", "Exercise", "Emoji", true),
            new IconDefinition("💰", "Finance", "Emoji", true),
            new IconDefinition("🏠", "Home", "Emoji", true),
            new IconDefinition("✈️", "Travel", "Emoji", true),
            new IconDefinition("📖", "Learning", "Emoji", true),

            // General
            new IconDefinition("🎯", "Focus", "Emoji", true),
            new IconDefinition("⚡", "Quick", "Emoji", true),
            new IconDefinition("🏃‍♂️", "Errand", "Emoji", true),
            new IconDefinition("⏳", "Waiting", "Emoji", true),
            new IconDefinition("📌", "Task", "Emoji", true),
            new IconDefinition("✅", "Done", "Emoji", true),
            new IconDefinition("🔥", "Urgent", "Emoji", true),
            new IconDefinition("⭐", "Important", "Emoji", true),
            new IconDefinition("💡", "Idea", "Emoji", true),
            new IconDefinition("🚀", "Launch", "Emoji", true),
            new IconDefinition("🎉", "Celebrate", "Emoji", true),
            new IconDefinition("📅", "Calendar", "Emoji", true),
            new IconDefinition("🔧", "Fix", "Emoji", true),
            new IconDefinition("📦", "Package", "Emoji", true),
        });

        return icons;
    }

    public Dictionary<string, List<IconDefinition>> GetIconsByCategory()
    {
        return _allIcons
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public List<IconDefinition> GetEmojiIcons()
    {
        return _allIcons.Where(i => i.IsEmoji).ToList();
    }

    public List<IconDefinition> GetProjectIcons()
    {
        return _allIcons.Where(i => i.Category == "Projects" || i.IsEmoji).ToList();
    }

    public List<IconDefinition> GetGoalIcons()
    {
        return _allIcons.Where(i => i.Category == "Goals" || i.IsEmoji).ToList();
    }

    public IconDefinition? GetIconById(string id)
    {
        return _allIcons.FirstOrDefault(i => i.Id == id);
    }

    public List<IconDefinition> SearchIcons(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allIcons;

        var lowerQuery = query.ToLowerInvariant();
        return _allIcons
            .Where(i => i.Name.ToLowerInvariant().Contains(lowerQuery) ||
                       i.Category.ToLowerInvariant().Contains(lowerQuery))
            .ToList();
    }
}
