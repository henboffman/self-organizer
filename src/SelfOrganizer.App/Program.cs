using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SelfOrganizer.App;
using SelfOrganizer.App.Services;
using SelfOrganizer.App.Services.Auth;
using SelfOrganizer.App.Services.Commands;
using SelfOrganizer.App.Services.Data;
using SelfOrganizer.App.Services.Domain;
using SelfOrganizer.App.Services.OutlookCalendar;
using SelfOrganizer.App.Services.Intelligence;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Authorization
builder.Services.AddAuthorizationCore();

// IndexedDB Service
builder.Services.AddSingleton<IIndexedDbService, IndexedDbService>();

// Network Status and Pending Operation Queue (for hybrid/offline support)
builder.Services.AddSingleton<INetworkStatusService, NetworkStatusService>();
builder.Services.AddSingleton<IPendingOperationQueue, PendingOperationQueue>();

// Repositories (HybridRepository for online/offline support)
builder.Services.AddScoped<IRepository<CaptureItem>>(sp =>
    new HybridRepository<CaptureItem>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Captures, StoreNames.Captures));
builder.Services.AddScoped<IRepository<TodoTask>>(sp =>
    new HybridRepository<TodoTask>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Tasks, StoreNames.Tasks));
builder.Services.AddScoped<IRepository<Project>>(sp =>
    new HybridRepository<Project>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Projects, StoreNames.Projects));
builder.Services.AddScoped<IRepository<CalendarEvent>>(sp =>
    new HybridRepository<CalendarEvent>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Events, StoreNames.Events));
builder.Services.AddScoped<IRepository<TimeBlock>>(sp =>
    new HybridRepository<TimeBlock>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.TimeBlocks, StoreNames.TimeBlocks));
builder.Services.AddScoped<IRepository<Contact>>(sp =>
    new HybridRepository<Contact>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Contacts, StoreNames.Contacts));
builder.Services.AddScoped<IRepository<ReferenceItem>>(sp =>
    new HybridRepository<ReferenceItem>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.References, StoreNames.References));
builder.Services.AddScoped<IRepository<Context>>(sp =>
    new HybridRepository<Context>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Contexts, StoreNames.Contexts));
builder.Services.AddScoped<IRepository<CategoryDefinition>>(sp =>
    new HybridRepository<CategoryDefinition>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Categories, StoreNames.Categories));
builder.Services.AddScoped<IRepository<UserPreferences>>(sp =>
    new HybridRepository<UserPreferences>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Preferences, StoreNames.Preferences));
builder.Services.AddScoped<IRepository<DailySnapshot>>(sp =>
    new HybridRepository<DailySnapshot>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.DailySnapshots, StoreNames.DailySnapshots));
builder.Services.AddScoped<IRepository<Goal>>(sp =>
    new HybridRepository<Goal>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Goals, StoreNames.Goals));
builder.Services.AddScoped<IRepository<Idea>>(sp =>
    new HybridRepository<Idea>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Ideas, StoreNames.Ideas));
builder.Services.AddScoped<IRepository<Habit>>(sp =>
    new HybridRepository<Habit>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Habits, StoreNames.Habits));
builder.Services.AddScoped<IRepository<HabitLog>>(sp =>
    new HybridRepository<HabitLog>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.HabitLogs, StoreNames.HabitLogs));
builder.Services.AddScoped<IRepository<WeeklySnapshot>>(sp =>
    new HybridRepository<WeeklySnapshot>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.WeeklySnapshots, StoreNames.WeeklySnapshots));
builder.Services.AddScoped<IRepository<EntityLinkRule>>(sp =>
    new HybridRepository<EntityLinkRule>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.EntityLinkRules, StoreNames.EntityLinkRules));
builder.Services.AddScoped<IRepository<FocusSessionLog>>(sp =>
    new HybridRepository<FocusSessionLog>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.FocusSessionLogs, StoreNames.FocusSessionLogs));
builder.Services.AddScoped<IRepository<TaskReminderSnooze>>(sp =>
    new HybridRepository<TaskReminderSnooze>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.TaskReminderSnoozes, StoreNames.TaskReminderSnoozes));
builder.Services.AddScoped<IRepository<Skill>>(sp =>
    new HybridRepository<Skill>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.Skills, StoreNames.Skills));
builder.Services.AddScoped<IRepository<CareerPlan>>(sp =>
    new HybridRepository<CareerPlan>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.CareerPlans, StoreNames.CareerPlans));
builder.Services.AddScoped<IRepository<GrowthSnapshot>>(sp =>
    new HybridRepository<GrowthSnapshot>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IIndexedDbService>(),
        sp.GetRequiredService<INetworkStatusService>(),
        sp.GetRequiredService<IPendingOperationQueue>(),
        EntityTypeNames.GrowthSnapshots, StoreNames.GrowthSnapshots));

// User Preferences Provider (must be registered before services that depend on it)
builder.Services.AddScoped<IUserPreferencesProvider, UserPreferencesProvider>();

// Domain Services
builder.Services.AddScoped<ICaptureService, CaptureService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IContextService, ContextService>();
builder.Services.AddScoped<IBalanceDimensionService, BalanceDimensionService>();
builder.Services.AddScoped<IMeetingInsightService, MeetingInsightService>();
builder.Services.AddScoped<IExternalCalendarService, ExternalCalendarService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IIdeaService, IdeaService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDataSyncService, DataSyncService>();
builder.Services.AddScoped<ISampleDataService, SampleDataService>();
builder.Services.AddScoped<IStaleTaskReminderService, StaleTaskReminderService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<ICareerPlanService, CareerPlanService>();
builder.Services.AddScoped<IGrowthContextService, GrowthContextService>();

// Intelligence Services
builder.Services.AddScoped<ICategoryMatcherService, CategoryMatcherService>();
builder.Services.AddScoped<IEntityExtractionService, EntityExtractionService>();
builder.Services.AddScoped<ITaskOptimizerService, TaskOptimizerService>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IGoalAiService, GoalAiService>();
builder.Services.AddScoped<IHabitAiService, HabitAiService>();
builder.Services.AddScoped<ISkillAiService, SkillAiService>();
builder.Services.AddScoped<ICareerAiService, CareerAiService>();
builder.Services.AddScoped<IBalanceAiService, BalanceAiService>();
builder.Services.AddScoped<IProactiveSuggestionsService, ProactiveSuggestionsService>();
builder.Services.AddScoped<IEntityLinkingService, EntityLinkingService>();
builder.Services.AddScoped<ICalendarIntelligenceService, CalendarIntelligenceService>();

// Icon Services
builder.Services.AddSingleton<IIconLibraryService, IconLibraryService>();
builder.Services.AddScoped<ITaskIconIntelligenceService, TaskIconIntelligenceService>();

// UI Services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IPlatformService, PlatformService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISettingsExportService, SettingsExportService>();
builder.Services.AddScoped<INaturalLanguageCommandService, NaturalLanguageCommandService>();

// Microsoft Entra (Azure AD) Authentication - Server-based
// In hosted mode, authentication is handled by the server via cookies
builder.Services.AddScoped<ServerAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ServerAuthenticationStateProvider>());
builder.Services.AddScoped<IEntraAuthService>(sp =>
    sp.GetRequiredService<ServerAuthenticationStateProvider>());

// Outlook Calendar Services
builder.Services.AddScoped<IOutlookCalendarSyncService, OutlookCalendarSyncService>();

// Database Sync Service
builder.Services.AddScoped<IDbSyncService, DbSyncService>();

// Notification Service (Singleton so all components share the same instance)
builder.Services.AddSingleton<IDataChangeNotificationService, DataChangeNotificationService>();

// Toast Notification Service (Singleton so toasts can be shown from anywhere)
builder.Services.AddSingleton<IToastService, ToastService>();

// Command History (Singleton so undo/redo state persists across page navigations)
builder.Services.AddSingleton<ICommandHistory, CommandHistory>();

// Focus Timer State (Singleton so timer state persists and syncs across components)
builder.Services.AddSingleton<IFocusTimerState, FocusTimerState>();

// Keyboard Navigation (Singleton so all components share the same event source)
builder.Services.AddSingleton<KeyboardNavigationService>();

await builder.Build().RunAsync();
