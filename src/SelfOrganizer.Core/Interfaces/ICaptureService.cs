using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ICaptureService
{
    Task<CaptureItem> CaptureAsync(string rawText);
    Task<IEnumerable<CaptureItem>> GetUnprocessedAsync();
    Task<int> GetUnprocessedCountAsync();
    Task<int> GetTodayCaptureCountAsync();
    Task<CaptureItem> MarkAsProcessedAsync(Guid captureId, Guid processedIntoId, ProcessedItemType type);
    Task DeleteAsync(Guid id);
}
