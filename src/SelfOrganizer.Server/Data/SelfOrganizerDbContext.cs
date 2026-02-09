using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfOrganizer.Core.Models;

namespace SelfOrganizer.Server.Data;

/// <summary>
/// Entity Framework DbContext for SelfOrganizer.
/// All entities include a UserId shadow property for multi-tenant data isolation.
/// </summary>
public class SelfOrganizerDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SelfOrganizerDbContext(DbContextOptions<SelfOrganizerDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<CaptureItem> Captures => Set<CaptureItem>();
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<ReferenceItem> References => Set<ReferenceItem>();
    public DbSet<Context> Contexts => Set<Context>();
    public DbSet<CategoryDefinition> Categories => Set<CategoryDefinition>();
    public DbSet<UserPreferences> Preferences => Set<UserPreferences>();
    public DbSet<DailySnapshot> DailySnapshots => Set<DailySnapshot>();
    public DbSet<WeeklySnapshot> WeeklySnapshots => Set<WeeklySnapshot>();
    public DbSet<TimeBlock> TimeBlocks => Set<TimeBlock>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<EntityLinkRule> EntityLinkRules => Set<EntityLinkRule>();
    public DbSet<FocusSessionLog> FocusSessionLogs => Set<FocusSessionLog>();
    public DbSet<TaskReminderSnooze> TaskReminderSnoozes => Set<TaskReminderSnooze>();
    public DbSet<CareerPlan> CareerPlans => Set<CareerPlan>();
    public DbSet<GrowthSnapshot> GrowthSnapshots => Set<GrowthSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add UserId shadow property to all BaseEntity types for multi-tenant support
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<string>("UserId")
                    .HasMaxLength(50)
                    .IsRequired();

                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex("UserId");
            }
        }

        // Configure converters for complex types
        ConfigureTodoTask(modelBuilder);
        ConfigureProject(modelBuilder);
        ConfigureGoal(modelBuilder);
        ConfigureCalendarEvent(modelBuilder);
        ConfigureHabit(modelBuilder);
        ConfigureSkill(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureDailySnapshot(modelBuilder);
        ConfigureWeeklySnapshot(modelBuilder);
        ConfigureIdea(modelBuilder);
        ConfigureReferenceItem(modelBuilder);
        ConfigureContext(modelBuilder);
        ConfigureCategoryDefinition(modelBuilder);
        ConfigureTimeBlock(modelBuilder);
        ConfigureFocusSessionLog(modelBuilder);
        ConfigureEntityLinkRule(modelBuilder);
        ConfigureCareerPlan(modelBuilder);
        ConfigureGrowthSnapshot(modelBuilder);
    }

    private void ConfigureTodoTask(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TodoTask>();

        entity.Property(e => e.Contexts)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.Links)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedMeetingIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.SubtaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.BlockedByTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.GoalIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.SkillIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureProject(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Project>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedSkillIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureGoal(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Goal>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedProjectIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedHabitIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedSkillIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.BalanceDimensionIds)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());
    }

    private void ConfigureCalendarEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CalendarEvent>();

        entity.Property(e => e.Attendees)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureHabit(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Habit>();

        entity.Property(e => e.LinkedGoalIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedSkillIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.TrackedDays)
            .HasConversion(CreateListConverter<DayOfWeek>(), CreateListComparer<DayOfWeek>());
    }

    private void ConfigureSkill(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Skill>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedGoalIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedProjectIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedHabitIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureUserPreferences(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserPreferences>();

        entity.Property(e => e.WorkDays)
            .HasConversion(CreateListConverter<DayOfWeek>(), CreateListComparer<DayOfWeek>());

        entity.Property(e => e.GoogleCalendarSelectedCalendarIds)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.OutlookCalendarSelectedIds)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.EnabledBalanceDimensions)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LifeAreaRatings)
            .HasConversion(CreateDictionaryConverter<string, int>(), CreateDictionaryComparer<string, int>());

        entity.Property(e => e.ContextDimensionMappings)
            .HasConversion(
                CreateDictionaryOfListsConverter<string, string>(),
                CreateDictionaryOfListsComparer<string, string>());

        entity.Property(e => e.BalanceRatingsByMode)
            .HasConversion(
                CreateNestedDictionaryConverter<string, string, int>(),
                CreateNestedDictionaryComparer<string, string, int>());

        entity.Property(e => e.Accessibility)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<AccessibilitySettings>(v, JsonOptions));
    }

    private void ConfigureDailySnapshot(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DailySnapshot>();

        entity.Property(e => e.TopPriorityTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureWeeklySnapshot(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WeeklySnapshot>();

        entity.Property(e => e.OtherWins)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.NextWeekPriorities)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());
    }

    private void ConfigureIdea(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Idea>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        // LinkedGoalId and LinkedProjectId are single Guid? properties, no conversion needed
    }

    private void ConfigureReferenceItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ReferenceItem>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());
    }

    private void ConfigureContext(ModelBuilder modelBuilder)
    {
        // Context entity has no list properties that need conversion
    }

    private void ConfigureCategoryDefinition(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CategoryDefinition>();

        entity.Property(e => e.MatchTerms)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());
    }

    private void ConfigureTimeBlock(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TimeBlock>();

        entity.Property(e => e.AssignedTaskIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());
    }

    private void ConfigureFocusSessionLog(ModelBuilder modelBuilder)
    {
        // FocusSessionLog has no list properties that need conversion
    }

    private void ConfigureEntityLinkRule(ModelBuilder modelBuilder)
    {
        // EntityLinkRule has no list properties that need conversion
        // TargetEntityId is a single Guid?, not a list
    }

    private void ConfigureCareerPlan(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CareerPlan>();

        entity.Property(e => e.Tags)
            .HasConversion(CreateListConverter<string>(), CreateListComparer<string>());

        entity.Property(e => e.LinkedGoalIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedSkillIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedProjectIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.LinkedHabitIds)
            .HasConversion(CreateListConverter<Guid>(), CreateListComparer<Guid>());

        entity.Property(e => e.Milestones)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<CareerMilestone>(), JsonOptions),
                v => string.IsNullOrEmpty(v)
                    ? new List<CareerMilestone>()
                    : JsonSerializer.Deserialize<List<CareerMilestone>>(v, JsonOptions) ?? new List<CareerMilestone>());

        entity.Ignore(e => e.ProgressPercent);
    }

    private void ConfigureGrowthSnapshot(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GrowthSnapshot>();

        entity.Property(e => e.SkillProficiencies)
            .HasConversion(CreateDictionaryConverter<Guid, int>(), CreateDictionaryComparer<Guid, int>());

        entity.Property(e => e.SkillNames)
            .HasConversion(CreateDictionaryConverter<Guid, string>(), CreateDictionaryComparer<Guid, string>());

        entity.Property(e => e.GoalProgress)
            .HasConversion(CreateDictionaryConverter<Guid, int>(), CreateDictionaryComparer<Guid, int>());

        entity.Property(e => e.GoalTitles)
            .HasConversion(CreateDictionaryConverter<Guid, string>(), CreateDictionaryComparer<Guid, string>());

        entity.Property(e => e.BalanceRatings)
            .HasConversion(CreateDictionaryConverter<string, int>(), CreateDictionaryComparer<string, int>());
    }

    #region Value Converters and Comparers

    private static ValueConverter<List<T>, string> CreateListConverter<T>()
    {
        return new ValueConverter<List<T>, string>(
            v => JsonSerializer.Serialize(v ?? new List<T>(), JsonOptions),
            v => string.IsNullOrEmpty(v)
                ? new List<T>()
                : JsonSerializer.Deserialize<List<T>>(v, JsonOptions) ?? new List<T>());
    }

    private static ValueComparer<List<T>> CreateListComparer<T>()
    {
        return new ValueComparer<List<T>>(
            (c1, c2) => (c1 ?? new List<T>()).SequenceEqual(c2 ?? new List<T>()),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())),
            c => c == null ? new List<T>() : c.ToList());
    }

    private static ValueConverter<Dictionary<TKey, TValue>?, string> CreateDictionaryConverter<TKey, TValue>()
        where TKey : notnull
    {
        return new ValueConverter<Dictionary<TKey, TValue>?, string>(
            v => JsonSerializer.Serialize(v ?? new Dictionary<TKey, TValue>(), JsonOptions),
            v => string.IsNullOrEmpty(v)
                ? new Dictionary<TKey, TValue>()
                : JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(v, JsonOptions) ?? new Dictionary<TKey, TValue>());
    }

    private static ValueComparer<Dictionary<TKey, TValue>?> CreateDictionaryComparer<TKey, TValue>()
        where TKey : notnull
    {
        return new ValueComparer<Dictionary<TKey, TValue>?>(
            (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
            c => c == null ? new Dictionary<TKey, TValue>() : JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions));
    }

    private static ValueConverter<Dictionary<TKey, List<TValue>>?, string> CreateDictionaryOfListsConverter<TKey, TValue>()
        where TKey : notnull
    {
        return new ValueConverter<Dictionary<TKey, List<TValue>>?, string>(
            v => JsonSerializer.Serialize(v ?? new Dictionary<TKey, List<TValue>>(), JsonOptions),
            v => string.IsNullOrEmpty(v)
                ? new Dictionary<TKey, List<TValue>>()
                : JsonSerializer.Deserialize<Dictionary<TKey, List<TValue>>>(v, JsonOptions) ?? new Dictionary<TKey, List<TValue>>());
    }

    private static ValueComparer<Dictionary<TKey, List<TValue>>?> CreateDictionaryOfListsComparer<TKey, TValue>()
        where TKey : notnull
    {
        return new ValueComparer<Dictionary<TKey, List<TValue>>?>(
            (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
            c => c == null ? new Dictionary<TKey, List<TValue>>() : JsonSerializer.Deserialize<Dictionary<TKey, List<TValue>>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions));
    }

    private static ValueConverter<Dictionary<TKey1, Dictionary<TKey2, TValue>>?, string> CreateNestedDictionaryConverter<TKey1, TKey2, TValue>()
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return new ValueConverter<Dictionary<TKey1, Dictionary<TKey2, TValue>>?, string>(
            v => JsonSerializer.Serialize(v ?? new Dictionary<TKey1, Dictionary<TKey2, TValue>>(), JsonOptions),
            v => string.IsNullOrEmpty(v)
                ? new Dictionary<TKey1, Dictionary<TKey2, TValue>>()
                : JsonSerializer.Deserialize<Dictionary<TKey1, Dictionary<TKey2, TValue>>>(v, JsonOptions) ?? new Dictionary<TKey1, Dictionary<TKey2, TValue>>());
    }

    private static ValueComparer<Dictionary<TKey1, Dictionary<TKey2, TValue>>?> CreateNestedDictionaryComparer<TKey1, TKey2, TValue>()
        where TKey1 : notnull
        where TKey2 : notnull
    {
        return new ValueComparer<Dictionary<TKey1, Dictionary<TKey2, TValue>>?>(
            (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
            c => c == null ? new Dictionary<TKey1, Dictionary<TKey2, TValue>>() : JsonSerializer.Deserialize<Dictionary<TKey1, Dictionary<TKey2, TValue>>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions));
    }

    #endregion
}
