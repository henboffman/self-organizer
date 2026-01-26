import { test, expect } from '../fixtures/base';

/**
 * Onboarding Wizard E2E Tests
 * Tests the new user onboarding experience
 *
 * The onboarding wizard has 9 steps:
 * 0: Theme Selection
 * 1: Accessibility Settings
 * 2: Welcome
 * 3: App Mode Selection
 * 4: Mode Preview
 * 5: Capture Flow Explanation
 * 6: Calendar Provider
 * 7: Balance Areas Selection
 * 8: Get Started
 */

test.describe('Onboarding Wizard - Fresh User Experience', () => {
  test.beforeEach(async ({ page }) => {
    // Clear IndexedDB to simulate a fresh user
    await page.goto('/');
    await page.evaluate(async () => {
      const databases = await indexedDB.databases();
      for (const db of databases) {
        if (db.name) {
          indexedDB.deleteDatabase(db.name);
        }
      }
    });
  });

  test('shows onboarding wizard on first visit', async ({ page }) => {
    // Reload to trigger fresh state
    await page.reload();

    // Wait for Blazor to load
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    // Check if onboarding is visible
    const onboardingOverlay = page.locator('.onboarding-overlay');
    await expect(onboardingOverlay).toBeVisible({ timeout: 30000 });
  });

  test('Step 0: Theme Selection is shown first', async ({ page }) => {
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    const themeStep = page.locator('h2:has-text("Choose Your Theme")');
    await expect(themeStep).toBeVisible({ timeout: 30000 });

    // Should show theme options
    await expect(page.locator('text=Light Mode')).toBeVisible();
    await expect(page.locator('text=Dark Mode')).toBeVisible();
    await expect(page.locator('text=Match System')).toBeVisible();
  });

  test('can select light theme', async ({ page }) => {
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    const lightTheme = page.locator('button.theme-option:has-text("Light Mode")');
    await lightTheme.click();

    // Should show selected state
    await expect(lightTheme).toHaveClass(/selected/);
  });

  test('can navigate through all steps with Next button', async ({ page }) => {
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    // Step 0: Theme
    await expect(page.locator('h2:has-text("Choose Your Theme")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 1: Accessibility
    await expect(page.locator('h2:has-text("Reading Preferences")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 2: Welcome
    await expect(page.locator('h2:has-text("Welcome to Self Organizer")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 3: App Mode
    await expect(page.locator('h2:has-text("How Will You Use")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 4: Preview
    await expect(page.locator('h2:has-text("Setup")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 5: Capture
    await expect(page.locator('h2:has-text("Capture Everything")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 6: Calendar
    await expect(page.locator('h2:has-text("Calendar Integration")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 7: Balance
    await expect(page.locator('h2:has-text("Track Your Balance")')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Step 8: Get Started
    await expect(page.locator('h2:has-text("You\'re All Set")')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('button:has-text("Get Started")')).toBeVisible();
  });

  test('Get Started button completes onboarding', async ({ page }) => {
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    // Navigate through all steps
    for (let i = 0; i < 8; i++) {
      const nextButton = page.locator('button:has-text("Next")');
      if (await nextButton.isVisible().catch(() => false)) {
        await nextButton.click();
        await page.waitForTimeout(500);
      }
    }

    // Click Get Started
    await page.locator('button:has-text("Get Started")').click();
    await page.waitForTimeout(2000);

    // Onboarding should be closed
    const onboardingOverlay = page.locator('.onboarding-overlay');
    await expect(onboardingOverlay).not.toBeVisible({ timeout: 10000 });

    // Should see the dashboard
    await expect(page.locator('h3:has-text("Dashboard")')).toBeVisible({ timeout: 10000 });
  });

  test('Back button navigates to previous step', async ({ page }) => {
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    // Go to step 1
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);
    await expect(page.locator('h2:has-text("Reading Preferences")')).toBeVisible({ timeout: 10000 });

    // Go back to step 0
    await page.locator('button:has-text("Back")').click();
    await page.waitForTimeout(500);
    await expect(page.locator('h2:has-text("Choose Your Theme")')).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Onboarding Wizard - App Mode Selection', () => {
  test.beforeEach(async ({ page }) => {
    // Clear and reload
    await page.goto('/');
    await page.evaluate(async () => {
      const databases = await indexedDB.databases();
      for (const db of databases) {
        if (db.name) {
          indexedDB.deleteDatabase(db.name);
        }
      }
    });
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);

    // Navigate to App Mode step (step 3)
    for (let i = 0; i < 3; i++) {
      await page.locator('button:has-text("Next")').click();
      await page.waitForTimeout(500);
    }
  });

  test('shows three app mode options', async ({ page }) => {
    await expect(page.locator('text=Work Mode')).toBeVisible();
    await expect(page.locator('text=Life Mode')).toBeVisible();
    await expect(page.locator('text=Balanced Mode')).toBeVisible();
  });

  test('can select Work mode', async ({ page }) => {
    const workMode = page.locator('button.mode-card:has-text("Work Mode")');
    await workMode.click();
    await expect(workMode).toHaveClass(/selected/);
  });

  test('can select Life mode', async ({ page }) => {
    const lifeMode = page.locator('button.mode-card:has-text("Life Mode")');
    await lifeMode.click();
    await expect(lifeMode).toHaveClass(/selected/);
  });

  test('Balanced mode is default selected', async ({ page }) => {
    const balancedMode = page.locator('button.mode-card:has-text("Balanced Mode")');
    await expect(balancedMode).toHaveClass(/selected/);
  });
});

test.describe('Onboarding Wizard - Accessibility', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.evaluate(async () => {
      const databases = await indexedDB.databases();
      for (const db of databases) {
        if (db.name) {
          indexedDB.deleteDatabase(db.name);
        }
      }
    });
    await page.reload();
    await page.waitForLoadState('domcontentloaded');
    await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(2000);
  });

  test('wizard has proper heading structure', async ({ page }) => {
    const heading = page.locator('.onboarding-wizard h2');
    await expect(heading).toBeVisible();
  });

  test('progress steps are clickable for completed steps', async ({ page }) => {
    // Go to step 1
    await page.locator('button:has-text("Next")').click();
    await page.waitForTimeout(500);

    // Click on step 0 to go back
    const stepDot = page.locator('.progress-step').first();
    await stepDot.click();
    await page.waitForTimeout(500);

    // Should be back at step 0
    await expect(page.locator('h2:has-text("Choose Your Theme")')).toBeVisible();
  });

  test('buttons have accessible text', async ({ page }) => {
    // Next button
    const nextButton = page.locator('button:has-text("Next")');
    await expect(nextButton).toBeVisible();

    // Theme options have text
    await expect(page.locator('button.theme-option:has-text("Light Mode")')).toBeVisible();
    await expect(page.locator('button.theme-option:has-text("Dark Mode")')).toBeVisible();
  });
});
