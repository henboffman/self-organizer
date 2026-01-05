using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Domain;

public class CalendarService : ICalendarService
{
    private readonly IRepository<CalendarEvent> _repository;

    public CalendarService(IRepository<CalendarEvent> repository)
    {
        _repository = repository;
    }

    public async Task<CalendarEvent?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

        return await _repository.QueryAsync(e =>
            e.StartTime >= startOfDay && e.StartTime <= endOfDay ||
            e.EndTime >= startOfDay && e.EndTime <= endOfDay ||
            (e.StartTime <= startOfDay && e.EndTime >= endOfDay));
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsForRangeAsync(DateTime start, DateTime end)
    {
        return await _repository.QueryAsync(e =>
            (e.StartTime >= start && e.StartTime <= end) ||
            (e.EndTime >= start && e.EndTime <= end) ||
            (e.StartTime <= start && e.EndTime >= end));
    }

    public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int count = 10)
    {
        var now = DateTime.UtcNow;
        var events = await _repository.QueryAsync(e => e.StartTime >= now);
        return events.OrderBy(e => e.StartTime).Take(count);
    }

    public async Task<CalendarEvent> CreateAsync(CalendarEvent calendarEvent)
    {
        return await _repository.AddAsync(calendarEvent);
    }

    public async Task<CalendarEvent> UpdateAsync(CalendarEvent calendarEvent)
    {
        return await _repository.UpdateAsync(calendarEvent);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<CalendarEvent> LinkTaskAsync(Guid eventId, Guid taskId)
    {
        var calendarEvent = await _repository.GetByIdAsync(eventId);
        if (calendarEvent == null)
            throw new InvalidOperationException($"Event {eventId} not found");

        if (!calendarEvent.LinkedTaskIds.Contains(taskId))
        {
            calendarEvent.LinkedTaskIds.Add(taskId);
            await _repository.UpdateAsync(calendarEvent);
        }

        return calendarEvent;
    }

    public async Task<CalendarEvent> UnlinkTaskAsync(Guid eventId, Guid taskId)
    {
        var calendarEvent = await _repository.GetByIdAsync(eventId);
        if (calendarEvent == null)
            throw new InvalidOperationException($"Event {eventId} not found");

        calendarEvent.LinkedTaskIds.Remove(taskId);
        return await _repository.UpdateAsync(calendarEvent);
    }
}
