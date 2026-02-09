using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfOrganizer.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Captures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedIntoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedIntoType = table.Column<int>(type: "int", nullable: true),
                    ExtractedTags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CleanedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Captures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchTerms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultPrepMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultDecompressMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultEnergyRequired = table.Column<int>(type: "int", nullable: false),
                    TypicallyRequiresFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TasksCompleted = table.Column<int>(type: "int", nullable: false),
                    TasksCreated = table.Column<int>(type: "int", nullable: false),
                    CapturesProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalMinutesWorked = table.Column<int>(type: "int", nullable: false),
                    MeetingMinutes = table.Column<int>(type: "int", nullable: false),
                    DeepWorkMinutes = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewCompleted = table.Column<bool>(type: "bit", nullable: false),
                    MorningCheckinCompleted = table.Column<bool>(type: "bit", nullable: false),
                    MorningCheckinTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MorningEnergy = table.Column<int>(type: "int", nullable: true),
                    MorningMood = table.Column<int>(type: "int", nullable: true),
                    MorningIntention = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopPriorityTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MorningGratitude = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EveningCheckoutCompleted = table.Column<bool>(type: "bit", nullable: false),
                    EveningCheckoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EveningAccomplishment = table.Column<int>(type: "int", nullable: true),
                    DayReflection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityLinkRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCaseSensitive = table.Column<bool>(type: "bit", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetEntityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ApplyToTitle = table.Column<bool>(type: "bit", nullable: false),
                    ApplyToDescription = table.Column<bool>(type: "bit", nullable: false),
                    ApplyToAttendees = table.Column<bool>(type: "bit", nullable: false),
                    ApplyToTags = table.Column<bool>(type: "bit", nullable: false),
                    MinConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityLinkRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoCategory = table.Column<int>(type: "int", nullable: true),
                    OverrideCategory = table.Column<int>(type: "int", nullable: true),
                    PrepTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    DecompressTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    LinkedTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Attendees = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresPrep = table.Column<bool>(type: "bit", nullable: false),
                    RequiresFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    LinkedProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedGoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedIdeaIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutoLinked = table.Column<bool>(type: "bit", nullable: false),
                    LastLinkAnalysisAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FocusSessionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Intention = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FocusRating = table.Column<int>(type: "int", nullable: false),
                    WasDistracted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskCompleted = table.Column<bool>(type: "bit", nullable: false),
                    EnergyLevel = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualElapsedSeconds = table.Column<int>(type: "int", nullable: false),
                    OriginalEstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    ExtensionCount = table.Column<int>(type: "int", nullable: false),
                    TotalExtensionMinutes = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusSessionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesiredOutcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuccessCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Obstacles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resources = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Timeframe = table.Column<int>(type: "int", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    LinkedProjectIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedHabitIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiGeneratedPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BalanceDimensionIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryBalanceDimensionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BalanceImpact = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HabitLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HabitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Habits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    TargetCount = table.Column<int>(type: "int", nullable: false),
                    TrackedDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedGoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiRationale = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ideas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConvertedToTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    HasActionPotential = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnboardingCompleted = table.Column<bool>(type: "bit", nullable: false),
                    PreferredCalendarProvider = table.Column<int>(type: "int", nullable: true),
                    ShowSampleData = table.Column<bool>(type: "bit", nullable: false),
                    SampleDataSeeded = table.Column<bool>(type: "bit", nullable: false),
                    WorkDayStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    WorkDayEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    WorkDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultTaskDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MinimumUsableBlockMinutes = table.Column<int>(type: "int", nullable: false),
                    DeepWorkMinimumMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxConsecutiveMeetingMinutes = table.Column<int>(type: "int", nullable: false),
                    BufferBetweenMeetingsMinutes = table.Column<int>(type: "int", nullable: false),
                    MorningEnergyPeak = table.Column<int>(type: "int", nullable: false),
                    AfternoonEnergyPeak = table.Column<int>(type: "int", nullable: false),
                    AutoScheduleEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DailyReviewReminderHour = table.Column<int>(type: "int", nullable: false),
                    WeeklyReviewDay = table.Column<int>(type: "int", nullable: false),
                    EnableCelebrationEffects = table.Column<bool>(type: "bit", nullable: false),
                    EnableFocusTimer = table.Column<bool>(type: "bit", nullable: false),
                    FocusTimerMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableTimeBlindnessHelpers = table.Column<bool>(type: "bit", nullable: false),
                    EnableTaskChunking = table.Column<bool>(type: "bit", nullable: false),
                    MaxTaskChunkMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableHyperfocusAlerts = table.Column<bool>(type: "bit", nullable: false),
                    HyperfocusAlertMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableContextSwitchWarnings = table.Column<bool>(type: "bit", nullable: false),
                    EnableMinimalMode = table.Column<bool>(type: "bit", nullable: false),
                    EnablePickForMe = table.Column<bool>(type: "bit", nullable: false),
                    EnableBodyDoubling = table.Column<bool>(type: "bit", nullable: false),
                    EnableProgressVisualization = table.Column<bool>(type: "bit", nullable: false),
                    EnableMicroRewards = table.Column<bool>(type: "bit", nullable: false),
                    TaskStartGracePeriod = table.Column<int>(type: "int", nullable: false),
                    EnableGentleReminders = table.Column<bool>(type: "bit", nullable: false),
                    ShowEstimatedCompletion = table.Column<bool>(type: "bit", nullable: false),
                    EnableFocusSounds = table.Column<bool>(type: "bit", nullable: false),
                    PreferredFocusSound = table.Column<int>(type: "int", nullable: false),
                    BreakReminderMinutes = table.Column<int>(type: "int", nullable: false),
                    AutoStartBreakTimer = table.Column<bool>(type: "bit", nullable: false),
                    EnableTaskTransitions = table.Column<bool>(type: "bit", nullable: false),
                    TransitionBufferMinutes = table.Column<int>(type: "int", nullable: false),
                    ShowSessionIntentionPrompt = table.Column<bool>(type: "bit", nullable: false),
                    ShowPostSessionReflection = table.Column<bool>(type: "bit", nullable: false),
                    TrackDistractions = table.Column<bool>(type: "bit", nullable: false),
                    EnableFocusStreaks = table.Column<bool>(type: "bit", nullable: false),
                    PlayTimerCompletionSound = table.Column<bool>(type: "bit", nullable: false),
                    ShowTimerNotification = table.Column<bool>(type: "bit", nullable: false),
                    PauseTimerOnWindowBlur = table.Column<bool>(type: "bit", nullable: false),
                    LongBreakMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionsBeforeLongBreak = table.Column<int>(type: "int", nullable: false),
                    MaxVisibleTasks = table.Column<int>(type: "int", nullable: false),
                    OneTaskAtATimeMode = table.Column<bool>(type: "bit", nullable: false),
                    HideTaskCounts = table.Column<bool>(type: "bit", nullable: false),
                    HideDueDatesOnCards = table.Column<bool>(type: "bit", nullable: false),
                    SimplifiedTaskView = table.Column<bool>(type: "bit", nullable: false),
                    ShowEncouragingMessages = table.Column<bool>(type: "bit", nullable: false),
                    ShowElapsedTime = table.Column<bool>(type: "bit", nullable: false),
                    ShowTimeRemainingInBlock = table.Column<bool>(type: "bit", nullable: false),
                    TimeAwarenessIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableAmbientTimeDisplay = table.Column<bool>(type: "bit", nullable: false),
                    ShowTimeSinceLastBreak = table.Column<bool>(type: "bit", nullable: false),
                    AutoDarkMode = table.Column<bool>(type: "bit", nullable: false),
                    DarkModeStartHour = table.Column<int>(type: "int", nullable: false),
                    DarkModEndHour = table.Column<int>(type: "int", nullable: false),
                    SoundVolume = table.Column<int>(type: "int", nullable: false),
                    MuteAllSounds = table.Column<bool>(type: "bit", nullable: false),
                    EnableDailyGoalSetting = table.Column<bool>(type: "bit", nullable: false),
                    ShowDailyProgressBar = table.Column<bool>(type: "bit", nullable: false),
                    DailyTaskGoal = table.Column<int>(type: "int", nullable: false),
                    EnableWeeklySummary = table.Column<bool>(type: "bit", nullable: false),
                    CelebrationIntensity = table.Column<int>(type: "int", nullable: false),
                    SuggestTaskBreakdown = table.Column<bool>(type: "bit", nullable: false),
                    TaskBreakdownThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableBlockerPrompts = table.Column<bool>(type: "bit", nullable: false),
                    StaleTakPromptDays = table.Column<int>(type: "int", nullable: false),
                    EnableQuickCaptureMode = table.Column<bool>(type: "bit", nullable: false),
                    AutoExpandNextActions = table.Column<bool>(type: "bit", nullable: false),
                    EnableMorningPlanningPrompt = table.Column<bool>(type: "bit", nullable: false),
                    MorningPlanningHour = table.Column<int>(type: "int", nullable: false),
                    EnableEveningWindDown = table.Column<bool>(type: "bit", nullable: false),
                    EveningWindDownHour = table.Column<int>(type: "int", nullable: false),
                    EnableCommitmentMode = table.Column<bool>(type: "bit", nullable: false),
                    RequireAbandonReason = table.Column<bool>(type: "bit", nullable: false),
                    Accessibility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextGroupingWeight = table.Column<int>(type: "int", nullable: false),
                    SimilarWorkGroupingWeight = table.Column<int>(type: "int", nullable: false),
                    EnergyMatchingWeight = table.Column<int>(type: "int", nullable: false),
                    DueDateUrgencyWeight = table.Column<int>(type: "int", nullable: false),
                    StakeholderGroupingWeight = table.Column<int>(type: "int", nullable: false),
                    TagSimilarityWeight = table.Column<int>(type: "int", nullable: false),
                    DeepWorkPreferenceWeight = table.Column<int>(type: "int", nullable: false),
                    BlockedTaskPenalty = table.Column<int>(type: "int", nullable: false),
                    LifeAreaRatings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LifeAreaAssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BalanceAiSuggestTasks = table.Column<bool>(type: "bit", nullable: false),
                    BalanceAiSuggestGoals = table.Column<bool>(type: "bit", nullable: false),
                    BalanceAiShowInsights = table.Column<bool>(type: "bit", nullable: false),
                    BalanceAiThreshold = table.Column<int>(type: "int", nullable: false),
                    ContextDimensionMappings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppMode = table.Column<int>(type: "int", nullable: false),
                    AppModeSetAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EnabledBalanceDimensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BalanceRatingsByMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleCalendarAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleCalendarRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleCalendarTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GoogleCalendarEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleCalendarSelectedCalendarIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleCalendarSyncPastDays = table.Column<int>(type: "int", nullable: false),
                    GoogleCalendarSyncFutureDays = table.Column<int>(type: "int", nullable: false),
                    GoogleCalendarLastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutlookCalendarSelectedIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutlookCalendarSyncPastDays = table.Column<int>(type: "int", nullable: false),
                    OutlookCalendarSyncFutureDays = table.Column<int>(type: "int", nullable: false),
                    OutlookCalendarLastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DesiredOutcome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IconImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "References",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedProjectIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_References", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskReminderSnoozes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnoozedUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskReminderSnoozes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Contexts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnergyLevel = table.Column<int>(type: "int", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualMinutes = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WaitingForContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaitingForNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaitingForSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    LinkedTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedMeetingIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresDeepWork = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubtaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockedByTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "int", nullable: true),
                    RecurrenceIntervalDays = table.Column<int>(type: "int", nullable: true),
                    LastRecurrenceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Links = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhoFor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsIconAutoDetected = table.Column<bool>(type: "bit", nullable: false),
                    DetectedCategory = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    LinkedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedTaskIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStart = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalTasksCompleted = table.Column<int>(type: "int", nullable: false),
                    TotalTasksCreated = table.Column<int>(type: "int", nullable: false),
                    TotalMinutesWorked = table.Column<int>(type: "int", nullable: false),
                    TotalMeetingMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalDeepWorkMinutes = table.Column<int>(type: "int", nullable: false),
                    DaysWithMorningCheckin = table.Column<int>(type: "int", nullable: false),
                    HabitsCompletedCount = table.Column<int>(type: "int", nullable: false),
                    BiggestWin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherWins = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BiggestChallenge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonsLearned = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GratitudeReflection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AverageEnergyLevel = table.Column<int>(type: "int", nullable: true),
                    AverageMoodLevel = table.Column<int>(type: "int", nullable: true),
                    WorkLifeBalanceRating = table.Column<int>(type: "int", nullable: true),
                    ProductivityRating = table.Column<int>(type: "int", nullable: true),
                    InboxCleared = table.Column<bool>(type: "bit", nullable: false),
                    ProjectsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    WaitingForReviewed = table.Column<bool>(type: "bit", nullable: false),
                    SomedayMaybeReviewed = table.Column<bool>(type: "bit", nullable: false),
                    CalendarReviewed = table.Column<bool>(type: "bit", nullable: false),
                    NextWeekFocus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextWeekPriorities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captures_UserId",
                table: "Captures",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserId",
                table: "Contacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contexts_UserId",
                table: "Contexts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySnapshots_UserId",
                table: "DailySnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityLinkRules_UserId",
                table: "EntityLinkRules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserId",
                table: "Events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessionLogs_UserId",
                table: "FocusSessionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId",
                table: "Goals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitLogs_UserId",
                table: "HabitLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Habits_UserId",
                table: "Habits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_UserId",
                table: "Ideas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId",
                table: "Preferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserId",
                table: "Projects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_References_UserId",
                table: "References",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskReminderSnoozes_UserId",
                table: "TaskReminderSnoozes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBlocks_UserId",
                table: "TimeBlocks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySnapshots_UserId",
                table: "WeeklySnapshots",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Captures");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Contexts");

            migrationBuilder.DropTable(
                name: "DailySnapshots");

            migrationBuilder.DropTable(
                name: "EntityLinkRules");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "FocusSessionLogs");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "HabitLogs");

            migrationBuilder.DropTable(
                name: "Habits");

            migrationBuilder.DropTable(
                name: "Ideas");

            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "References");

            migrationBuilder.DropTable(
                name: "TaskReminderSnoozes");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "TimeBlocks");

            migrationBuilder.DropTable(
                name: "WeeklySnapshots");
        }
    }
}
