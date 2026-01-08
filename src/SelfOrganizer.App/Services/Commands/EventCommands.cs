using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Commands;

/// <summary>
/// Command to create a new calendar event.
/// </summary>
public class CreateEventCommand : ICommand
{
    private readonly IRepository<CalendarEvent> _repository;
    private readonly CalendarEvent _event;

    public string Description => $"Create event: {_event.Title}";
    public DateTime ExecutedAt { get; private set; }

    public CreateEventCommand(IRepository<CalendarEvent> repository, CalendarEvent calendarEvent)
    {
        _repository = repository;
        _event = calendarEvent;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;
        await _repository.AddAsync(_event);
    }

    public async Task UndoAsync()
    {
        await _repository.DeleteAsync(_event.Id);
    }
}

/// <summary>
/// Command to update an existing calendar event.
/// Stores the previous state for full restoration on undo.
/// </summary>
public class UpdateEventCommand : ICommand
{
    private readonly IRepository<CalendarEvent> _repository;
    private readonly CalendarEvent _newState;
    private CalendarEvent? _previousState;

    public string Description => $"Update event: {_newState.Title}";
    public DateTime ExecutedAt { get; private set; }

    public UpdateEventCommand(IRepository<CalendarEvent> repository, CalendarEvent newState)
    {
        _repository = repository;
        _newState = newState;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the previous state for undo
        var existingEvent = await _repository.GetByIdAsync(_newState.Id);
        if (existingEvent != null)
        {
            _previousState = CloneEvent(existingEvent);
        }

        await _repository.UpdateAsync(_newState);
    }

    public async Task UndoAsync()
    {
        if (_previousState == null)
            throw new InvalidOperationException("Cannot undo: no previous state captured.");

        await _repository.UpdateAsync(_previousState);
    }

    private static CalendarEvent CloneEvent(CalendarEvent calendarEvent)
    {
        return new CalendarEvent
        {
            Id = calendarEvent.Id,
            CreatedAt = calendarEvent.CreatedAt,
            ModifiedAt = calendarEvent.ModifiedAt,
            Title = calendarEvent.Title,
            Description = calendarEvent.Description,
            StartTime = calendarEvent.StartTime,
            EndTime = calendarEvent.EndTime,
            Location = calendarEvent.Location,
            IsAllDay = calendarEvent.IsAllDay,
            ExternalId = calendarEvent.ExternalId,
            Source = calendarEvent.Source,
            AutoCategory = calendarEvent.AutoCategory,
            OverrideCategory = calendarEvent.OverrideCategory,
            PrepTimeMinutes = calendarEvent.PrepTimeMinutes,
            DecompressTimeMinutes = calendarEvent.DecompressTimeMinutes,
            LinkedTaskIds = new List<Guid>(calendarEvent.LinkedTaskIds),
            Attendees = new List<string>(calendarEvent.Attendees),
            RequiresPrep = calendarEvent.RequiresPrep,
            RequiresFollowUp = calendarEvent.RequiresFollowUp,
            Tags = new List<string>(calendarEvent.Tags)
        };
    }
}

/// <summary>
/// Command to delete a calendar event.
/// Stores the deleted event for restoration on undo.
/// </summary>
public class DeleteEventCommand : ICommand
{
    private readonly IRepository<CalendarEvent> _repository;
    private readonly Guid _eventId;
    private CalendarEvent? _deletedEvent;

    public string Description => _deletedEvent != null
        ? $"Delete event: {_deletedEvent.Title}"
        : "Delete event";
    public DateTime ExecutedAt { get; private set; }

    public DeleteEventCommand(IRepository<CalendarEvent> repository, Guid eventId)
    {
        _repository = repository;
        _eventId = eventId;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the event before deletion for undo
        _deletedEvent = await _repository.GetByIdAsync(_eventId);
        await _repository.DeleteAsync(_eventId);
    }

    public async Task UndoAsync()
    {
        if (_deletedEvent == null)
            throw new InvalidOperationException("Cannot undo: no deleted event captured.");

        await _repository.AddAsync(_deletedEvent);
    }
}
