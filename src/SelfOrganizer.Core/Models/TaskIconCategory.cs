namespace SelfOrganizer.Core.Models;

/// <summary>
/// Categories for intelligent task icon detection based on task content
/// </summary>
public enum TaskIconCategory
{
    // Communication
    Email,
    Call,
    Meeting,
    Message,

    // Work Types
    Code,
    Write,
    Design,
    Review,
    Research,
    Planning,
    Admin,

    // Personal
    Shopping,
    Health,
    Exercise,
    Finance,
    Home,
    Travel,
    Learning,

    // General
    DeepWork,
    QuickTask,
    Errand,
    WaitingFor,
    Unknown
}
