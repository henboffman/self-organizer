namespace SelfOrganizer.Core.Models;

/// <summary>
/// Comprehensive accessibility settings for vision, reading, motion, and interaction accommodations
/// </summary>
public class AccessibilitySettings
{
    // Vision Settings
    /// <summary>Color blindness accommodation mode</summary>
    public ColorBlindnessMode ColorBlindnessMode { get; set; } = ColorBlindnessMode.None;

    /// <summary>Enable high contrast mode for better visibility</summary>
    public bool HighContrastMode { get; set; } = false;

    /// <summary>Minimum contrast ratio (WCAG AA default is 4.5)</summary>
    public double MinContrastRatio { get; set; } = 4.5;

    // Reading Settings
    /// <summary>Use dyslexia-friendly fonts (OpenDyslexic or Atkinson Hyperlegible)</summary>
    public bool DyslexiaFriendlyFont { get; set; } = false;

    /// <summary>Text scaling percentage (100-200%)</summary>
    public int TextScalingPercent { get; set; } = 100;

    /// <summary>Line height multiplier for improved readability (1.0-2.0)</summary>
    public double LineHeightMultiplier { get; set; } = 1.5;

    /// <summary>Letter spacing in em units (0-0.2)</summary>
    public double LetterSpacing { get; set; } = 0;

    // Motion Settings
    /// <summary>Reduced motion preference mode</summary>
    public ReducedMotionMode ReducedMotionMode { get; set; } = ReducedMotionMode.RespectSystem;

    // Interaction Settings
    /// <summary>Focus indicator size in pixels (2-4)</summary>
    public int FocusIndicatorSize { get; set; } = 2;

    /// <summary>Enable larger click targets for easier interaction</summary>
    public bool LargerClickTargets { get; set; } = false;

    /// <summary>Tooltip display delay in milliseconds</summary>
    public int TooltipDelayMs { get; set; } = 500;

    /// <summary>Always show underlines on links for visibility</summary>
    public bool AlwaysShowLinkUnderlines { get; set; } = true;
}

/// <summary>
/// Color blindness accommodation modes
/// </summary>
public enum ColorBlindnessMode
{
    /// <summary>No color blindness accommodation</summary>
    None,

    /// <summary>Red-blind (red-green color blindness)</summary>
    Protanopia,

    /// <summary>Green-blind (red-green color blindness)</summary>
    Deuteranopia,

    /// <summary>Blue-blind (blue-yellow color blindness)</summary>
    Tritanopia,

    /// <summary>Complete color blindness (monochromacy)</summary>
    Achromatopsia
}

/// <summary>
/// Reduced motion preference modes
/// </summary>
public enum ReducedMotionMode
{
    /// <summary>Respect the user's system preference for reduced motion</summary>
    RespectSystem,

    /// <summary>Always reduce motion regardless of system setting</summary>
    AlwaysReduce,

    /// <summary>Never reduce motion regardless of system setting</summary>
    NeverReduce
}
