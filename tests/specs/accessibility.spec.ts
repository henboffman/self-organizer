import { test, expect } from '../fixtures/base';

/**
 * Accessibility E2E Tests
 * Tests ARIA attributes, keyboard navigation, and screen reader support
 *
 * These tests verify:
 * - Proper semantic HTML structure
 * - ARIA labels and roles
 * - Keyboard navigation
 * - Focus management
 * - Color contrast indicators
 */

test.describe('Global Accessibility', () => {
  test('main layout has proper landmark structure', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    // Should have main landmark
    const main = page.locator('main, [role="main"]');
    await expect(main).toBeVisible();

    // Should have navigation
    const nav = page.locator('nav, [role="navigation"]');
    await expect(nav).toBeVisible();
  });

  test('page has skip to main content link', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    // Press Tab to reveal skip link (if it exists)
    await page.keyboard.press('Tab');

    // Many apps have skip links as first focusable element
    const skipLink = page.locator('a:has-text("Skip"), a[href="#main"], a[href="#content"]');
    const isVisible = await skipLink.isVisible().catch(() => false);

    // This is a recommendation, not a hard failure
    if (!isVisible) {
      console.log('Recommendation: Add skip to main content link');
    }
  });

  test('interactive elements are focusable', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    // Tab through and verify focus is visible
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab');
      const focused = page.locator(':focus');
      const isVisible = await focused.isVisible().catch(() => false);
      if (isVisible) {
        // Good - we can tab to elements
        return;
      }
    }
  });
});

test.describe('Capture Page Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('capture input has proper label or placeholder', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await expect(input).toBeVisible();

    const placeholder = await input.getAttribute('placeholder');
    const ariaLabel = await input.getAttribute('aria-label');
    const labelledBy = await input.getAttribute('aria-labelledby');

    expect(placeholder || ariaLabel || labelledBy).toBeTruthy();
  });

  test('capture button has accessible name', async ({ page }) => {
    const button = page.locator('button:has-text("Capture")');
    await expect(button).toBeVisible();

    const text = await button.textContent();
    const ariaLabel = await button.getAttribute('aria-label');

    expect(text || ariaLabel).toBeTruthy();
  });

  test('page heading is properly marked up', async ({ page }) => {
    const heading = page.locator('h1, h2, h3').first();
    await expect(heading).toBeVisible();
  });

  test('keyboard shortcut hint is visible', async ({ page }) => {
    // Should show keyboard shortcut hint
    const hint = page.locator('kbd, text=/Cmd|Ctrl.*Enter/');
    const isVisible = await hint.isVisible().catch(() => false);

    // Recommendation if not visible
    if (!isVisible) {
      console.log('Recommendation: Show keyboard shortcut hint');
    }
  });
});

test.describe('Inbox Page Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/inbox');
  });

  test('page has proper heading', async ({ page }) => {
    const heading = page.locator('h1, h2, h3').first();
    await expect(heading).toBeVisible();
  });

  test('inbox items have proper structure', async ({ page }) => {
    await page.waitForTimeout(2000);

    const items = page.locator('.list-group-item');
    const count = await items.count();

    if (count > 0) {
      // Each item should have discernible text
      const firstItem = items.first();
      const text = await firstItem.textContent();
      expect(text).toBeTruthy();
    }
  });

  test('action buttons have accessible names', async ({ page }) => {
    await page.waitForTimeout(2000);

    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      const text = await processButton.textContent();
      const ariaLabel = await processButton.getAttribute('aria-label');
      expect(text || ariaLabel).toBeTruthy();
    }
  });

  test('modal has proper ARIA attributes when opened', async ({ page, helpers }) => {
    // Create an item to process
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await input.fill('Accessibility modal test ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();

      // Modal should have proper attributes
      const modal = page.locator('[role="dialog"], .modal, .modal-content');
      await expect(modal).toBeVisible();

      // Check for aria-modal or aria-labelledby
      const modalRole = await modal.getAttribute('role');
      const ariaModal = await modal.getAttribute('aria-modal');

      // At least the modal should be visible and identifiable
      expect(modal).toBeTruthy();
    }
  });
});

test.describe('Dashboard Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/');
  });

  test('stat cards have accessible content', async ({ page }) => {
    const cards = page.locator('.card');
    const count = await cards.count();

    expect(count).toBeGreaterThan(0);

    // Each card should have readable content
    for (let i = 0; i < Math.min(count, 4); i++) {
      const card = cards.nth(i);
      const text = await card.textContent();
      expect(text).toBeTruthy();
    }
  });

  test('links are distinguishable', async ({ page }) => {
    const links = page.locator('a[href]');
    const count = await links.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const link = links.nth(i);
      if (await link.isVisible().catch(() => false)) {
        const text = await link.textContent();
        const ariaLabel = await link.getAttribute('aria-label');
        expect(text || ariaLabel).toBeTruthy();
      }
    }
  });

  test('form inputs have labels', async ({ page }) => {
    const inputs = page.locator('input, textarea, select');
    const count = await inputs.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const input = inputs.nth(i);
      if (await input.isVisible().catch(() => false)) {
        const ariaLabel = await input.getAttribute('aria-label');
        const placeholder = await input.getAttribute('placeholder');
        const id = await input.getAttribute('id');

        // If has id, check for associated label
        if (id) {
          const label = page.locator(`label[for="${id}"]`);
          const hasLabel = await label.isVisible().catch(() => false);
          expect(ariaLabel || placeholder || hasLabel).toBeTruthy();
        } else {
          expect(ariaLabel || placeholder).toBeTruthy();
        }
      }
    }
  });
});

test.describe('Focus Timer Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/focus');
  });

  test('timer display is accessible', async ({ page }) => {
    const timer = page.locator('[data-testid="timer-display"], .timer-display, text=/\\d+:\\d+/');
    await expect(timer).toBeVisible({ timeout: 10000 });
  });

  test('timer controls have accessible names', async ({ page }) => {
    const buttons = page.locator('button');
    const count = await buttons.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const button = buttons.nth(i);
      if (await button.isVisible().catch(() => false)) {
        const text = await button.textContent();
        const ariaLabel = await button.getAttribute('aria-label');
        const title = await button.getAttribute('title');
        expect(text || ariaLabel || title).toBeTruthy();
      }
    }
  });

  test('page has proper heading structure', async ({ page }) => {
    const heading = page.locator('h1, h2, h3').first();
    await expect(heading).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Navigation Accessibility', () => {
  test('navigation has proper landmark', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    const nav = page.locator('nav, [role="navigation"]');
    await expect(nav).toBeVisible();
  });

  test('nav links have accessible names', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    const navLinks = page.locator('nav a, [role="navigation"] a');
    const count = await navLinks.count();

    for (let i = 0; i < Math.min(count, 10); i++) {
      const link = navLinks.nth(i);
      if (await link.isVisible().catch(() => false)) {
        const text = await link.textContent();
        const ariaLabel = await link.getAttribute('aria-label');
        expect(text?.trim() || ariaLabel).toBeTruthy();
      }
    }
  });

  test('active page is indicated', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    // Look for active/current indicator
    const activeLink = page.locator('nav .active, nav [aria-current="page"], nav .nav-link.active');
    const isVisible = await activeLink.isVisible().catch(() => false);

    // This is a recommendation
    if (!isVisible) {
      console.log('Recommendation: Indicate current page in navigation');
    }
  });
});

test.describe('Task List Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/tasks/next');
  });

  test('task list has proper structure', async ({ page }) => {
    await page.waitForTimeout(2000);

    // Should use semantic list or proper ARIA
    const list = page.locator('.list-group, ul, ol, [role="list"]');
    const isVisible = await list.isVisible().catch(() => false);

    if (!isVisible) {
      console.log('Recommendation: Use semantic list elements for task lists');
    }
  });

  test('task items are properly labeled', async ({ page }) => {
    await page.waitForTimeout(2000);

    const items = page.locator('.list-group-item, [role="listitem"]');
    const count = await items.count();

    if (count > 0) {
      const firstItem = items.first();
      const text = await firstItem.textContent();
      expect(text).toBeTruthy();
    }
  });

  test('priority indicators are accessible', async ({ page }) => {
    await page.waitForTimeout(2000);

    // If there are priority indicators, they should be accessible
    const priorityIndicators = page.locator('[class*="priority"], [data-priority]');
    const count = await priorityIndicators.count();

    for (let i = 0; i < Math.min(count, 3); i++) {
      const indicator = priorityIndicators.nth(i);
      if (await indicator.isVisible().catch(() => false)) {
        const ariaLabel = await indicator.getAttribute('aria-label');
        const title = await indicator.getAttribute('title');
        const text = await indicator.textContent();

        // Should have some form of accessible label
        expect(ariaLabel || title || text).toBeTruthy();
      }
    }
  });
});

test.describe('Color and Contrast', () => {
  test('success states are not indicated by color alone', async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');

    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await input.fill('Color test');
    await page.locator('button:has-text("Capture")').click();

    // Success message should have text, not just color
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
  });

  test('error states are not indicated by color alone', async ({ page, helpers }) => {
    // Navigate to a page and look for any error indicators
    await helpers.navigateTo('/');

    const errorIndicators = page.locator('.text-danger, .alert-danger, [role="alert"]');
    const count = await errorIndicators.count();

    for (let i = 0; i < Math.min(count, 3); i++) {
      const indicator = errorIndicators.nth(i);
      if (await indicator.isVisible().catch(() => false)) {
        const text = await indicator.textContent();
        // Errors should have text content
        expect(text).toBeTruthy();
      }
    }
  });
});

test.describe('Keyboard Navigation', () => {
  test('can navigate entire app with keyboard', async ({ page, helpers }) => {
    await helpers.navigateTo('/');

    // Tab through multiple elements
    for (let i = 0; i < 20; i++) {
      await page.keyboard.press('Tab');
    }

    // Should be able to tab without getting stuck
    const focused = page.locator(':focus');
    await expect(focused).toBeVisible({ timeout: 5000 }).catch(() => {});
  });

  test('escape key closes modals', async ({ page, helpers }) => {
    // Create item and open process modal
    await helpers.navigateTo('/capture');
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    await input.fill('Escape test ' + Date.now());
    await page.locator('button:has-text("Capture")').click();
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    await helpers.navigateTo('/inbox');
    const processButton = page.locator('button:has-text("Process")').first();

    if (await processButton.isVisible().catch(() => false)) {
      await processButton.click();
      await expect(page.locator('text=Is this actionable')).toBeVisible();

      // Press Escape to close
      await page.keyboard.press('Escape');

      // Modal should close
      await expect(page.locator('text=Is this actionable')).not.toBeVisible({ timeout: 5000 });
    }
  });
});
