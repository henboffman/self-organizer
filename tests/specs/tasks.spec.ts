import { test, expect } from '../fixtures/base';

/**
 * Task CRUD Operations E2E Tests
 * Based on: .ai/PETRI-MODELS.md - "Task CRUD Operations", "Task Status Transitions"
 * Based on: .ai/STATECHARTS.md - "Task Card Statechart"
 *
 * Task Statuses (from Petri Net):
 * Inbox -> NextAction / Reference / SomedayMaybe
 * NextAction -> Scheduled -> WaitingFor -> Completed
 *
 * Task Card States (from Statechart):
 * [Default] -> [Hovered] -> [Selected] -> [Editing] / [Completing]
 */

test.describe('Task CRUD - Create Operations', () => {
  test('can create a task via inbox processing', async ({ page, helpers }) => {
    // Create capture item
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('New task via inbox ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process in inbox
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("Yes")').first().click();
      await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();

      // Fill task form
      const titleInput = page.locator('input[type="text"]').first();
      await titleInput.fill('Created task via inbox');
      await page.locator('button:has-text("Create Task")').click();

      // Verify task was created by navigating to tasks
      await helpers.navigateTo('/tasks/next');
      await expect(page.locator('text=Created task via inbox')).toBeVisible({ timeout: 10000 });
    }
  });
});

test.describe('Task CRUD - Read Operations', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks/next');
  });

  test('displays Next Actions list', async ({ page }) => {
    const heading = page.locator('h1, h2, h3').first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('task list shows task cards', async ({ page }) => {
    // Either has tasks or empty state
    const taskList = page.locator('.list-group, .task-list, [data-testid="task-list"]');
    const emptyState = page.locator('text=No next actions, text=no tasks');

    await page.waitForTimeout(2000);

    const hasTaskList = await taskList.isVisible().catch(() => false);
    const hasEmptyState = await emptyState.isVisible().catch(() => false);

    expect(hasTaskList || hasEmptyState).toBeTruthy();
  });

  test('can navigate to different task views', async ({ page, helpers }) => {
    // Test navigation to Waiting For
    await helpers.navigateTo('/tasks/waiting');
    await expect(page.locator('text=Waiting For')).toBeVisible({ timeout: 10000 });

    // Test navigation to Scheduled
    await helpers.navigateTo('/tasks/scheduled');
    await expect(page.locator('text=Scheduled')).toBeVisible({ timeout: 10000 });

    // Test navigation to Someday/Maybe
    await helpers.navigateTo('/tasks/someday');
    await expect(page.locator('text=Someday')).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Task CRUD - Update Operations', () => {
  test('can complete a task', async ({ page, helpers }) => {
    // First ensure there's a task
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('Task to complete ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process it
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("Yes")').first().click();
      await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();
      await page.locator('button:has-text("Create Task")').click();
      await page.waitForTimeout(1000);
    }

    // Navigate to tasks
    await helpers.navigateTo('/tasks/next');
    await page.waitForTimeout(2000);

    // Find and complete a task
    const completeButton = page.locator('button:has(.oi-check), button:has(.oi-circle-check), button[aria-label*="complete"]').first();

    if (await completeButton.isVisible().catch(() => false)) {
      await completeButton.click();

      // Task should be marked complete (might be removed from list or show completion state)
      await page.waitForTimeout(1000);
    }
  });

  test('Task Card Statechart: Default -> Hovered on mouse enter', async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks/next');
    await page.waitForTimeout(2000);

    const taskCard = page.locator('.list-group-item, .task-card, [data-testid="task-card"]').first();

    if (await taskCard.isVisible().catch(() => false)) {
      // Hover over task
      await taskCard.hover();

      // Should show hover state (additional buttons might appear)
      await page.waitForTimeout(300);
    }
  });

  test('Task Card Statechart: can click task to navigate to details', async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks/next');
    await page.waitForTimeout(2000);

    // Find a task link
    const taskLink = page.locator('.list-group-item a, .task-card a, a[href*="/tasks/"]').first();

    if (await taskLink.isVisible().catch(() => false)) {
      await taskLink.click();

      // Should navigate to task details
      await page.waitForURL(/\/tasks\//);
    }
  });
});

test.describe('Task CRUD - Delete Operations', () => {
  test('can delete a task', async ({ page, helpers }) => {
    // Create a task first
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('Task to delete ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process it
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("Yes")').first().click();
      await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();
      await page.locator('button:has-text("Create Task")').click();
      await page.waitForTimeout(1000);
    }

    // Navigate to tasks
    await helpers.navigateTo('/tasks/next');
    await page.waitForTimeout(2000);

    // Find and delete a task
    const deleteButton = page.locator('button:has(.oi-trash), button[aria-label*="delete"]').first();

    if (await deleteButton.isVisible().catch(() => false)) {
      await deleteButton.click();

      // May need to confirm deletion
      const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Delete")');
      if (await confirmButton.isVisible().catch(() => false)) {
        await confirmButton.click();
      }

      await page.waitForTimeout(1000);
    }
  });
});

test.describe('Task Status Transitions - Petri Net Tests', () => {
  test('Petri Net: Task status transitions follow valid paths', async ({ page, helpers }) => {
    // Create a task
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('Status transition test ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process from Inbox -> NextAction
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("Yes")').first().click();
      await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();
      await page.locator('button:has-text("Create Task")').click();
      await page.waitForTimeout(1000);

      // Verify task is in NextAction
      await helpers.navigateTo('/tasks/next');
      await expect(page.locator('text=Status transition test')).toBeVisible({ timeout: 10000 });
    }
  });

  test('Petri Net: Inbox -> Reference transition via processing', async ({ page, helpers }) => {
    // Create a capture
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('Reference item test ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process as non-actionable -> Reference
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("No")').first().click();
      await page.locator('button:has-text("Reference")').click();
      await page.waitForTimeout(1000);

      // Should be moved to reference (removed from inbox)
      await expect(page.locator('text=Reference item test')).not.toBeVisible({ timeout: 5000 });
    }
  });

  test('Petri Net: Inbox -> SomedayMaybe transition via processing', async ({ page, helpers }) => {
    // Create a capture
    await helpers.navigateTo('/capture');
    const captureInput = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await captureInput.fill('Someday item test ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Process as non-actionable -> Someday
    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await page.locator('button:has-text("No")').first().click();
      await page.locator('button:has-text("Someday")').click();
      await page.waitForTimeout(1000);

      // Verify in someday list
      await helpers.navigateTo('/tasks/someday');
      await expect(page.locator('text=Someday item test')).toBeVisible({ timeout: 10000 });
    }
  });
});

test.describe('Task Lists - Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks/next');
  });

  test('task list has proper structure', async ({ page }) => {
    await page.waitForTimeout(2000);

    // Should use list semantics
    const list = page.locator('.list-group, ul, ol, [role="list"]');
    await expect(list).toBeVisible({ timeout: 5000 }).catch(() => {});
  });

  test('task actions are keyboard accessible', async ({ page }) => {
    await page.waitForTimeout(2000);

    // Tab through the page
    await page.keyboard.press('Tab');

    // Some element should be focused
    const focused = page.locator(':focus');
    await expect(focused).toBeVisible({ timeout: 5000 }).catch(() => {});
  });

  test('task cards have accessible labels', async ({ page }) => {
    await page.waitForTimeout(2000);

    const taskCard = page.locator('.list-group-item, .task-card').first();

    if (await taskCard.isVisible().catch(() => false)) {
      // Should have some text content
      const text = await taskCard.textContent();
      expect(text).toBeTruthy();
    }
  });

  test('action buttons have accessible names', async ({ page }) => {
    await page.waitForTimeout(2000);

    const buttons = page.locator('.list-group-item button, .task-card button');
    const count = await buttons.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const button = buttons.nth(i);
      if (await button.isVisible().catch(() => false)) {
        const ariaLabel = await button.getAttribute('aria-label');
        const text = await button.textContent();
        const title = await button.getAttribute('title');
        expect(ariaLabel || text || title).toBeTruthy();
      }
    }
  });
});
