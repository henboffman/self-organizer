using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Core.Interfaces;

public interface ICalendarService
{
    Task<CalendarEvent?> GetByIdAsync(Guid id);
    Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(DateOnly date);
    Task<IEnumerable<CalendarEvent>> GetEventsForRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int count = 10);
    Task<CalendarEvent> CreateAsync(CalendarEvent calendarEvent);
    Task<CalendarEvent> UpdateAsync(CalendarEvent calendarEvent);
    Task DeleteAsync(Guid id);
    Task<CalendarEvent> LinkTaskAsync(Guid eventId, Guid taskId);
    Task<CalendarEvent> UnlinkTaskAsync(Guid eventId, Guid taskId);
}
