namespace SelfOrganizer.Core.Interfaces;

/// <summary>
/// Service for managing sample/demo data created during onboarding
/// </summary>
public interface ISampleDataService
{
    /// <summary>
    /// Seeds sample data if not already seeded. Called after onboarding completes.
    /// Creates a sample goal, project, and tasks to help users explore the app.
    /// </summary>
    Task SeedSampleDataAsync();

    /// <summary>
    /// Permanently deletes all sample data from the system.
    /// </summary>
    Task DeleteAllSampleDataAsync();

    /// <summary>
    /// Checks if any sample data exists in the system.
    /// </summary>
    Task<bool> HasSampleDataAsync();
}
