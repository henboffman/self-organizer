# Ideas 7

1. Add concept of an idea to the application, to sit separate from task. These should appear in the summaries, reports, etc. The thought is that the user should be able to capture thoughts that might not lead to tasks- just as a way to capture ideas. And we might already support this in the application by saying that the task isn't actionable, so maybe this is already implemented and we don't need standalone ideas. use your discretion- the goal is that we can explicitly track ideas separate from actionable tasks
2. Users can’t associate existing project or tasks with a goal
3. Users can’t associate existing tasks or goals with project
4. Users can’t edit tasks from project view or goal view
5. Want full task CRUD from project and goal page
6. The goals link in the sidebar needs an icon
7. It’d be nice if we could see the history of the goal and all changes to it and its child tasks
8. We should be able to say that a task depends on another task before we can start it, for the sake of scheduling. When we generate tasks using the LLM, it should be able to identify this dependency chain.
9. If we have multiple tasks, goals, projects with similar deadlines, the scheduler should probably distribute the work based on other criteria for our optimizer
10. It’d be nice if tasks could have energy, context, etc.. Maybe they do, but I want them editable in the edit task screen
11. When creating a goal or project, use the full screen. Again, this makes me wonder if it should be a standalone page instead of a dialog(this came up for goals already).
12. For projects, there is probably more information we might want to track. Although we do need to remember that this is intended to be used by individuals and not as a project management tool. That said, we should focus on fields and actions that the user might want to take about their project. We also don’t want to reinvent project planning apps, however, we need enough functionality for the user to be able to manage their own mental load for the project.
13. For projects, we probably want to track who is on the project besides me, how much they’re going to work on it, what they’re going to do, and then when they’re going to work on it. Then, for any tasks that might be dependent on them, that can be factored into the scheduler.
14. Users should be able to select a project or a goal to use as a starting point. This would create a copy of that object, with all associated children or references, and then allow the user to edit. This would be particularly useful when we’re creating multiple of similar goals or projects.
15. It still doesn’t quite feel like we’re tracking everything we need to correctly schedule our tasks. For everything that is calculated in the scheduler, we need to make sure that it is being captured in the create or edit task page.
16. The styling on the first time use dialog looks rough, but it might be because of my browser extensions. Even if that’s the case, we need to make sure that we are always rendering text on a high contrast background.
17. The first time use dialogue should also ask the user about their LLM preferences
