using SelfOrganizer.Core.Interfaces;

namespace SelfOrganizer.App.Services.Commands;

/// <summary>
/// Manages command history for undo/redo functionality.
/// Uses two stacks to track executed commands and undone commands.
/// </summary>
public class CommandHistory : ICommandHistory
{
    private const int MaxHistorySize = 50;

    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public event Action? OnHistoryChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? NextUndoDescription => _undoStack.TryPeek(out var command) ? command.Description : null;
    public string? NextRedoDescription => _redoStack.TryPeek(out var command) ? command.Description : null;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// Clears the redo stack since a new action invalidates the redo history.
    /// </summary>
    public async Task ExecuteAsync(ICommand command)
    {
        await command.ExecuteAsync();

        _undoStack.Push(command);
        _redoStack.Clear();

        // Enforce maximum history size
        TrimUndoStack();

        OnHistoryChanged?.Invoke();
    }

    /// <summary>
    /// Undoes the most recent command.
    /// If undo fails, the command is restored to the undo stack.
    /// </summary>
    public async Task<bool> UndoAsync()
    {
        if (!CanUndo)
            return false;

        var command = _undoStack.Pop();

        try
        {
            await command.UndoAsync();
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
            return true;
        }
        catch
        {
            // If undo fails, restore the command to the undo stack
            _undoStack.Push(command);
            OnHistoryChanged?.Invoke();
            throw;
        }
    }

    /// <summary>
    /// Redoes the most recently undone command.
    /// If redo fails, the command is restored to the redo stack.
    /// </summary>
    public async Task<bool> RedoAsync()
    {
        if (!CanRedo)
            return false;

        var command = _redoStack.Pop();

        try
        {
            await command.ExecuteAsync();
            _undoStack.Push(command);
            OnHistoryChanged?.Invoke();
            return true;
        }
        catch
        {
            // If redo fails, restore the command to the redo stack
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
            throw;
        }
    }

    /// <summary>
    /// Clears all command history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        OnHistoryChanged?.Invoke();
    }

    /// <summary>
    /// Trims the undo stack to enforce the maximum history size.
    /// Removes oldest commands when limit is exceeded.
    /// </summary>
    private void TrimUndoStack()
    {
        if (_undoStack.Count <= MaxHistorySize)
            return;

        // Convert to list, trim, and rebuild stack
        var commands = _undoStack.ToList();
        commands.Reverse(); // Stack.ToList() gives newest first, we want oldest first

        // Keep only the most recent MaxHistorySize commands
        var trimmedCommands = commands.Skip(commands.Count - MaxHistorySize).ToList();

        _undoStack.Clear();
        foreach (var command in trimmedCommands)
        {
            _undoStack.Push(command);
        }
    }
}
