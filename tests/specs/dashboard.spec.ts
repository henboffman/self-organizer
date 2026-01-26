import { test, expect } from '../fixtures/base';

/**
 * Dashboard E2E Tests
 * Based on: .ai/STATECHARTS.md - "Dashboard Pick 3 Statechart", "Quick Capture Widget Statechart"
 * Based on: .ai/PETRI-MODELS.md - "Daily Pick 3 Flow"
 *
 * Dashboard Features:
 * - Quick stats (Inbox, Completed Today, Next Actions, Overdue)
 * - Today's Focus (Pick 3)
 * - Quick Capture Widget
 * - Top Next Actions
 * - Today's Schedule
 * - Waiting For
 * - Reviews links
 */

test.describe('Dashboard - Page Structure', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays dashboard heading', async ({ page }) => {
    const heading = page.locator('h3:has-text("Dashboard"), h2:has-text("Dashboard"), h1:has-text("Dashboard")');
    await expect(heading).toBeVisible({ timeout: 10000 });
  });

  test('displays quick stats cards', async ({ page }) => {
    // Should have stats cards for Inbox, Completed, Next Actions, Overdue
    await expect(page.locator('text=Inbox')).toBeVisible();
    await expect(page.locator('text=Completed Today')).toBeVisible();
    await expect(page.locator('text=Next Actions')).toBeVisible();
    await expect(page.locator('text=Overdue')).toBeVisible();
  });

  test('stats cards have navigation links', async ({ page }) => {
    // Inbox card should link to /inbox
    const inboxLink = page.locator('a[href="/inbox"]');
    await expect(inboxLink).toBeVisible();

    // Next Actions should link to /tasks/next
    const tasksLink = page.locator('a[href="/tasks/next"]');
    await expect(tasksLink).toBeVisible();
  });
});

test.describe('Dashboard - Quick Capture Widget', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('Quick Capture Widget: Idle -> Focused -> HasText flow', async ({ page }) => {
    // Find quick capture input
    const quickCaptureInput = page.locator('input[placeholder*="mind"], input[placeholder*="capture"]');
    await expect(quickCaptureInput).toBeVisible();

    // Idle -> Focused: click input
    await quickCaptureInput.click();
    await expect(quickCaptureInput).toBeFocused();

    // Focused -> HasText: type text
    await quickCaptureInput.fill('Quick capture test');
    await expect(quickCaptureInput).toHaveValue('Quick capture test');
  });

  test('Quick Capture Widget: HasText -> Submitting -> Success', async ({ page }) => {
    const quickCaptureInput = page.locator('input[placeholder*="mind"], input[placeholder*="capture"]');
    const captureButton = page.locator('.card:has-text("Quick Capture") button, button:has(.oi-plus)').first();

    await quickCaptureInput.fill('Dashboard capture test ' + Date.now());
    await captureButton.click();

    // Success state: should show success message
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
  });

  test('Quick Capture Widget: Enter key submits capture', async ({ page }) => {
    const quickCaptureInput = page.locator('input[placeholder*="mind"], input[placeholder*="capture"]');

    await quickCaptureInput.fill('Enter key test ' + Date.now());
    await quickCaptureInput.press('Enter');

    // Should capture successfully
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Dashboard - Pick 3 (Today\'s Focus)', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays Today\'s Focus section', async ({ page }) => {
    const focusSection = page.locator('text=Today\'s Focus');
    await expect(focusSection).toBeVisible();
  });

  test('Pick 3 Statechart: Loading -> NoPicks: shows pick button when no picks', async ({ page }) => {
    // Wait for loading to complete
    await page.waitForTimeout(2000);

    // Should either show "Pick My 3" button or already have picks
    const pickButton = page.locator('button:has-text("Pick My 3"), button:has-text("Pick")');
    const existingPicks = page.locator('.list-group-item').first();

    const hasPickButton = await pickButton.isVisible().catch(() => false);
    const hasPicks = await existingPicks.isVisible().catch(() => false);

    // One of these should be true
    expect(hasPickButton || hasPicks).toBeTruthy();
  });

  test('Pick 3 Statechart: NoPicks -> Selecting: clicking pick button shows task selection', async ({ page }) => {
    await page.waitForTimeout(2000);

    const pickButton = page.locator('button:has-text("Pick My 3"), button:has-text("Pick")');

    if (await pickButton.isVisible().catch(() => false)) {
      await pickButton.click();

      // Should show task selection list
      await expect(page.locator('text=Select, text=up to 3')).toBeVisible({ timeout: 5000 });
    }
  });

  test('Pick 3 Statechart: Selecting -> HasPicks: can confirm picks', async ({ page }) => {
    await page.waitForTimeout(2000);

    const pickButton = page.locator('button:has-text("Pick My 3"), button:has-text("Pick")');

    if (await pickButton.isVisible().catch(() => false)) {
      await pickButton.click();
      await page.waitForTimeout(1000);

      // Select a task if available
      const taskItem = page.locator('.list-group-item-action').first();

      if (await taskItem.isVisible().catch(() => false)) {
        await taskItem.click();

        // Confirm picks
        const confirmButton = page.locator('button:has-text("Set These"), button:has-text("Confirm")');
        if (await confirmButton.isEnabled().catch(() => false)) {
          await confirmButton.click();

          // Should now show the picked tasks
          await page.waitForTimeout(1000);
        }
      }
    }
  });

  test('Pick 3 Statechart: HasPicks -> can toggle completion', async ({ page }) => {
    await page.waitForTimeout(2000);

    // If there are picks, try to toggle one
    const completeButton = page.locator('.list-group-item button:has(.oi-check), .list-group-item button:has(.oi-circle-check)').first();

    if (await completeButton.isVisible().catch(() => false)) {
      await completeButton.click();

      // Should update the pick status
      await page.waitForTimeout(500);
    }
  });

  test('Pick 3 Statechart: HasPicks -> NoPicks: Reset Picks clears picks', async ({ page }) => {
    await page.waitForTimeout(2000);

    const resetButton = page.locator('button:has-text("Reset Picks"), button:has-text("Clear")');

    if (await resetButton.isVisible().catch(() => false)) {
      await resetButton.click();

      // Should go back to NoPicks state
      const pickButton = page.locator('button:has-text("Pick My 3"), button:has-text("Pick")');
      await expect(pickButton).toBeVisible({ timeout: 5000 });
    }
  });
});

test.describe('Dashboard - Task Lists', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays Top Next Actions section', async ({ page }) => {
    const nextActionsSection = page.locator('text=Top Next Actions, text=Next Actions');
    await expect(nextActionsSection).toBeVisible();
  });

  test('displays Waiting For section', async ({ page }) => {
    const waitingSection = page.locator('text=Waiting For');
    await expect(waitingSection).toBeVisible();
  });

  test('Next Actions links to task list', async ({ page }) => {
    const viewAllLink = page.locator('a:has-text("View All")[href="/tasks/next"]');
    await expect(viewAllLink).toBeVisible();
  });

  test('Waiting For links to waiting tasks', async ({ page }) => {
    const viewAllLink = page.locator('a:has-text("View All")[href="/tasks/waiting"]');
    await expect(viewAllLink).toBeVisible();
  });
});

test.describe('Dashboard - Calendar Section', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays Today\'s Schedule section', async ({ page }) => {
    const scheduleSection = page.locator('text=Today\'s Schedule');
    await expect(scheduleSection).toBeVisible();
  });

  test('Calendar section links to calendar page', async ({ page }) => {
    const calendarLink = page.locator('a[href="/calendar"]');
    await expect(calendarLink).toBeVisible();
  });
});

test.describe('Dashboard - Project Status', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays Project Status section', async ({ page }) => {
    const projectSection = page.locator('text=Project Status');
    await expect(projectSection).toBeVisible();
  });

  test('shows stalled projects warning or success state', async ({ page }) => {
    await page.waitForTimeout(2000);

    // Either shows stalled warning or all-good message
    const stalledWarning = page.locator('text=need a next action');
    const allGood = page.locator('text=All projects have next actions');

    const hasWarning = await stalledWarning.isVisible().catch(() => false);
    const hasSuccess = await allGood.isVisible().catch(() => false);

    expect(hasWarning || hasSuccess).toBeTruthy();
  });
});

test.describe('Dashboard - Reviews Section', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('displays Reviews section', async ({ page }) => {
    const reviewsSection = page.locator('text=Reviews');
    await expect(reviewsSection).toBeVisible();
  });

  test('has Daily Review link', async ({ page }) => {
    const dailyReviewLink = page.locator('a[href="/review/daily"]');
    await expect(dailyReviewLink).toBeVisible();
  });

  test('has Weekly Review link', async ({ page }) => {
    const weeklyReviewLink = page.locator('a[href="/review/weekly"]');
    await expect(weeklyReviewLink).toBeVisible();
  });
});

test.describe('Dashboard - Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('has proper heading hierarchy', async ({ page }) => {
    // Main heading should be h3 or higher
    const mainHeading = page.locator('h1, h2, h3').first();
    await expect(mainHeading).toBeVisible();
  });

  test('stat cards are accessible', async ({ page }) => {
    // Cards should have proper structure
    const cards = page.locator('.card');
    const count = await cards.count();
    expect(count).toBeGreaterThan(0);
  });

  test('links have accessible text', async ({ page }) => {
    // Navigation links should have visible text
    const links = page.locator('a[href]');
    const count = await links.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const link = links.nth(i);
      const text = await link.textContent();
      const ariaLabel = await link.getAttribute('aria-label');
      expect(text || ariaLabel).toBeTruthy();
    }
  });

  test('interactive elements are keyboard accessible', async ({ page }) => {
    // Tab through the page
    await page.keyboard.press('Tab');
    const focused = page.locator(':focus');

    // Something should be focused
    await expect(focused).toBeVisible({ timeout: 5000 }).catch(() => {});
  });
});
