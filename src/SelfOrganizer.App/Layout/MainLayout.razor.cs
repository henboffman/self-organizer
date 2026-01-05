using Microsoft.AspNetCore.Components;
using SelfOrganizer.App.Services.Data;

namespace SelfOrganizer.App.Layout;

public partial class MainLayout
{
    [Inject]
    private IIndexedDbService DbService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await DbService.InitializeAsync();
    }
}
