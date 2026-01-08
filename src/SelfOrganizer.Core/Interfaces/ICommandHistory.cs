namespace SelfOrganizer.Core.Interfaces;

public interface ICommandHistory
{
    event Action? OnHistoryChanged;
    Task ExecuteAsync(ICommand command);
    Task<bool> UndoAsync();
    Task<bool> RedoAsync();
    bool CanUndo { get; }
    bool CanRedo { get; }
    string? NextUndoDescription { get; }
    string? NextRedoDescription { get; }
    int UndoCount { get; }
    int RedoCount { get; }
    void Clear();
}
