using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Services.Commands;

/// <summary>
/// Command to create a new project.
/// </summary>
public class CreateProjectCommand : ICommand
{
    private readonly IRepository<Project> _repository;
    private readonly Project _project;

    public string Description => $"Create project: {_project.Name}";
    public DateTime ExecutedAt { get; private set; }

    public CreateProjectCommand(IRepository<Project> repository, Project project)
    {
        _repository = repository;
        _project = project;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;
        await _repository.AddAsync(_project);
    }

    public async Task UndoAsync()
    {
        await _repository.DeleteAsync(_project.Id);
    }
}

/// <summary>
/// Command to update an existing project.
/// Stores the previous state for full restoration on undo.
/// </summary>
public class UpdateProjectCommand : ICommand
{
    private readonly IRepository<Project> _repository;
    private readonly Project _newState;
    private Project? _previousState;

    public string Description => $"Update project: {_newState.Name}";
    public DateTime ExecutedAt { get; private set; }

    public UpdateProjectCommand(IRepository<Project> repository, Project newState)
    {
        _repository = repository;
        _newState = newState;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the previous state for undo
        var existingProject = await _repository.GetByIdAsync(_newState.Id);
        if (existingProject != null)
        {
            _previousState = CloneProject(existingProject);
        }

        await _repository.UpdateAsync(_newState);
    }

    public async Task UndoAsync()
    {
        if (_previousState == null)
            throw new InvalidOperationException("Cannot undo: no previous state captured.");

        await _repository.UpdateAsync(_previousState);
    }

    private static Project CloneProject(Project project)
    {
        return new Project
        {
            Id = project.Id,
            CreatedAt = project.CreatedAt,
            ModifiedAt = project.ModifiedAt,
            Name = project.Name,
            Description = project.Description,
            DesiredOutcome = project.DesiredOutcome,
            Status = project.Status,
            Category = project.Category,
            DueDate = project.DueDate,
            CompletedAt = project.CompletedAt,
            Priority = project.Priority,
            Tags = new List<string>(project.Tags),
            Notes = project.Notes,
            Url = project.Url
        };
    }
}

/// <summary>
/// Command to delete a project.
/// Stores the deleted project for restoration on undo.
/// </summary>
public class DeleteProjectCommand : ICommand
{
    private readonly IRepository<Project> _repository;
    private readonly Guid _projectId;
    private Project? _deletedProject;

    public string Description => _deletedProject != null
        ? $"Delete project: {_deletedProject.Name}"
        : "Delete project";
    public DateTime ExecutedAt { get; private set; }

    public DeleteProjectCommand(IRepository<Project> repository, Guid projectId)
    {
        _repository = repository;
        _projectId = projectId;
    }

    public async Task ExecuteAsync()
    {
        ExecutedAt = DateTime.UtcNow;

        // Store the project before deletion for undo
        _deletedProject = await _repository.GetByIdAsync(_projectId);
        await _repository.DeleteAsync(_projectId);
    }

    public async Task UndoAsync()
    {
        if (_deletedProject == null)
            throw new InvalidOperationException("Cannot undo: no deleted project captured.");

        await _repository.AddAsync(_deletedProject);
    }
}
