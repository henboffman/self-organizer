namespace SelfOrganizer.Core.Interfaces;

public interface ICommand
{
    string Description { get; }
    DateTime ExecutedAt { get; }
    Task ExecuteAsync();
    Task UndoAsync();
}
