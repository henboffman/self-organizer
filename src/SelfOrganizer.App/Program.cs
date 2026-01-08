using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SelfOrganizer.App;
using SelfOrganizer.App.Services;
using SelfOrganizer.App.Services.Commands;
using SelfOrganizer.App.Services.Data;
using SelfOrganizer.App.Services.Domain;
using SelfOrganizer.App.Services.Intelligence;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// IndexedDB Service
builder.Services.AddSingleton<IIndexedDbService, IndexedDbService>();

// Repositories
builder.Services.AddScoped<IRepository<CaptureItem>>(sp =>
    new IndexedDbRepository<CaptureItem>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Captures));
builder.Services.AddScoped<IRepository<TodoTask>>(sp =>
    new IndexedDbRepository<TodoTask>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Tasks));
builder.Services.AddScoped<IRepository<Project>>(sp =>
    new IndexedDbRepository<Project>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Projects));
builder.Services.AddScoped<IRepository<CalendarEvent>>(sp =>
    new IndexedDbRepository<CalendarEvent>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Events));
builder.Services.AddScoped<IRepository<TimeBlock>>(sp =>
    new IndexedDbRepository<TimeBlock>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.TimeBlocks));
builder.Services.AddScoped<IRepository<Contact>>(sp =>
    new IndexedDbRepository<Contact>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Contacts));
builder.Services.AddScoped<IRepository<ReferenceItem>>(sp =>
    new IndexedDbRepository<ReferenceItem>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.References));
builder.Services.AddScoped<IRepository<Context>>(sp =>
    new IndexedDbRepository<Context>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Contexts));
builder.Services.AddScoped<IRepository<CategoryDefinition>>(sp =>
    new IndexedDbRepository<CategoryDefinition>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Categories));
builder.Services.AddScoped<IRepository<UserPreferences>>(sp =>
    new IndexedDbRepository<UserPreferences>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Preferences));
builder.Services.AddScoped<IRepository<DailySnapshot>>(sp =>
    new IndexedDbRepository<DailySnapshot>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.DailySnapshots));
builder.Services.AddScoped<IRepository<Goal>>(sp =>
    new IndexedDbRepository<Goal>(sp.GetRequiredService<IIndexedDbService>(), StoreNames.Goals));

// Domain Services
builder.Services.AddScoped<ICaptureService, CaptureService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IContextService, ContextService>();
builder.Services.AddScoped<IMeetingInsightService, MeetingInsightService>();
builder.Services.AddScoped<IExternalCalendarService, ExternalCalendarService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Intelligence Services
builder.Services.AddScoped<ICategoryMatcherService, CategoryMatcherService>();
builder.Services.AddScoped<IEntityExtractionService, EntityExtractionService>();
builder.Services.AddScoped<ITaskOptimizerService, TaskOptimizerService>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IGoalAiService, GoalAiService>();

// UI Services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IPlatformService, PlatformService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Notification Service (Singleton so all components share the same instance)
builder.Services.AddSingleton<IDataChangeNotificationService, DataChangeNotificationService>();

// Command History (Singleton so undo/redo state persists across page navigations)
builder.Services.AddSingleton<ICommandHistory, CommandHistory>();

// Focus Timer State (Singleton so timer state persists and syncs across components)
builder.Services.AddSingleton<IFocusTimerState, FocusTimerState>();

await builder.Build().RunAsync();
