using System.ComponentModel.DataAnnotations;

namespace SelfOrganizer.Core.Models;

/// <summary>
/// Project - collection of tasks toward an outcome
/// </summary>
public class Project : BaseEntity
{
    [Required(ErrorMessage = "Project name is required")]
    [MinLength(1, ErrorMessage = "Project name cannot be empty")]
    [MaxLength(200, ErrorMessage = "Project name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DesiredOutcome { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string? Category { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    [Range(1, 3, ErrorMessage = "Priority must be between 1 (High) and 3 (Low)")]
    public int Priority { get; set; } = 2;
    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }
    public string? Url { get; set; }

    /// <summary>
    /// Hex color code for visual identification (e.g., "#3b82f6")
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier (Open Iconic class name like "oi-folder", or emoji)
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Base64 data URL for custom uploaded image (64x64)
    /// </summary>
    public string? IconImageUrl { get; set; }
}
