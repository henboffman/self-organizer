using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Core.Services;

namespace SelfOrganizer.App.Services.Domain;

public class CaptureService : ICaptureService
{
    private readonly IRepository<CaptureItem> _repository;

    public CaptureService(IRepository<CaptureItem> repository)
    {
        _repository = repository;
    }

    public async Task<CaptureItem> CaptureAsync(string rawText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawText);
        // Parse tags from the raw text
        var (cleanedText, tags) = TagParsingService.ParseTextAndTags(rawText);

        var item = new CaptureItem
        {
            RawText = rawText,
            CleanedText = cleanedText,
            ExtractedTags = tags,
            IsProcessed = false
        };
        return await _repository.AddAsync(item);
    }

    public async Task<IEnumerable<CaptureItem>> GetUnprocessedAsync()
    {
        return await _repository.QueryAsync(c => !c.IsProcessed);
    }

    public async Task<int> GetUnprocessedCountAsync()
    {
        return await _repository.CountAsync(c => !c.IsProcessed);
    }

    public async Task<int> GetTodayCaptureCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _repository.CountAsync(c => c.CreatedAt.Date == today);
    }

    public async Task<CaptureItem> MarkAsProcessedAsync(Guid captureId, Guid processedIntoId, ProcessedItemType type)
    {
        var item = await _repository.GetByIdAsync(captureId);
        if (item == null)
            throw new InvalidOperationException($"Capture item {captureId} not found");

        item.IsProcessed = true;
        item.ProcessedIntoId = processedIntoId;
        item.ProcessedIntoType = type;
        return await _repository.UpdateAsync(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
