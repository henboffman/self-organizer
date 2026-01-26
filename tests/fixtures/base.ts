import { test as base, expect, Page } from '@playwright/test';

/**
 * Extended test fixture for Self-Organizer GTD tests
 * Provides common utilities and helpers for Blazor WASM testing
 */

export interface TestHelpers {
  /**
   * Wait for Blazor to finish loading and be interactive
   */
  waitForBlazorReady: () => Promise<void>;

  /**
   * Complete or skip the onboarding wizard if it appears
   */
  completeOnboarding: () => Promise<void>;

  /**
   * Clear all IndexedDB data for a fresh test state
   */
  clearIndexedDb: () => Promise<void>;

  /**
   * Wait for a toast notification to appear and optionally dismiss it
   */
  waitForToast: (type?: 'success' | 'error' | 'info' | 'warning') => Promise<void>;

  /**
   * Get the current item count in IndexedDB for a store
   */
  getDbCount: (storeName: string) => Promise<number>;

  /**
   * Navigate and wait for Blazor page to be ready
   */
  navigateTo: (path: string) => Promise<void>;
}

export const test = base.extend<{ helpers: TestHelpers }>({
  helpers: async ({ page }, use) => {
    const helpers: TestHelpers = {
      async waitForBlazorReady() {
        // Wait for Blazor WASM to fully load - check for app content
        // Blazor WASM shows a loading indicator initially, then renders the app

        // First, wait for the page to have any content
        await page.waitForLoadState('domcontentloaded');

        // Wait for Blazor to initialize - check for the app element being hydrated
        // For Blazor WASM, we wait for the main content to appear
        await page.waitForFunction(() => {
          // Check if Blazor has rendered content (not just the loading placeholder)
          const app = document.querySelector('#app, .page, main, [data-page]');
          const hasContent = app && app.innerHTML.trim().length > 100;
          const noLoading = !document.querySelector('.loading, #blazor-loading-ui');
          return hasContent || noLoading;
        }, { timeout: 60000 });

        // Wait for initial loading spinner to disappear
        await page.waitForSelector('svg.loading-progress', { state: 'hidden', timeout: 60000 }).catch(() => {});

        // Small delay for Blazor re-renders
        await page.waitForTimeout(1500);
      },

      async completeOnboarding() {
        // Check if onboarding wizard is visible
        const onboardingOverlay = page.locator('.onboarding-overlay');
        const isOnboardingVisible = await onboardingOverlay.isVisible().catch(() => false);

        if (!isOnboardingVisible) {
          return; // No onboarding, continue with test
        }

        console.log('Onboarding wizard detected, completing...');

        // The onboarding has 9 steps (0-8)
        // We need to click through all of them

        // Step 0: Theme Selection - click a theme option
        const lightTheme = page.locator('button.theme-option:has-text("Light Mode")');
        if (await lightTheme.isVisible().catch(() => false)) {
          await lightTheme.click();
          await page.waitForTimeout(300);
        }

        // Click through all the Next buttons until we reach "Get Started"
        for (let step = 0; step < 10; step++) {
          // Check for "Get Started" button (final step)
          const getStarted = page.locator('button:has-text("Get Started")');
          if (await getStarted.isVisible().catch(() => false)) {
            await getStarted.click();
            await page.waitForTimeout(1000);
            break;
          }

          // Check for "Next" button
          const nextButton = page.locator('button:has-text("Next")');
          if (await nextButton.isVisible().catch(() => false)) {
            await nextButton.click();
            await page.waitForTimeout(500);
          }
        }

        // Wait for onboarding to close
        await onboardingOverlay.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {});

        // Additional delay for the app to settle
        await page.waitForTimeout(1000);
      },

      async clearIndexedDb() {
        await page.evaluate(async () => {
          const databases = await indexedDB.databases();
          for (const db of databases) {
            if (db.name) {
              indexedDB.deleteDatabase(db.name);
            }
          }
        });
        // Reload to reinitialize empty database
        await page.reload();
        await helpers.waitForBlazorReady();
      },

      async waitForToast(type?: 'success' | 'error' | 'info' | 'warning') {
        const selector = type
          ? `.toast.bg-${type}, .alert-${type}`
          : '.toast, .alert';
        await page.waitForSelector(selector, { state: 'visible', timeout: 5000 });
      },

      async getDbCount(storeName: string) {
        return await page.evaluate(async (store) => {
          return new Promise<number>((resolve, reject) => {
            const request = indexedDB.open('SelfOrganizerDb');
            request.onsuccess = () => {
              const db = request.result;
              try {
                const tx = db.transaction(store, 'readonly');
                const objectStore = tx.objectStore(store);
                const countRequest = objectStore.count();
                countRequest.onsuccess = () => resolve(countRequest.result);
                countRequest.onerror = () => reject(countRequest.error);
              } catch (e) {
                resolve(0);
              }
            };
            request.onerror = () => reject(request.error);
          });
        }, storeName);
      },

      async navigateTo(path: string) {
        await page.goto(path);
        await helpers.waitForBlazorReady();
        await helpers.completeOnboarding();
      }
    };

    await use(helpers);
  },
});

export { expect };
