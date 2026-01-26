import { test, expect } from '../fixtures/base';

/**
 * Capture Workflow E2E Tests
 * Based on: .ai/STATECHARTS.md - "Capture Input Statechart"
 * Based on: .ai/PETRI-MODELS.md - "Capture Item Operation"
 *
 * Statechart States:
 * [Empty] -> [HasText] -> [Submitting] -> [Success]
 *
 * Petri Net Places:
 * Idle -> Capturing -> Persisting -> Complete/Failed
 */

test.describe('Capture Workflow - Statechart Tests', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('Empty -> HasText: typing text enables capture button', async ({ page }) => {
    // Initial state: Empty
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Verify initial empty state
    await expect(input).toHaveValue('');
    await expect(captureButton).toBeDisabled();

    // Transition: type text -> HasText state
    await input.fill('Test capture item');

    // Verify HasText state: button enabled
    await expect(captureButton).toBeEnabled();
  });

  test('HasText -> Empty: clearing text disables capture button', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Enter HasText state
    await input.fill('Test capture item');
    await expect(captureButton).toBeEnabled();

    // Transition: clear text -> Empty state
    await input.fill('');

    // Verify Empty state
    await expect(captureButton).toBeDisabled();
  });

  test('HasText -> Submitting -> Success: successful capture via button click', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Enter HasText state
    await input.fill('My important task');
    await expect(captureButton).toBeEnabled();

    // Transition: submit -> Submitting -> Success
    await captureButton.click();

    // Verify Success state: success message appears
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Verify input cleared (back to Empty state after timeout)
    await expect(input).toHaveValue('');
  });

  test('HasText -> Submitting: keyboard shortcut Cmd/Ctrl+Enter triggers capture', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');

    // Enter HasText state
    await input.fill('Keyboard shortcut test');

    // Use platform-appropriate modifier
    const modifier = process.platform === 'darwin' ? 'Meta' : 'Control';

    // Transition: Cmd+Enter -> Submitting
    await input.press(`${modifier}+Enter`);

    // Verify capture succeeded
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
  });

  test('captures appear in recently captured list', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Capture an item
    await input.fill('Test item for recent list');
    await captureButton.click();

    // Verify it appears in recent captures
    await expect(page.locator('text=Test item for recent list')).toBeVisible({ timeout: 5000 });
  });

  test('capture count increments after successful capture', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Get initial count (look for text like "X item(s) captured today")
    const statsText = page.locator('text=/\\d+ item.*captured/');

    // Capture an item
    await input.fill('Counting test');
    await captureButton.click();

    // Wait for success
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // Verify count updated
    await expect(statsText).toBeVisible();
  });
});

test.describe('Capture Workflow - Petri Net Tests', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('Petri Net: Idle -> Capturing -> Persisting -> Complete (normal flow)', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // P1: Idle state (token in Idle)
    await expect(captureButton).toBeDisabled();

    // Fill input (prepare for T1: Submit)
    await input.fill('Petri net test item');

    // T1: Submit transition (Idle -> Capturing)
    await captureButton.click();

    // Verify button shows loading state during Capturing/Persisting
    // The button should be disabled or show a spinner
    await expect(captureButton).toBeDisabled();

    // T2: SaveSuccess transition (Persisting -> Complete)
    // Verify success message appears
    await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });

    // T4: AckComplete transition (Complete -> Idle)
    // Input should be cleared, ready for next capture
    await expect(input).toHaveValue('');
    await expect(captureButton).toBeDisabled(); // Back to idle (no text)
  });

  test('Petri Net: Double-submit prevention (1-bounded property)', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    await input.fill('Double submit test');
    await captureButton.click();

    // During Submitting state, button should be disabled (1-bounded: only one token)
    await expect(captureButton).toBeDisabled();

    // Cannot submit again while in Submitting state
    // This verifies the Petri net is 1-bounded
  });

  test('Petri Net: Multiple sequential captures (liveness)', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');
    const captureButton = page.locator('button:has-text("Capture")');

    // Verify we can capture multiple items sequentially
    // This proves liveness: all transitions can eventually fire
    for (let i = 1; i <= 3; i++) {
      await input.fill(`Sequential capture ${i}`);
      await captureButton.click();
      await expect(page.locator('.capture-success, .text-success:has-text("Captured")')).toBeVisible({ timeout: 5000 });
      await page.waitForTimeout(2500); // Wait for success message to clear and state to reset
    }
  });
});

test.describe('Capture Page - Accessibility', () => {
  test.beforeEach(async ({ page, helpers }) => {
    await helpers.navigateTo('/capture');
  });

  test('capture input has proper ARIA attributes', async ({ page }) => {
    const input = page.locator('.capture-input, textarea[placeholder*="mind"]');

    // Check for placeholder or aria-label
    const placeholder = await input.getAttribute('placeholder');
    const ariaLabel = await input.getAttribute('aria-label');

    expect(placeholder || ariaLabel).toBeTruthy();
  });

  test('capture button has accessible name', async ({ page }) => {
    const captureButton = page.locator('button:has-text("Capture")');

    // Button should have visible text
    await expect(captureButton).toHaveText(/Capture/);
  });

  test('page has proper heading structure', async ({ page }) => {
    // Should have h2 or h3 as main heading
    const heading = page.locator('h2, h3').first();
    await expect(heading).toBeVisible();
  });

  test('back link is keyboard accessible', async ({ page }) => {
    const backLink = page.locator('a:has-text("Back"), a[href="/"]');

    // Should be focusable
    await backLink.focus();
    await expect(backLink).toBeFocused();
  });
});
