namespace SelfOrganizer.Core.Models;

/// <summary>
/// Holds all entity collections needed for career plan HTML export.
/// Allows passing pre-assembled demo data without touching repositories.
/// </summary>
public record CareerExportData(
    List<CareerPlan> Plans,
    List<Skill> Skills,
    List<Goal> Goals,
    List<Project> Projects,
    List<Habit> Habits,
    List<HabitLog> HabitLogs,
    List<FocusSessionLog> FocusSessions,
    List<WeeklySnapshot> WeeklySnapshots,
    List<GrowthSnapshot> GrowthSnapshots,
    List<TodoTask> Tasks
);
