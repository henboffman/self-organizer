using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services;

/// <summary>
/// Service for intelligently detecting task icons based on content analysis
/// </summary>
public class TaskIconIntelligenceService : ITaskIconIntelligenceService
{
    private readonly Dictionary<TaskIconCategory, string> _categoryIcons;
    private readonly Dictionary<TaskIconCategory, (string[] Keywords, double Weight)> _categoryKeywords;

    public TaskIconIntelligenceService()
    {
        _categoryIcons = InitializeCategoryIcons();
        _categoryKeywords = InitializeCategoryKeywords();
    }

    private static Dictionary<TaskIconCategory, string> InitializeCategoryIcons()
    {
        return new Dictionary<TaskIconCategory, string>
        {
            // Communication
            [TaskIconCategory.Email] = "📧",
            [TaskIconCategory.Call] = "📞",
            [TaskIconCategory.Meeting] = "👥",
            [TaskIconCategory.Message] = "💬",

            // Work Types
            [TaskIconCategory.Code] = "💻",
            [TaskIconCategory.Write] = "✍️",
            [TaskIconCategory.Design] = "🎨",
            [TaskIconCategory.Review] = "🔍",
            [TaskIconCategory.Research] = "📚",
            [TaskIconCategory.Planning] = "📋",
            [TaskIconCategory.Admin] = "📁",

            // Personal
            [TaskIconCategory.Shopping] = "🛒",
            [TaskIconCategory.Health] = "🏥",
            [TaskIconCategory.Exercise] = "🏃",
            [TaskIconCategory.Finance] = "💰",
            [TaskIconCategory.Home] = "🏠",
            [TaskIconCategory.Travel] = "✈️",
            [TaskIconCategory.Learning] = "📖",

            // General
            [TaskIconCategory.DeepWork] = "🎯",
            [TaskIconCategory.QuickTask] = "⚡",
            [TaskIconCategory.Errand] = "🏃‍♂️",
            [TaskIconCategory.WaitingFor] = "⏳",
            [TaskIconCategory.Unknown] = "📌",
        };
    }

    private static Dictionary<TaskIconCategory, (string[] Keywords, double Weight)> InitializeCategoryKeywords()
    {
        return new Dictionary<TaskIconCategory, (string[], double)>
        {
            // Communication - high confidence keywords
            [TaskIconCategory.Email] = (new[] { "email", "e-mail", "inbox", "reply", "send", "forward", "newsletter", "unsubscribe", "mail" }, 1.0),
            [TaskIconCategory.Call] = (new[] { "call", "phone", "ring", "dial", "voicemail", "callback" }, 1.0),
            [TaskIconCategory.Meeting] = (new[] { "meeting", "meet", "sync", "1:1", "one-on-one", "standup", "stand-up", "retro", "retrospective", "interview", "presentation", "demo", "workshop", "conference" }, 1.0),
            [TaskIconCategory.Message] = (new[] { "message", "text", "slack", "teams", "chat", "dm", "respond", "reply to" }, 0.9),

            // Work Types
            [TaskIconCategory.Code] = (new[] { "code", "coding", "debug", "deploy", "pr", "pull request", "merge", "commit", "refactor", "fix bug", "implement", "api", "function", "test", "unit test", "integration" }, 1.0),
            [TaskIconCategory.Write] = (new[] { "write", "draft", "document", "documentation", "blog", "article", "copy", "content", "readme", "spec", "specification", "proposal" }, 0.9),
            [TaskIconCategory.Design] = (new[] { "design", "mockup", "wireframe", "prototype", "ui", "ux", "figma", "sketch", "layout", "visual", "graphic" }, 1.0),
            [TaskIconCategory.Review] = (new[] { "review", "feedback", "approve", "check", "audit", "assess", "evaluate", "inspect", "proofread", "edit" }, 0.8),
            [TaskIconCategory.Research] = (new[] { "research", "investigate", "explore", "analyze", "study", "learn about", "read about", "find out", "discover", "compare" }, 0.9),
            [TaskIconCategory.Planning] = (new[] { "plan", "planning", "strategy", "roadmap", "schedule", "organize", "prioritize", "agenda", "outline", "timeline" }, 0.8),
            [TaskIconCategory.Admin] = (new[] { "admin", "administration", "paperwork", "file", "form", "submit", "expense", "invoice", "report", "update spreadsheet", "timesheet" }, 0.8),

            // Personal
            [TaskIconCategory.Shopping] = (new[] { "buy", "purchase", "order", "shop", "grocery", "groceries", "amazon", "store", "return", "pickup" }, 1.0),
            [TaskIconCategory.Health] = (new[] { "doctor", "dentist", "appointment", "prescription", "medicine", "medication", "health", "checkup", "therapy", "therapist", "medical" }, 1.0),
            [TaskIconCategory.Exercise] = (new[] { "exercise", "workout", "gym", "run", "running", "yoga", "walk", "hike", "swim", "bike", "cycling", "stretch", "fitness" }, 1.0),
            [TaskIconCategory.Finance] = (new[] { "pay", "payment", "bill", "budget", "bank", "transfer", "tax", "taxes", "invest", "investment", "savings", "401k", "insurance" }, 0.9),
            [TaskIconCategory.Home] = (new[] { "home", "house", "clean", "cleaning", "laundry", "dishes", "vacuum", "mow", "repair", "fix", "maintenance", "organize closet", "declutter" }, 0.8),
            [TaskIconCategory.Travel] = (new[] { "travel", "trip", "flight", "hotel", "book", "reservation", "pack", "packing", "itinerary", "vacation", "passport", "visa" }, 1.0),
            [TaskIconCategory.Learning] = (new[] { "learn", "course", "class", "tutorial", "training", "study", "practice", "read book", "watch course", "certification", "exam" }, 0.9),

            // General
            [TaskIconCategory.DeepWork] = (new[] { "deep work", "focus", "concentrate", "uninterrupted", "block time", "flow" }, 0.7),
            [TaskIconCategory.QuickTask] = (new[] { "quick", "fast", "5 min", "10 min", "simple", "easy", "small" }, 0.6),
            [TaskIconCategory.Errand] = (new[] { "errand", "pickup", "drop off", "deliver", "post office", "dmv", "dry cleaning" }, 0.9),
        };
    }

    public TaskIconAnalysisResult AnalyzeTask(string title, string? description, List<string>? contexts)
    {
        var text = $"{title} {description ?? ""}".ToLowerInvariant();
        var matchedKeywords = new List<string>();
        var scores = new Dictionary<TaskIconCategory, double>();

        // Check contexts for special handling
        if (contexts != null)
        {
            if (contexts.Any(c => c.Contains("@waiting", StringComparison.OrdinalIgnoreCase) ||
                                  c.Contains("waiting", StringComparison.OrdinalIgnoreCase)))
            {
                return new TaskIconAnalysisResult(
                    TaskIconCategory.WaitingFor,
                    _categoryIcons[TaskIconCategory.WaitingFor],
                    1.0,
                    new[] { "@waiting" }
                );
            }

            if (contexts.Any(c => c.Contains("@home", StringComparison.OrdinalIgnoreCase)))
            {
                scores[TaskIconCategory.Home] = 0.3; // Slight boost for home context
            }

            if (contexts.Any(c => c.Contains("@work", StringComparison.OrdinalIgnoreCase) ||
                                  c.Contains("@office", StringComparison.OrdinalIgnoreCase)))
            {
                scores[TaskIconCategory.Code] = 0.2;
                scores[TaskIconCategory.Meeting] = 0.2;
            }
        }

        // Score each category based on keyword matches
        foreach (var (category, (keywords, weight)) in _categoryKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    if (!scores.ContainsKey(category))
                        scores[category] = 0;

                    scores[category] += weight;
                    matchedKeywords.Add(keyword);
                }
            }
        }

        // Find best match
        if (scores.Count > 0)
        {
            var bestMatch = scores.OrderByDescending(s => s.Value).First();
            var confidence = Math.Min(bestMatch.Value, 1.0);

            return new TaskIconAnalysisResult(
                bestMatch.Key,
                _categoryIcons[bestMatch.Key],
                confidence,
                matchedKeywords.Distinct().ToArray()
            );
        }

        // Default to unknown
        return new TaskIconAnalysisResult(
            TaskIconCategory.Unknown,
            _categoryIcons[TaskIconCategory.Unknown],
            0.0,
            Array.Empty<string>()
        );
    }

    public string GetIconForCategory(TaskIconCategory category)
    {
        return _categoryIcons.TryGetValue(category, out var icon) ? icon : _categoryIcons[TaskIconCategory.Unknown];
    }

    public Dictionary<TaskIconCategory, string> GetCategoryIconMappings()
    {
        return new Dictionary<TaskIconCategory, string>(_categoryIcons);
    }
}
