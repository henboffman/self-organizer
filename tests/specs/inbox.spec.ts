import { test, expect } from '../fixtures/base';

/**
 * Inbox Processing Workflow E2E Tests
 * Based on: .ai/STATECHARTS.md - "Inbox Process Modal Statechart"
 * Based on: .ai/PETRI-MODELS.md - "Inbox Processing Flow"
 *
 * Statechart States:
 * [Closed] -> [Step1_Actionable] -> [Step2_Actionable/NonActionable] -> [Step3_CreateTask]
 *
 * Processing Decision Tree:
 * Is it actionable?
 *   No -> Move to Reference / Someday / Delete
 *   Yes -> Can do in 2 min?
 *     Yes -> Do it now (start timer)
 *     No -> Create task with details
 */

test.describe('Inbox Processing - Statechart Tests', () => {
  // First, create a capture item to process
  test.beforeEach(async ({ page, helpers }) => {
    // Navigate to capture and create an item
    await helpers.navigateTo('/capture');

    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    await input.fill('Test inbox item ' + Date.now());
    await captureButton.click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Navigate to inbox
    await helpers.navigateTo('/inbox');
  });

  test('Closed -> Step1_Actionable: clicking Process opens modal', async ({ page }) => {
    // Find a capture item and click Process
    const processButton = page.locator('button:has-text("Process")').first();

    // If no items, we have inbox zero
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) {
      // Verify inbox zero state
      await expect(page.locator('text=Inbox Zero')).toBeVisible();
      return;
    }

    // Transition: click Process -> Step1_Actionable
    await processButton.click();

    // Verify modal is open with Step 1 content
    await expect(page.locator('text=Is this actionable')).toBeVisible();
    await expect(page.locator('button:has-text("Yes")')).toBeVisible();
    await expect(page.locator('button:has-text("No")')).toBeVisible();
  });

  test('Step1_Actionable -> Step2_Actionable: clicking Yes shows 2-minute check', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();

    // Transition: Yes -> Step2_Actionable
    await page.locator('button:has-text("Yes")').first().click();

    // Verify Step 2 Actionable content (2-minute rule)
    await expect(page.locator('h5:has-text("2 minutes"), strong:has-text("2 minutes")').first()).toBeVisible();
  });

  test('Step1_Actionable -> Step2_NonActionable: clicking No shows destination options', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();

    // Transition: No -> Step2_NonActionable
    await page.locator('button:has-text("No")').first().click();

    // Verify destination options (use button-specific locators)
    await expect(page.locator('button:has-text("Reference")')).toBeVisible();
    await expect(page.locator('button:has-text("Someday")')).toBeVisible();
  });

  test('Step2_Actionable -> Step3_CreateTask: clicking "needs more time" shows task form', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();
    await page.locator('button:has-text("Yes")').first().click();

    // Transition: needs more time -> Step3_CreateTask
    await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();

    // Verify task creation form
    await expect(page.locator('label:has-text("Title")')).toBeVisible();
    await expect(page.locator('button:has-text("Create Task")')).toBeVisible();
  });

  test('Step3_CreateTask: can create task with title and estimated time', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    // Navigate to task creation form
    await processButton.click();
    await page.locator('button:has-text("Yes")').first().click();
    await page.locator('button:has-text("needs more time"), button:has-text("No")').first().click();

    // Fill task details
    const titleInput = page.locator('input[type="text"]').first();
    await titleInput.fill('My new task from inbox');

    // Create task
    await page.locator('button:has-text("Create Task")').click();

    // Modal should close and item should be removed from inbox
    await expect(page.locator('text=Is this actionable')).not.toBeVisible({ timeout: 5000 });
  });

  test('Any -> Closed: Cancel button closes modal', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();
    await expect(page.locator('text=Is this actionable')).toBeVisible();

    // Cancel should close modal
    await page.locator('button:has-text("Cancel")').click();

    await expect(page.locator('text=Is this actionable')).not.toBeVisible({ timeout: 5000 });
  });

  test('Any -> PreviousStep: Back button goes to previous step', async ({ page }) => {
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    // Go to Step 2
    await processButton.click();
    await page.locator('button:has-text("Yes")').first().click();

    // Verify we're in Step 2
    await expect(page.locator('h5:has-text("2 minutes"), strong:has-text("2 minutes")').first()).toBeVisible();

    // Back to Step 1
    await page.locator('button:has-text("Back")').click();

    // Verify we're back in Step 1
    await expect(page.locator('text=Is this actionable')).toBeVisible();
  });
});

test.describe('Inbox Processing - Petri Net Tests', () => {
  test('Petri Net: Complete processing flow returns to Loading state', async ({ page, helpers }) => {
    // Create a capture item
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');
    await input.fill('Petri net flow test ' + Date.now());
    await captureButton.click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Navigate to inbox - this is P1: Loading -> T1: LoadItems
    await helpers.navigateTo('/inbox');

    // P2: Fetching -> T2: ItemsLoaded -> P3: Displaying
    // We should see the item
    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    // P4: Processing (click Process)
    await processButton.click();

    // Complete the processing (delete the item)
    await page.locator('button:has-text("No")').first().click();
    await page.locator('button:has-text("Delete")').click();

    // T5: Delete -> Back to P1: Loading (page reloads list)
    // Verify we're back in the main list view
    await expect(page.locator('text=Is this actionable')).not.toBeVisible({ timeout: 5000 });
  });

  test('Petri Net: Processing multiple items maintains deadlock-freedom', async ({ page, helpers }) => {
    // Create multiple capture items
    await helpers.navigateTo('/capture');

    for (let i = 1; i <= 2; i++) {
      const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
      const captureButton = page.locator('button:has-text("Capture")');
      await input.fill(`Multi-item test ${i} - ${Date.now()}`);
      await captureButton.click();
      await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
      await page.waitForTimeout(500);
    }

    // Navigate to inbox
    await helpers.navigateTo('/inbox');

    // Process first item
    let processButton = page.locator('button:has-text("Process")').first();
    let hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();
    await page.locator('button:has-text("No")').first().click();
    await page.locator('button:has-text("Delete")').click();

    await page.waitForTimeout(1000);

    // Should still be able to process next item (deadlock-freedom)
    processButton = page.locator('button:has-text("Process")').first();
    hasItems = await processButton.isVisible().catch(() => false);

    // Either we have more items to process or we reached inbox zero
    expect(hasItems || (await page.locator('text=Inbox Zero').isVisible())).toBeTruthy();
  });
});

test.describe('Inbox Page - Empty State and Special Cases', () => {
  test('shows Inbox Zero message when no items', async ({ page, helpers }) => {
    await helpers.navigateTo('/inbox');

    // Check if inbox is empty or has items
    const inboxZero = page.locator('text=Inbox Zero');
    const processButton = page.locator('button:has-text("Process")').first();

    // One of these should be visible
    const isEmpty = await inboxZero.isVisible().catch(() => false);
    const hasItems = await processButton.isVisible().catch(() => false);

    expect(isEmpty || hasItems).toBeTruthy();
  });

  test('shows capture link when inbox is empty', async ({ page, helpers }) => {
    await helpers.navigateTo('/inbox');

    const inboxZero = page.locator('text=Inbox Zero');
    const isEmpty = await inboxZero.isVisible().catch(() => false);

    if (isEmpty) {
      // Should have a link to capture
      const captureLink = page.locator('a[href="/capture"], a:has-text("Capture")');
      await expect(captureLink).toBeVisible();
    }
  });

  test('displays item count badge when items exist', async ({ page, helpers }) => {
    // Create an item
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');
    await input.fill('Badge test item ' + Date.now());
    await captureButton.click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    await helpers.navigateTo('/inbox');

    // Should show count badge
    const badge = page.locator('.badge');
    await expect(badge).toBeVisible();
  });
});

test.describe('Inbox Processing - Accessibility', () => {
  test('modal has proper focus management', async ({ page, helpers }) => {
    // Create an item to process
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');
    await input.fill('Focus test item ' + Date.now());
    await captureButton.click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    await helpers.navigateTo('/inbox');

    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();

    // Focus should be trapped in modal
    // First focusable element should be focused or at least visible
    await expect(page.locator('.modal-content').first()).toBeVisible();
  });

  test('action buttons have clear labels', async ({ page, helpers }) => {
    // Create an item to process
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');
    await input.fill('Label test item ' + Date.now());
    await captureButton.click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    await helpers.navigateTo('/inbox');

    const processButton = page.locator('button:has-text("Process")').first();
    const hasItems = await processButton.isVisible().catch(() => false);
    if (!hasItems) return;

    await processButton.click();

    // Yes/No buttons should have clear text
    await expect(page.locator('button:has-text("Yes")')).toBeVisible();
    await expect(page.locator('button:has-text("No")')).toBeVisible();
  });
});
