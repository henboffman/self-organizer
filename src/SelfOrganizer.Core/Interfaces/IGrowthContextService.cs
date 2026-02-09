using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface IGrowthContextService
{
    Task<GrowthSnapshot> CaptureSnapshotAsync(SnapshotTrigger trigger = SnapshotTrigger.Manual, string? notes = null);
    Task<IEnumerable<GrowthSnapshot>> GetSnapshotsInRangeAsync(DateOnly start, DateOnly end);
    Task<GrowthSnapshot?> GetLatestSnapshotAsync();
    Task DeleteSnapshotAsync(Guid id);

    Task<GrowthContextSummary> GetContextForPeriodAsync(DateOnly periodStart, DateOnly periodEnd);
    Task<List<GrowthContextSummary>> GetJourneyAsync(DateOnly start, DateOnly end, bool monthly = true);

    Task AutoSnapshotIfDueAsync();

    Task<string> ExportCareerPlanHtmlAsync(Guid? careerPlanId = null);
    Task<string> ExportCareerPlanHtmlAsync(CareerExportData data);
}
