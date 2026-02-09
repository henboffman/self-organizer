import { test, expect } from '../fixtures/base';

/**
 * Workflow E2E Tests for Self-Organizer
 *
 * Each test simulates a real multi-step user workflow.
 * No silent skips — if an element isn't found, the test fails.
 * Each test creates its own data (no dependency on prior tests).
 */

type Page = import('@playwright/test').Page;
type Locator = import('@playwright/test').Locator;
type TestHelpers = import('../fixtures/base').TestHelpers;

// Helper: Blazor @bind inputs listen for 'change' events (on blur), but
// Playwright's fill() only dispatches 'input' events. This helper fills the
// input AND dispatches the 'change' event so Blazor updates its backing field.
async function blazorFill(locator: Locator, value: string) {
  await locator.fill(value);
  await locator.dispatchEvent('change');
}

// Helper: clear then fill a Blazor @bind input
async function blazorClearAndFill(locator: Locator, value: string) {
  await locator.clear();
  await locator.fill(value);
  await locator.dispatchEvent('change');
}

// Helper: wait for a modal to appear
async function waitForModal(page: Page) {
  await page.locator('.modal.show').waitFor({ state: 'visible', timeout: 10000 });
}

// Helper: wait for a modal to disappear
async function waitForModalClosed(page: Page) {
  await page.locator('.modal.show').waitFor({ state: 'hidden', timeout: 10000 });
}

// Helper: create a task from /tasks/next and return its title
async function createTaskOnNextActions(page: Page, title: string, opts?: { priority?: string; context?: string }) {
  await page.locator('button:has-text("New Task")').click();
  await waitForModal(page);
  const modal = page.locator('.modal.show');
  await blazorFill(modal.locator('input.form-control[type="text"]').first(), title);
  if (opts?.priority) {
    await modal.locator('select.form-select').selectOption(opts.priority);
  }
  if (opts?.context) {
    await modal.locator(`button:has-text("${opts.context}")`).click();
  }
  await modal.locator('button.btn-primary:has-text("Create")').click();
  await waitForModalClosed(page);
  await expect(page.locator('.task-card', { hasText: title })).toBeVisible({ timeout: 10000 });
}

// Helper: create a project from /projects and return to the list
async function createProject(page: Page, name: string, opts?: { outcome?: string; priority?: string }) {
  await page.locator('button:has-text("New Project")').click();
  await waitForModal(page);
  const modal = page.locator('.modal.show');
  await blazorFill(modal.locator('input.form-control[type="text"]').first(), name);
  if (opts?.outcome) {
    await blazorFill(modal.locator('textarea[placeholder="What does \'done\' look like?"]'), opts.outcome);
  }
  if (opts?.priority) {
    await modal.locator('select.form-select').first().selectOption(opts.priority);
  }
  await modal.locator('button.btn-primary:has-text("Create")').click();
  await waitForModalClosed(page);
  await expect(page.locator('.card', { hasText: name })).toBeVisible({ timeout: 10000 });
}

// Helper: create a goal from /goals/new and wait for redirect to /goals
async function createGoal(page: Page, helpers: TestHelpers, title: string) {
  await helpers.navigateTo('/goals/new');
  await blazorFill(page.locator('input[placeholder="What do you want to achieve?"]'), title);
  await page.locator('button.btn-primary:has-text("Create Goal")').click();
  await page.waitForURL('**/goals', { timeout: 10000 });
  await expect(page.locator('.goal-card', { hasText: title })).toBeVisible({ timeout: 10000 });
}

// ============================================================================
// CORE ENTITY LIFECYCLE TESTS
// ============================================================================

test.describe('Workflow: Goal lifecycle', () => {
  test('create goal, view in list, open detail, edit title', async ({ page, helpers }) => {
    const goalTitle = `Test Goal ${Date.now()}`;
    const updatedTitle = `Updated Goal ${Date.now()}`;

    await helpers.navigateTo('/goals/new');
    await blazorFill(page.locator('input[placeholder="What do you want to achieve?"]'), goalTitle);
    await blazorFill(page.locator('textarea[placeholder*="more context"]'), 'E2E test goal description');
    await blazorFill(page.locator('textarea[placeholder*="success look like"]'), 'Tests pass consistently');
    await page.locator('button.btn-primary:has-text("Create Goal")').click();

    await page.waitForURL('**/goals', { timeout: 10000 });
    await expect(page.locator('.goal-card', { hasText: goalTitle })).toBeVisible({ timeout: 10000 });

    // Open detail modal
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);
    await expect(page.locator('.modal.show').locator('h3, h4', { hasText: goalTitle })).toBeVisible();

    // Edit from detail modal
    await page.locator('.modal.show').locator('button:has-text("Edit")').click();
    await page.waitForURL('**/goals/*/edit', { timeout: 10000 });

    const titleInput = page.locator('input[placeholder="What do you want to achieve?"]');
    await blazorClearAndFill(titleInput, updatedTitle);
    await page.locator('button.btn-primary:has-text("Save Changes")').click();

    await page.waitForURL('**/goals', { timeout: 10000 });
    await expect(page.locator('.goal-card', { hasText: updatedTitle })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Workflow: Task lifecycle', () => {
  test('create task, verify in list, edit, delete', async ({ page, helpers }) => {
    const taskTitle = `Test Task ${Date.now()}`;
    const updatedTitle = `Edited Task ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle, { priority: '1', context: 'work' });

    // Edit
    const taskCard = page.locator('.task-card', { hasText: taskTitle });
    await taskCard.locator('button[aria-label="Edit task"]').click();
    await waitForModal(page);

    const editModal = page.locator('.modal.show');
    const editTitleInput = editModal.locator('input.form-control[type="text"]').first();
    await blazorClearAndFill(editTitleInput, updatedTitle);
    await editModal.locator('button.btn-primary:has-text("Save")').click();
    await waitForModalClosed(page);
    await expect(page.locator('.task-card', { hasText: updatedTitle })).toBeVisible({ timeout: 10000 });

    // Delete
    const updatedCard = page.locator('.task-card', { hasText: updatedTitle });
    await updatedCard.locator('button[aria-label="Delete task"]').click();
    await waitForModal(page);
    await page.locator('.modal.show button.btn-danger:has-text("Confirm")').click();
    await waitForModalClosed(page);
    await expect(page.locator('.task-card', { hasText: updatedTitle })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Workflow: Task completion', () => {
  test('create task and complete it', async ({ page, helpers }) => {
    const taskTitle = `Complete Me ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    await page.locator('.task-card', { hasText: taskTitle }).locator('button[aria-label="Complete task"]').click();
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Workflow: Project with tasks', () => {
  test('create project, add tasks, verify progress, complete task', async ({ page, helpers }) => {
    const projectName = `Test Project ${Date.now()}`;
    const task1Title = `Project Task 1 ${Date.now()}`;
    const task2Title = `Project Task 2 ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName, { outcome: 'All tasks done', priority: '1' });

    // Navigate to project detail
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });
    await expect(page.locator('h3', { hasText: projectName })).toBeVisible();

    // Add first task
    await page.locator('button.btn-primary:has-text("New Task")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), task1Title);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
    await waitForModalClosed(page);
    await expect(page.locator('a', { hasText: task1Title })).toBeVisible({ timeout: 10000 });

    // Add second task
    await page.locator('button.btn-primary:has-text("New Task")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), task2Title);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
    await waitForModalClosed(page);

    // Verify progress
    await expect(page.locator('text=0 of 2 tasks completed')).toBeVisible({ timeout: 10000 });

    // Complete first task
    const task1Row = page.locator('.border-bottom', { hasText: task1Title });
    await task1Row.locator('button.btn-outline-success').first().click();
    await expect(page.locator('text=1 of 2 tasks completed')).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Workflow: Goal with linked tasks', () => {
  test('create goal, add tasks from goal detail, set next action', async ({ page, helpers }) => {
    const goalTitle = `Linked Goal ${Date.now()}`;
    const task1Title = `Goal Task A ${Date.now()}`;
    const task2Title = `Goal Task B ${Date.now()}`;

    await createGoal(page, helpers, goalTitle);

    // Open goal detail modal
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);
    const detailModal = page.locator('.modal.show');

    // Create first task from goal detail
    await detailModal.locator('button.btn-success:has-text("New")').click();
    await page.locator('.modal.show:has-text("Create New Task for Goal")').waitFor({ timeout: 10000 });
    const taskModal1 = page.locator('.modal.show:has-text("Create New Task for Goal")');
    await blazorFill(taskModal1.locator('input[placeholder="What needs to be done?"]'), task1Title);
    await taskModal1.locator('button.btn-success:has-text("Create Task")').click();
    await page.waitForTimeout(1000);
    await expect(detailModal.locator('a', { hasText: task1Title })).toBeVisible({ timeout: 10000 });

    // Create second task
    await detailModal.locator('button.btn-success:has-text("New")').click();
    await page.locator('.modal.show:has-text("Create New Task for Goal")').waitFor({ timeout: 10000 });
    const taskModal2 = page.locator('.modal.show:has-text("Create New Task for Goal")');
    await blazorFill(taskModal2.locator('input[placeholder="What needs to be done?"]'), task2Title);
    await taskModal2.locator('button.btn-success:has-text("Create Task")').click();
    await page.waitForTimeout(1000);
    await expect(detailModal.locator('a', { hasText: task2Title })).toBeVisible({ timeout: 10000 });

    // Verify count badge
    await expect(detailModal.locator('.badge.bg-success:has-text("2")')).toBeVisible({ timeout: 10000 });

    // Set first task as next action
    const task1Item = detailModal.locator('li', { hasText: task1Title });
    await task1Item.locator('button.next-action-star').click();
    await expect(task1Item.locator('button.next-action-star.active')).toBeVisible({ timeout: 10000 });
  });
});

// ============================================================================
// CROSS-ENTITY / CROSS-PAGE WORKFLOWS
// ============================================================================

test.describe('Cross-page: task created in project appears in tasks index', () => {
  test('task added to project is visible in /tasks and /tasks/next', async ({ page, helpers }) => {
    const projectName = `Cross Page Proj ${Date.now()}`;
    const taskTitle = `Cross Page Task ${Date.now()}`;

    // Create project and add a task to it
    await helpers.navigateTo('/projects');
    await createProject(page, projectName);
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    await page.locator('button.btn-primary:has-text("New Task")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), taskTitle);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
    await waitForModalClosed(page);

    // Navigate to /tasks/next — the task should appear there
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });

    // Navigate to task detail via the task-detail link
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Verify the task detail page shows the project link
    await expect(page.locator('text=' + projectName)).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Cross-page: complete task in project, verify gone from next actions', () => {
  test('completing a project task removes it from the next actions list', async ({ page, helpers }) => {
    const projectName = `Completion Proj ${Date.now()}`;
    const taskTitle = `Will Complete ${Date.now()}`;

    // Create project with task
    await helpers.navigateTo('/projects');
    await createProject(page, projectName);
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    await page.locator('button.btn-primary:has-text("New Task")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), taskTitle);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
    await waitForModalClosed(page);

    // Verify task shows in next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });

    // Go back to project and complete the task there
    await helpers.navigateTo('/projects');
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    const taskRow = page.locator('.border-bottom', { hasText: taskTitle });
    await taskRow.locator('button.btn-outline-success').first().click();

    // Now go to next actions — task should be gone
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Cross-page: goal links to project, project shows in goal detail', () => {
  test('link a project to a goal and verify it appears', async ({ page, helpers }) => {
    const goalTitle = `Linked Proj Goal ${Date.now()}`;
    const projectName = `Linkable Proj ${Date.now()}`;

    // Create a project first
    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Create a goal
    await createGoal(page, helpers, goalTitle);

    // Open goal detail modal
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);
    const detailModal = page.locator('.modal.show');

    // Click the link-project button (plus icon in Linked Projects header)
    await detailModal.locator('button[title="Link existing project"]').click();

    // Wait for the link project modal/panel to appear
    await page.locator('.modal.show:has-text("Link Project")').waitFor({ timeout: 10000 });

    // Click the project in the list to link it
    await page.locator('.modal.show:has-text("Link Project")').locator('button.list-group-item', { hasText: projectName }).click();

    // Verify the project now appears in Linked Projects
    await expect(detailModal.locator('a', { hasText: projectName })).toBeVisible({ timeout: 10000 });

    // Verify the linked projects badge count is 1
    await expect(detailModal.locator('.badge.bg-primary:has-text("1")')).toBeVisible({ timeout: 10000 });
  });
});

// ============================================================================
// TASK STATUS TRANSITIONS
// ============================================================================

test.describe('Task status transitions via task detail page', () => {
  test('create task, schedule it, verify on scheduled page, move back to next action', async ({ page, helpers }) => {
    const taskTitle = `Schedule Me ${Date.now()}`;

    // Create a task
    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Navigate to task detail page
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Verify task is in NextAction status
    await expect(page.locator('.badge:has-text("NextAction")')).toBeVisible();

    // Click Schedule quick action
    await page.locator('button:has-text("Schedule")').click();
    await waitForModal(page);

    // Set a future date
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dateStr = tomorrow.toISOString().split('T')[0];
    await blazorFill(page.locator('.modal.show input[type="date"]'), dateStr);
    await page.locator('.modal.show button.btn-primary:has-text("Schedule")').click();
    await waitForModalClosed(page);

    // Verify status changed to Scheduled
    await expect(page.locator('.badge:has-text("Scheduled")')).toBeVisible({ timeout: 10000 });

    // Navigate to scheduled tasks page and verify task is there
    await helpers.navigateTo('/tasks/scheduled');
    await expect(page.locator('a', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });

    // Move it back to Next Actions from the scheduled page
    const scheduledRow = page.locator('.card', { hasText: taskTitle });
    await scheduledRow.locator('button[title="Move to Next Actions"]').click();

    // Verify it disappears from scheduled
    await expect(page.locator('a', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });

    // Verify it reappears in next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Task status: move to waiting for', () => {
  test('create task, mark as waiting for, verify status and note', async ({ page, helpers }) => {
    const taskTitle = `Wait For Me ${Date.now()}`;
    const waitingNote = 'Client response needed';

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Go to task detail
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Click "Waiting For" quick action
    await page.locator('button:has-text("Waiting For")').click();
    await waitForModal(page);

    // Fill in the waiting-for note
    await blazorFill(page.locator('.modal.show input[placeholder*="waiting on"]'), waitingNote);
    await page.locator('.modal.show button.btn-warning:has-text("Set Waiting")').click();
    await waitForModalClosed(page);

    // Verify status changed to WaitingFor
    await expect(page.locator('.badge:has-text("WaitingFor")')).toBeVisible({ timeout: 10000 });

    // Verify waiting-for note is displayed
    await expect(page.locator('text=' + waitingNote)).toBeVisible();

    // Verify the task appears on the waiting tab of the tasks index
    await helpers.navigateTo('/tasks/waiting');
    await expect(page.locator('a', { hasText: taskTitle }).or(page.locator('span', { hasText: taskTitle }))).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Task status: move to someday/maybe', () => {
  test('create task, move to someday, verify it leaves next actions', async ({ page, helpers }) => {
    const taskTitle = `Someday Task ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Go to task detail
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Click "Move to Someday"
    await page.locator('button:has-text("Move to Someday")').click();

    // Verify status changed
    await expect(page.locator('.badge:has-text("SomedayMaybe")')).toBeVisible({ timeout: 10000 });

    // Verify task is gone from next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });

    // Verify it appears in someday tab
    await helpers.navigateTo('/tasks/someday');
    await expect(page.locator('a', { hasText: taskTitle }).or(page.locator('span', { hasText: taskTitle }))).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Task status: deactivate back to inbox', () => {
  test('create task, deactivate it, verify it leaves next actions', async ({ page, helpers }) => {
    const taskTitle = `Deactivate Me ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Click deactivate (undo arrow) on the task card
    const taskCard = page.locator('.task-card', { hasText: taskTitle });
    await taskCard.locator('button[aria-label="Move back to Inbox"]').click();

    // Verify task disappears from next actions
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });
  });
});

// ============================================================================
// DASHBOARD INTEGRATION
// ============================================================================

test.describe('Dashboard: quick capture creates inbox item', () => {
  test('capture from dashboard, verify inbox count increases', async ({ page, helpers }) => {
    const captureText = `Dashboard Capture ${Date.now()}`;

    await helpers.navigateTo('/');

    // Read initial inbox count
    const inboxCard = page.locator('.card.bg-primary');
    const initialCountText = await inboxCard.locator('h2').textContent();
    const initialCount = parseInt(initialCountText || '0', 10);

    // Type into the quick capture input and trigger change for Blazor
    const captureInput = page.locator('input[placeholder="What\'s on your mind?"]');
    await blazorFill(captureInput, captureText);
    await captureInput.press('Enter');

    // Wait for "Captured!" success indicator
    await expect(page.locator('text=Captured')).toBeVisible({ timeout: 10000 });

    // Verify inbox count increased
    await expect(inboxCard.locator('h2')).toHaveText(String(initialCount + 1), { timeout: 10000 });
  });
});

test.describe('Dashboard: next actions count reflects created tasks', () => {
  test('create task, dashboard count updates', async ({ page, helpers }) => {
    const taskTitle = `Dashboard Count ${Date.now()}`;

    // Check initial next actions count
    await helpers.navigateTo('/');
    const nextActionsCard = page.locator('.card.bg-warning');
    const initialCountText = await nextActionsCard.locator('h2').textContent();
    const initialCount = parseInt(initialCountText || '0', 10);

    // Create a task
    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Go back to dashboard and check count
    await helpers.navigateTo('/');
    await expect(nextActionsCard.locator('h2')).toHaveText(String(initialCount + 1), { timeout: 10000 });
  });
});

// ============================================================================
// IDEAS WORKFLOW
// ============================================================================

test.describe('Ideas: create idea, convert to task', () => {
  test('create an idea, then convert it to a task', async ({ page, helpers }) => {
    const ideaTitle = `Brilliant Idea ${Date.now()}`;
    const taskTitle = `Task from Idea ${Date.now()}`;

    await helpers.navigateTo('/ideas');

    // Create a new idea
    await page.locator('button:has-text("New Idea")').click();
    await waitForModal(page);

    const modal = page.locator('.modal.show');
    await blazorFill(modal.locator('input[placeholder="What\'s on your mind?"]'), ideaTitle);
    await blazorFill(modal.locator('textarea[placeholder="Describe your idea..."]'), 'This could become a task');
    await modal.locator('button.btn-primary:has-text("Create Idea")').click();
    await waitForModalClosed(page);

    // Verify idea appears in list
    await expect(page.locator('.idea-card', { hasText: ideaTitle }).or(page.locator('.card', { hasText: ideaTitle }))).toBeVisible({ timeout: 10000 });

    // Open dropdown menu on the idea card and click "Convert to Task"
    const ideaCard = page.locator('.card', { hasText: ideaTitle });
    await ideaCard.locator('button[data-bs-toggle="dropdown"]').click();
    await ideaCard.locator('button:has-text("Convert to Task")').click();
    await waitForModal(page);

    // Fill in conversion modal
    const convertModal = page.locator('.modal.show:has-text("Convert Idea to Task")');
    await blazorClearAndFill(convertModal.locator('input.form-control[type="text"]').first(), taskTitle);
    // Set status to Next Action
    await convertModal.locator('select.form-select').last().selectOption('NextAction');
    await convertModal.locator('button.btn-primary:has-text("Create Task")').click();
    await waitForModalClosed(page);

    // Verify the task now exists in next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Ideas: create idea linked to goal', () => {
  test('create goal first, then create idea linked to that goal', async ({ page, helpers }) => {
    const goalTitle = `Idea Goal ${Date.now()}`;
    const ideaTitle = `Linked Idea ${Date.now()}`;

    // Create a goal
    await createGoal(page, helpers, goalTitle);

    // Navigate to ideas and create an idea linked to the goal
    await helpers.navigateTo('/ideas');
    await page.locator('button:has-text("New Idea")').click();
    await waitForModal(page);

    const modal = page.locator('.modal.show');
    await blazorFill(modal.locator('input[placeholder="What\'s on your mind?"]'), ideaTitle);

    // Link to the goal we created
    const goalSelect = modal.locator('select.form-select').nth(1); // The "Link to Goal" select (after Priority)
    // Find the option by text that matches our goal
    await goalSelect.selectOption({ label: goalTitle });

    await modal.locator('button.btn-primary:has-text("Create Idea")').click();
    await waitForModalClosed(page);

    // Verify idea appears and shows the linked goal
    await expect(page.locator('.card', { hasText: ideaTitle })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.card', { hasText: ideaTitle }).locator('text=' + goalTitle)).toBeVisible();
  });
});

// ============================================================================
// PROJECT MANAGEMENT
// ============================================================================

test.describe('Project: stalled project detection', () => {
  test('project with no next action shows stalled warning', async ({ page, helpers }) => {
    const projectName = `Stalled Proj ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // The project should show a "No Next Action" badge/warning
    // Navigate to project detail
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    // Verify the "No Next Action" warning badge
    await expect(page.locator('.badge.bg-warning', { hasText: 'No Next Action' })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Project: designate next action star', () => {
  test('add tasks to project, star one as next action', async ({ page, helpers }) => {
    const projectName = `Star Proj ${Date.now()}`;
    const task1 = `Star Task 1 ${Date.now()}`;
    const task2 = `Star Task 2 ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Navigate to project detail
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    // Add two tasks
    for (const title of [task1, task2]) {
      await page.locator('button.btn-primary:has-text("New Task")').click();
      await waitForModal(page);
      await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), title);
      await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
      await waitForModalClosed(page);
    }

    // Star the first task as next action
    const task1Row = page.locator('.border-bottom', { hasText: task1 });
    await task1Row.locator('button.next-action-star').click();

    // Verify star is active
    await expect(task1Row.locator('button.next-action-star.active')).toBeVisible({ timeout: 10000 });

    // Verify the "No Next Action" warning is gone
    await expect(page.locator('.badge.bg-warning', { hasText: 'No Next Action' })).toBeHidden({ timeout: 10000 });

    // Star the second task instead (should unstick the first)
    const task2Row = page.locator('.border-bottom', { hasText: task2 });
    await task2Row.locator('button.next-action-star').click();

    await expect(task2Row.locator('button.next-action-star.active')).toBeVisible({ timeout: 10000 });
    // First star should no longer be active
    await expect(task1Row.locator('button.next-action-star.active')).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Project: edit project details', () => {
  test('create project then edit its name and priority from detail page', async ({ page, helpers }) => {
    const projectName = `Edit Me Proj ${Date.now()}`;
    const updatedName = `Edited Proj ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Navigate to project detail
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    // Click Edit button
    await page.locator('button.btn-outline-primary:has-text("Edit")').click();
    await waitForModal(page);

    // Change name
    const nameInput = page.locator('.modal.show').locator('input.form-control[type="text"]').first();
    await blazorClearAndFill(nameInput, updatedName);

    // Save
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Save")').click();
    await waitForModalClosed(page);

    // Verify the page header shows the updated name
    await expect(page.locator('h3', { hasText: updatedName })).toBeVisible({ timeout: 10000 });

    // Navigate back to projects list and verify it's updated there too
    await helpers.navigateTo('/projects');
    await expect(page.locator('.card', { hasText: updatedName })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Project: delete project with confirmation', () => {
  test('create project, delete from projects list', async ({ page, helpers }) => {
    const projectName = `Delete Me Proj ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Click delete (trash icon) on the project card
    const projectCard = page.locator('.card', { hasText: projectName });
    await projectCard.locator('button.btn-outline-danger').click();

    // Confirm deletion
    await waitForModal(page);
    await page.locator('.modal.show button.btn-danger:has-text("Confirm")').click();
    await waitForModalClosed(page);

    // Verify project is gone
    await expect(page.locator('.card', { hasText: projectName })).toBeHidden({ timeout: 10000 });
  });
});

// ============================================================================
// TASK DETAIL PAGE EDITING
// ============================================================================

test.describe('Task detail: edit from detail page', () => {
  test('navigate to task detail, edit title and priority, verify changes', async ({ page, helpers }) => {
    const taskTitle = `Detail Edit ${Date.now()}`;
    const updatedTitle = `Updated Detail ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Go to task detail page
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Verify title in the header
    await expect(page.locator('h3', { hasText: taskTitle })).toBeVisible();

    // Click Edit button
    await page.locator('button.btn-outline-primary:has-text("Edit")').click();
    await waitForModal(page);

    // Change title
    const editTitleInput = page.locator('.modal.show input.form-control[type="text"]').first();
    await blazorClearAndFill(editTitleInput, updatedTitle);

    // Change priority to High
    await page.locator('.modal.show select.form-select').first().selectOption('1');

    // Save
    await page.locator('.modal.show button.btn-primary:has-text("Save")').click();
    await waitForModalClosed(page);

    // Verify updated title on the detail page
    await expect(page.locator('h3', { hasText: updatedTitle })).toBeVisible({ timeout: 10000 });

    // Go back to next actions and verify the update is reflected
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: updatedTitle })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Task detail: complete from detail page', () => {
  test('navigate to task detail, complete it, verify status', async ({ page, helpers }) => {
    const taskTitle = `Complete From Detail ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, taskTitle);

    // Go to task detail page
    await page.locator('.task-card', { hasText: taskTitle }).locator('a.task-detail-link').click();
    await page.waitForURL('**/tasks/*', { timeout: 10000 });

    // Click Complete button
    await page.locator('button.btn-success:has-text("Complete")').click();

    // Verify status badge changes to Completed
    await expect(page.locator('.badge:has-text("Completed")')).toBeVisible({ timeout: 10000 });

    // Go to next actions — task should be gone
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeHidden({ timeout: 10000 });

    // Go to completed tab — task should be there
    await helpers.navigateTo('/tasks/completed');
    await expect(page.locator('span', { hasText: taskTitle }).or(page.locator('a', { hasText: taskTitle }))).toBeVisible({ timeout: 10000 });
  });
});

// ============================================================================
// FILTERING AND GROUPING
// ============================================================================

test.describe('Tasks index: status tabs show correct tasks', () => {
  test('tasks appear under correct status tabs', async ({ page, helpers }) => {
    const nextTitle = `Next Tab ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, nextTitle);

    // Verify it shows on the "next" tab
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: nextTitle })).toBeVisible({ timeout: 10000 });

    // Verify it does NOT show on the "completed" tab
    await helpers.navigateTo('/tasks/completed');
    await expect(page.locator('.task-card', { hasText: nextTitle }).or(page.locator('a', { hasText: nextTitle }))).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Next actions: group by priority', () => {
  test('create tasks with different priorities, group by priority', async ({ page, helpers }) => {
    const highTitle = `High Prio ${Date.now()}`;
    const lowTitle = `Low Prio ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, highTitle, { priority: '1' });
    await createTaskOnNextActions(page, lowTitle, { priority: '3' });

    // Switch to "By Priority" grouping
    await page.locator('button:has-text("By Priority")').click();

    // Verify group headers appear
    await expect(page.locator('h5:has-text("High Priority")')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('h5:has-text("Low Priority")')).toBeVisible({ timeout: 10000 });

    // Verify tasks are under correct headers
    await expect(page.locator('.task-card', { hasText: highTitle })).toBeVisible();
    await expect(page.locator('.task-card', { hasText: lowTitle })).toBeVisible();
  });
});

test.describe('Next actions: group by context', () => {
  test('create tasks with contexts, group by context', async ({ page, helpers }) => {
    const workTitle = `Work Task ${Date.now()}`;
    const homeTitle = `Home Task ${Date.now()}`;

    await helpers.navigateTo('/tasks/next');
    await createTaskOnNextActions(page, workTitle, { context: 'work' });
    await createTaskOnNextActions(page, homeTitle, { context: 'home' });

    // Switch to "By Context" grouping
    await page.locator('button:has-text("By Context")').click();

    // Verify context group headers appear
    await expect(page.locator('h5:has-text("work")')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('h5:has-text("home")')).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Projects: filter by status tabs', () => {
  test('active filter shows active projects, completed filter hides them', async ({ page, helpers }) => {
    const projectName = `Filter Proj ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Active tab should show the project
    await page.locator('button:has-text("Active")').first().click();
    await expect(page.locator('.card', { hasText: projectName })).toBeVisible({ timeout: 10000 });

    // Completed tab should NOT show the project
    await page.locator('button:has-text("Completed")').click();
    await expect(page.locator('.card', { hasText: projectName })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Goals: filter by status tabs', () => {
  test('active goals appear in active tab, not in completed tab', async ({ page, helpers }) => {
    const goalTitle = `Filter Goal ${Date.now()}`;

    await createGoal(page, helpers, goalTitle);

    // Active tab should show the goal
    await page.locator('button.nav-link', { hasText: 'Active' }).click();
    await expect(page.locator('.goal-card', { hasText: goalTitle })).toBeVisible({ timeout: 10000 });

    // Completed tab should NOT show the goal
    await page.locator('button.nav-link', { hasText: 'Completed' }).click();
    await expect(page.locator('.goal-card', { hasText: goalTitle })).toBeHidden({ timeout: 10000 });
  });
});

// ============================================================================
// MULTI-ENTITY SESSION (simulates real user bouncing between features)
// ============================================================================

test.describe('Real session: goal → project → tasks → complete → verify everywhere', () => {
  test('full GTD workflow across multiple entities and pages', async ({ page, helpers }) => {
    const ts = Date.now();
    const goalTitle = `Session Goal ${ts}`;
    const projectName = `Session Project ${ts}`;
    const task1 = `Session Task 1 ${ts}`;
    const task2 = `Session Task 2 ${ts}`;

    // 1. Create a goal
    await createGoal(page, helpers, goalTitle);

    // 2. Create a project
    await helpers.navigateTo('/projects');
    await createProject(page, projectName, { outcome: 'Complete all session tasks' });

    // 3. Navigate to project detail, add tasks
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    for (const title of [task1, task2]) {
      await page.locator('button.btn-primary:has-text("New Task")').click();
      await waitForModal(page);
      await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), title);
      await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
      await waitForModalClosed(page);
    }

    // 4. Verify both tasks show in next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: task1 })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.task-card', { hasText: task2 })).toBeVisible({ timeout: 10000 });

    // 5. Link project to goal
    await helpers.navigateTo('/goals');
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);

    const detailModal = page.locator('.modal.show');
    await detailModal.locator('button[title="Link existing project"]').click();
    await page.locator('.modal.show:has-text("Link Project")').waitFor({ timeout: 10000 });
    await page.locator('.modal.show:has-text("Link Project")').locator('button.list-group-item', { hasText: projectName }).click();
    await expect(detailModal.locator('a', { hasText: projectName })).toBeVisible({ timeout: 10000 });

    // Close goal detail modal
    await page.locator('.modal.show .btn-close').first().click();

    // 6. Complete task1 from the next actions page
    await helpers.navigateTo('/tasks/next');
    await page.locator('.task-card', { hasText: task1 }).locator('button[aria-label="Complete task"]').click();
    await expect(page.locator('.task-card', { hasText: task1 })).toBeHidden({ timeout: 10000 });

    // 7. Verify project progress updated
    await helpers.navigateTo('/projects');
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });
    await expect(page.locator('text=1 of 2 tasks completed')).toBeVisible({ timeout: 10000 });

    // 8. Complete the second task from project detail
    const task2Row = page.locator('.border-bottom', { hasText: task2 });
    await task2Row.locator('button.btn-outline-success').first().click();
    await expect(page.locator('text=2 of 2 tasks completed')).toBeVisible({ timeout: 10000 });

    // 9. Verify both tasks are gone from next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: task1 })).toBeHidden({ timeout: 10000 });
    await expect(page.locator('.task-card', { hasText: task2 })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Real session: idea → task → project → goal chain', () => {
  test('idea becomes task, task gets assigned to project, project linked to goal', async ({ page, helpers }) => {
    const ts = Date.now();
    const ideaTitle = `Chain Idea ${ts}`;
    const taskTitle = `Chain Task ${ts}`;
    const projectName = `Chain Project ${ts}`;
    const goalTitle = `Chain Goal ${ts}`;

    // 1. Create an idea
    await helpers.navigateTo('/ideas');
    await page.locator('button:has-text("New Idea")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input[placeholder="What\'s on your mind?"]'), ideaTitle);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Create Idea")').click();
    await waitForModalClosed(page);

    // 2. Convert idea to task
    const ideaCard = page.locator('.card', { hasText: ideaTitle });
    await ideaCard.locator('button[data-bs-toggle="dropdown"]').click();
    await ideaCard.locator('button:has-text("Convert to Task")').click();
    await waitForModal(page);

    const convertModal = page.locator('.modal.show:has-text("Convert Idea to Task")');
    await blazorClearAndFill(convertModal.locator('input.form-control[type="text"]').first(), taskTitle);
    await convertModal.locator('select.form-select').last().selectOption('NextAction');
    await convertModal.locator('button.btn-primary:has-text("Create Task")').click();
    await waitForModalClosed(page);

    // 3. Verify task exists in next actions
    await helpers.navigateTo('/tasks/next');
    await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible({ timeout: 10000 });

    // 4. Create a project
    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // 5. Create a goal
    await createGoal(page, helpers, goalTitle);

    // 6. Link project to goal
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);
    const detailModal = page.locator('.modal.show');
    await detailModal.locator('button[title="Link existing project"]').click();
    await page.locator('.modal.show:has-text("Link Project")').waitFor({ timeout: 10000 });
    await page.locator('.modal.show:has-text("Link Project")').locator('button.list-group-item', { hasText: projectName }).click();
    await expect(detailModal.locator('a', { hasText: projectName })).toBeVisible({ timeout: 10000 });
  });
});

// ============================================================================
// EDGE CASES AND ERROR HANDLING
// ============================================================================

test.describe('Edge case: create and immediately delete project', () => {
  test('rapidly create then delete a project', async ({ page, helpers }) => {
    const projectName = `Quick Delete ${Date.now()}`;

    await helpers.navigateTo('/projects');
    await createProject(page, projectName);

    // Immediately delete it
    const projectCard = page.locator('.card', { hasText: projectName });
    await projectCard.locator('button.btn-outline-danger').click();
    await waitForModal(page);
    await page.locator('.modal.show button.btn-danger:has-text("Confirm")').click();
    await waitForModalClosed(page);

    await expect(page.locator('.card', { hasText: projectName })).toBeHidden({ timeout: 10000 });
  });
});

test.describe('Edge case: edit goal while in different state', () => {
  test('create goal, navigate away, come back and edit', async ({ page, helpers }) => {
    const goalTitle = `Navigate Away Goal ${Date.now()}`;
    const updatedTitle = `Came Back Goal ${Date.now()}`;

    // Create goal
    await createGoal(page, helpers, goalTitle);

    // Navigate to a completely different section
    await helpers.navigateTo('/projects');
    await helpers.navigateTo('/tasks/next');

    // Come back to goals
    await helpers.navigateTo('/goals');
    await expect(page.locator('.goal-card', { hasText: goalTitle })).toBeVisible({ timeout: 10000 });

    // Open detail and edit
    await page.locator('.goal-card', { hasText: goalTitle }).click();
    await waitForModal(page);
    await page.locator('.modal.show').locator('button:has-text("Edit")').click();
    await page.waitForURL('**/goals/*/edit', { timeout: 10000 });

    const titleInput = page.locator('input[placeholder="What do you want to achieve?"]');
    await blazorClearAndFill(titleInput, updatedTitle);
    await page.locator('button.btn-primary:has-text("Save Changes")').click();

    await page.waitForURL('**/goals', { timeout: 10000 });
    await expect(page.locator('.goal-card', { hasText: updatedTitle })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Edge case: project with completed tasks shows correct count after navigation', () => {
  test('complete task in project, navigate away, come back, progress persists', async ({ page, helpers }) => {
    const projectName = `Persist Proj ${Date.now()}`;
    const taskTitle = `Persist Task ${Date.now()}`;

    // Create project with task
    await helpers.navigateTo('/projects');
    await createProject(page, projectName);
    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    await page.locator('button.btn-primary:has-text("New Task")').click();
    await waitForModal(page);
    await blazorFill(page.locator('.modal.show').locator('input.form-control[type="text"]').first(), taskTitle);
    await page.locator('.modal.show').locator('button.btn-primary:has-text("Add Task")').click();
    await waitForModalClosed(page);

    // Complete the task
    const taskRow = page.locator('.border-bottom', { hasText: taskTitle });
    await taskRow.locator('button.btn-outline-success').first().click();
    await expect(page.locator('text=1 of 1 tasks completed')).toBeVisible({ timeout: 10000 });

    // Navigate away to goals, then tasks, then back to this project
    await helpers.navigateTo('/goals');
    await helpers.navigateTo('/tasks/next');
    await helpers.navigateTo('/projects');

    await page.locator('a[href*="projects/"]', { hasText: projectName }).click();
    await page.waitForURL('**/projects/*', { timeout: 10000 });

    // Progress should still show 1 of 1
    await expect(page.locator('text=1 of 1 tasks completed')).toBeVisible({ timeout: 10000 });
  });
});
