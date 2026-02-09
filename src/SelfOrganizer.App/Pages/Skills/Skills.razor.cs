using Microsoft.AspNetCore.Components;
using SelfOrganizer.App.Services;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.App.Pages.Skills;

public partial class Skills : IDisposable
{
    private List<Skill> _allSkills = new();
    private List<Skill> _filteredSkills = new();
    private bool _isLoading = true;
    private string _typeFilter = "all";
    private SkillCategory? _categoryFilter = null;

    // Completed task counts per skill
    private Dictionary<Guid, int> _completedTaskCounts = new();

    // Counts for badges
    private int _allCount;
    private int _haveCount;
    private int _wantCount;

    // Modal state
    private bool _showDetailModal = false;
    private bool _showDeleteConfirm = false;

    // View state
    private Skill? _viewingSkill;
    private Skill? _deletingSkill;

    protected override async Task OnInitializedAsync()
    {
        DataChangeNotification.OnDataChanged += HandleDataChanged;
        await LoadSkills();
    }

    private async void HandleDataChanged()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                await LoadSkills();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed while handling data change
        }
        catch (Exception)
        {
            // Log error but don't crash - data will be stale until next refresh
        }
    }

    private async Task LoadSkills()
    {
        _isLoading = true;
        try
        {
            _allSkills = (await SkillService.GetActiveSkillsAsync()).ToList();

            // Load completed task counts for each skill
            _completedTaskCounts.Clear();
            foreach (var skill in _allSkills)
            {
                var count = await SkillService.GetCompletedTaskCountAsync(skill.Id);
                _completedTaskCounts[skill.Id] = count;
            }

            // Update counts
            _allCount = _allSkills.Count;
            _haveCount = _allSkills.Count(s => s.Type == SkillType.Have);
            _wantCount = _allSkills.Count(s => s.Type == SkillType.Want);

            ApplyFilter();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private int GetCompletedTaskCount(Guid skillId)
    {
        return _completedTaskCounts.TryGetValue(skillId, out var count) ? count : 0;
    }

    private void FilterByType(string type)
    {
        _typeFilter = type;
        ApplyFilter();
    }

    private void FilterByCategory(SkillCategory? category)
    {
        _categoryFilter = category;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allSkills.AsEnumerable();

        // Type filter
        filtered = _typeFilter switch
        {
            "have" => filtered.Where(s => s.Type == SkillType.Have),
            "want" => filtered.Where(s => s.Type == SkillType.Want),
            _ => filtered
        };

        // Category filter
        if (_categoryFilter.HasValue)
        {
            filtered = filtered.Where(s => s.Category == _categoryFilter.Value);
        }

        _filteredSkills = filtered.ToList();
    }

    private string GetTypeLabel() => _typeFilter switch
    {
        "have" => "skills you have",
        "want" => "skills you want",
        _ => ""
    };

    private string GetEmptyMessage() => _typeFilter switch
    {
        "have" => "Add skills you already possess to track your expertise.",
        "want" => "Add skills you want to develop to track your learning journey.",
        _ => "Track skills you have and want to develop."
    };

    private static string GetCategoryLabel(SkillCategory category) => category switch
    {
        SkillCategory.Technical => "Technical",
        SkillCategory.SoftSkills => "Soft Skills",
        SkillCategory.Creative => "Creative",
        SkillCategory.DomainKnowledge => "Domain",
        SkillCategory.ToolsSoftware => "Tools",
        _ => category.ToString()
    };

    // Navigation Methods
    private void NavigateToNewSkill()
    {
        NavigationManager.NavigateTo("skills/new");
    }

    private void NavigateToEditSkill(Guid skillId)
    {
        NavigationManager.NavigateTo($"skills/{skillId}/edit");
    }

    private void ShowDetailModal(Skill skill)
    {
        _viewingSkill = skill;
        _showDetailModal = true;
    }

    private void CloseDetailModal()
    {
        _showDetailModal = false;
        _viewingSkill = null;
    }

    private async Task OnSkillUpdatedFromDetail()
    {
        // Reload skills when updated from the detail view
        await LoadSkills();
        StateHasChanged();
    }

    // CRUD Operations
    private async Task LevelUpSkill(Skill skill)
    {
        if (skill.CurrentProficiency < skill.TargetProficiency)
        {
            await SkillService.UpdateProficiencyAsync(skill.Id, skill.CurrentProficiency + 1);
            DataChangeNotification.NotifyDataChanged();
            await LoadSkills();
        }
    }

    private void DeleteSkill(Skill skill)
    {
        _deletingSkill = skill;
        _showDeleteConfirm = true;
    }

    private async Task ConfirmDelete()
    {
        if (_deletingSkill != null)
        {
            await SkillService.DeleteAsync(_deletingSkill.Id);
            _deletingSkill = null;
        }
        _showDeleteConfirm = false;
        DataChangeNotification.NotifyDataChanged();
        await LoadSkills();
    }

    public void Dispose()
    {
        DataChangeNotification.OnDataChanged -= HandleDataChanged;
    }
}
