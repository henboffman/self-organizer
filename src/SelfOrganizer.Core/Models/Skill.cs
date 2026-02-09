using System.ComponentModel.DataAnnotations;

namespace SelfOrganizer.Core.Models;

/// <summary>
/// Represents a skill that a user has or wants to develop
/// </summary>
public class Skill : BaseEntity
{
    [Required(ErrorMessage = "Skill name is required")]
    [MinLength(1, ErrorMessage = "Skill name cannot be empty")]
    [MaxLength(200, ErrorMessage = "Skill name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the skill
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon identifier (Open Iconic class name like "oi-code" or emoji)
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Hex color for visual display (e.g., "#3b82f6")
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Category for grouping skills
    /// </summary>
    public SkillCategory Category { get; set; } = SkillCategory.Technical;

    /// <summary>
    /// Whether this is a skill the user HAS or WANTS to develop
    /// </summary>
    public SkillType Type { get; set; } = SkillType.Want;

    /// <summary>
    /// User's self-assessed current proficiency level (1-5 scale)
    /// 1=Novice, 2=Beginner, 3=Intermediate, 4=Advanced, 5=Expert
    /// </summary>
    [Range(1, 5, ErrorMessage = "Proficiency must be between 1 and 5")]
    public int CurrentProficiency { get; set; } = 1;

    /// <summary>
    /// User's target proficiency level (1-5 scale)
    /// </summary>
    [Range(1, 5, ErrorMessage = "Target proficiency must be between 1 and 5")]
    public int TargetProficiency { get; set; } = 5;

    /// <summary>
    /// Order for display in lists
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether the skill is actively being developed/tracked
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user started developing this skill
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Target date to reach target proficiency
    /// </summary>
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Goals that contribute to developing this skill (bidirectional with Goal.LinkedSkillIds)
    /// </summary>
    public List<Guid> LinkedGoalIds { get; set; } = new();

    /// <summary>
    /// Projects that use or develop this skill (bidirectional with Project.LinkedSkillIds)
    /// </summary>
    public List<Guid> LinkedProjectIds { get; set; } = new();

    /// <summary>
    /// Habits that help develop this skill (bidirectional with Habit.LinkedSkillIds)
    /// </summary>
    public List<Guid> LinkedHabitIds { get; set; } = new();

    /// <summary>
    /// Tasks that exercised or developed this skill (bidirectional with TodoTask.SkillIds)
    /// Used to infer skill usage from completed work
    /// </summary>
    public List<Guid> LinkedTaskIds { get; set; } = new();

    /// <summary>
    /// If AI-generated, the rationale for why this skill was suggested
    /// </summary>
    public string? AiRationale { get; set; }

    /// <summary>
    /// Whether this skill was AI-suggested (vs manually created)
    /// </summary>
    public bool IsAiSuggested { get; set; } = false;

    /// <summary>
    /// User's notes about this skill, learning resources, etc.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Tags for additional categorization and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Calculate progress percentage toward target proficiency
    /// </summary>
    public int ProgressPercent => TargetProficiency > CurrentProficiency
        ? (int)Math.Round((double)(CurrentProficiency - 1) / (TargetProficiency - 1) * 100)
        : 100;

    /// <summary>
    /// Get the proficiency level name
    /// </summary>
    public static string GetProficiencyName(int level) => level switch
    {
        1 => "Novice",
        2 => "Beginner",
        3 => "Intermediate",
        4 => "Advanced",
        5 => "Expert",
        _ => "Unknown"
    };
}
