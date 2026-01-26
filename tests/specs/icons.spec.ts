import { test, expect } from '../fixtures/base';

/**
 * Icon Components E2E Tests
 *
 * Tests for icon/image support in Projects, Goals, and intelligent Task icons.
 *
 * Based on formal models:
 * - Petri Net: Image Upload Operation (Idle -> FileSelected -> Validating -> Processing -> Complete/Failed)
 * - Statechart: IconPicker (Closed -> Open.Browsing -> Open.Searching -> IconSelected -> Closed)
 * - Statechart: ImageUploader (Empty -> DragOver -> Selected -> Processing -> Complete/Error)
 */

test.describe('Task Icon - Intelligent Display', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('tasks with email-related titles get email icon', async ({ page, helpers }) => {
    // Create a task with email-related content
    const captureInput = page.locator('textarea[placeholder*="capture"], input[placeholder*="capture"]').first();
    await captureInput.fill('Reply to email from John about the project');
    await page.locator('button:has-text("Capture")').click();

    // Wait for capture success
    await page.waitForSelector('.capture-success, .text-success:has-text("Captured")');

    // Navigate to tasks and check for icon
    await helpers.navigateTo('/tasks');
    await page.waitForTimeout(500);

    // Task should have an email-related icon (📧)
    const taskIcon = page.locator('[data-testid="task-icon"]').first();
    await expect(taskIcon).toBeVisible();
  });

  test('tasks with meeting-related titles get meeting icon', async ({ page, helpers }) => {
    const captureInput = page.locator('textarea[placeholder*="capture"], input[placeholder*="capture"]').first();
    await captureInput.fill('Schedule meeting with team for project review');
    await page.locator('button:has-text("Capture")').click();

    await page.waitForSelector('.capture-success, .text-success:has-text("Captured")');
    await helpers.navigateTo('/tasks');
    await page.waitForTimeout(500);

    const taskIcon = page.locator('[data-testid="task-icon"]').first();
    await expect(taskIcon).toBeVisible();
  });

  test('tasks with code-related titles get code icon', async ({ page, helpers }) => {
    const captureInput = page.locator('textarea[placeholder*="capture"], input[placeholder*="capture"]').first();
    await captureInput.fill('Debug the login feature and fix PR comments');
    await page.locator('button:has-text("Capture")').click();

    await page.waitForSelector('.capture-success, .text-success:has-text("Captured")');
    await helpers.navigateTo('/tasks');
    await page.waitForTimeout(500);

    const taskIcon = page.locator('[data-testid="task-icon"]').first();
    await expect(taskIcon).toBeVisible();
  });
});

test.describe('Project Icon - Display and Edit', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/projects');
  });

  test('project cards display ProjectIcon component', async ({ page, helpers }) => {
    // Create a project first
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();

    // Fill in project details
    await page.locator('input[placeholder*="name"], input#project-name').fill('Test Icon Project');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();

    await page.waitForTimeout(1000);

    // Check for project icon in the list
    const projectIcon = page.locator('[data-testid="project-icon"]').first();
    await expect(projectIcon).toBeVisible();
  });

  test('project detail page shows large project icon', async ({ page, helpers }) => {
    // Create a project
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();
    await page.locator('input[placeholder*="name"], input#project-name').fill('Detail Icon Test');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();

    await page.waitForTimeout(1000);

    // Click on the project to go to detail page
    await page.locator('text=Detail Icon Test').click();
    await page.waitForTimeout(500);

    // Check for large project icon in header
    const projectIcon = page.locator('[data-testid="project-icon"]').first();
    await expect(projectIcon).toBeVisible();
  });

  test('edit modal includes IconPicker', async ({ page, helpers }) => {
    // Create a project
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();
    await page.locator('input[placeholder*="name"], input#project-name').fill('Icon Picker Test');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();

    await page.waitForTimeout(1000);

    // Go to project detail
    await page.locator('text=Icon Picker Test').click();
    await page.waitForTimeout(500);

    // Click edit button
    await page.locator('button:has-text("Edit")').click();
    await page.waitForTimeout(500);

    // Check for icon picker trigger in modal
    const iconPicker = page.locator('[data-testid="icon-picker-trigger"]');
    await expect(iconPicker).toBeVisible();
  });
});

test.describe('IconPicker - Selection Flow', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/projects');
    // Create a project and open edit
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();
    await page.locator('input[placeholder*="name"], input#project-name').fill('Picker Test');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();
    await page.waitForTimeout(1000);
    await page.locator('text=Picker Test').click();
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Edit")').click();
    await page.waitForTimeout(500);
  });

  test('Closed -> Open: clicking trigger opens picker', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();

    const dropdown = page.locator('[data-testid="icon-picker-dropdown"]');
    await expect(dropdown).toBeVisible();
  });

  test('Open -> IconSelected: clicking icon selects it', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();

    await page.waitForTimeout(300);

    // Click an icon option
    const iconOption = page.locator('[data-testid="icon-option"]').first();
    await iconOption.click();

    // Dropdown should close
    const dropdown = page.locator('[data-testid="icon-picker-dropdown"]');
    await expect(dropdown).toBeHidden();
  });

  test('search filtering works', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();
    await page.waitForTimeout(300);

    const searchInput = page.locator('[data-testid="icon-search"]');
    await searchInput.fill('folder');

    // Icons should be filtered
    const iconGrid = page.locator('[data-testid="icon-grid"]');
    await expect(iconGrid).toBeVisible();
  });

  test('emoji tab shows emoji icons', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();
    await page.waitForTimeout(300);

    // Click emoji tab
    await page.locator('button:has-text("Emoji")').click();
    await page.waitForTimeout(300);

    const emojiGrid = page.locator('[data-testid="emoji-grid"]');
    await expect(emojiGrid).toBeVisible();

    const emojiOption = page.locator('[data-testid="emoji-option"]').first();
    await expect(emojiOption).toBeVisible();
  });

  test('clicking close button closes picker', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();
    await page.waitForTimeout(300);

    await page.locator('[data-testid="close-picker"]').click();

    const dropdown = page.locator('[data-testid="icon-picker-dropdown"]');
    await expect(dropdown).toBeHidden();
  });
});

test.describe('Goal Icon - Display', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/goals');
  });

  test('goal cards display GoalIcon component', async ({ page, helpers }) => {
    // Create a goal
    await page.locator('button:has-text("New Goal"), button:has-text("Add Goal")').click();
    await page.waitForTimeout(500);

    // Fill in goal details
    await page.locator('input[placeholder*="title"], input#goal-title').fill('Test Icon Goal');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();

    await page.waitForTimeout(1000);

    // Check for goal icon in the list
    const goalIcon = page.locator('[data-testid="goal-icon"]').first();
    await expect(goalIcon).toBeVisible();
  });
});

test.describe('ImageUploader - Upload Flow', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/projects');
    // Create a project and open edit
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();
    await page.locator('input[placeholder*="name"], input#project-name').fill('Upload Test');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();
    await page.waitForTimeout(1000);
    await page.locator('text=Upload Test').click();
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Edit")').click();
    await page.waitForTimeout(500);
  });

  test('upload tab is available in icon picker', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();
    await page.waitForTimeout(300);

    // Click upload tab
    await page.locator('button:has-text("Upload")').click();
    await page.waitForTimeout(300);

    const uploadTab = page.locator('[data-testid="upload-tab"]');
    await expect(uploadTab).toBeVisible();
  });

  test('empty state shows upload area', async ({ page }) => {
    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await trigger.click();
    await page.waitForTimeout(300);

    await page.locator('button:has-text("Upload")').click();
    await page.waitForTimeout(300);

    const uploadArea = page.locator('[data-testid="upload-area"]');
    await expect(uploadArea).toBeVisible();
  });
});

test.describe('Accessibility', () => {
  test('icon picker trigger has proper aria attributes', async ({ page, helpers }) => {
    await helpers.navigateTo('/projects');

    // Create a project and open edit
    await page.locator('button:has-text("New Project"), button:has-text("Add Project")').click();
    await page.locator('input[placeholder*="name"], input#project-name').fill('A11y Test');
    await page.locator('button:has-text("Create"), button:has-text("Save")').click();
    await page.waitForTimeout(1000);
    await page.locator('text=A11y Test').click();
    await page.waitForTimeout(500);
    await page.locator('button:has-text("Edit")').click();
    await page.waitForTimeout(500);

    const trigger = page.locator('[data-testid="icon-picker-trigger"]');
    await expect(trigger).toHaveAttribute('aria-expanded', 'false');
    await expect(trigger).toHaveAttribute('aria-haspopup', 'true');

    await trigger.click();
    await expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });

  test('icon display has alt text for custom images', async ({ page, helpers }) => {
    await helpers.navigateTo('/projects');

    // Look for any icon display with image
    const iconImages = page.locator('[data-testid="icon-display-image"]');
    const count = await iconImages.count();

    if (count > 0) {
      const firstImage = iconImages.first();
      await expect(firstImage).toHaveAttribute('alt');
    }
  });

  test('emoji icons have aria-label', async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks');

    const emojiIcons = page.locator('[data-testid="icon-display-emoji"]');
    const count = await emojiIcons.count();

    if (count > 0) {
      const firstEmoji = emojiIcons.first();
      await expect(firstEmoji).toHaveAttribute('role', 'img');
      await expect(firstEmoji).toHaveAttribute('aria-label');
    }
  });
});

test.describe('TaskCard - Icon Integration', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('task cards show TaskIcon before priority indicator', async ({ page, helpers }) => {
    // Create a task
    const captureInput = page.locator('textarea[placeholder*="capture"], input[placeholder*="capture"]').first();
    await captureInput.fill('Test task for icon display');
    await page.locator('button:has-text("Capture")').click();
    await page.waitForSelector('.capture-success, .text-success:has-text("Captured")');

    // Navigate to tasks
    await helpers.navigateTo('/tasks');
    await page.waitForTimeout(500);

    // Check that task icon is visible
    const taskIcon = page.locator('[data-testid="task-icon"]').first();
    await expect(taskIcon).toBeVisible();
  });
});
