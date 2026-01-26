import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for Self-Organizer GTD E2E tests
 * Tests are derived from formal models (Petri Nets + Statecharts)
 * See .ai/PETRI-MODELS.md and .ai/STATECHARTS.md for specifications
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: false, // Run tests sequentially for Blazor WASM
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,
  workers: 1, // Single worker for stability with Blazor WASM
  reporter: [
    ['html', { open: 'never' }],
    ['list']
  ],
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /* Run the Blazor app before running tests */
  webServer: {
    command: 'cd ../src/SelfOrganizer.App && dotnet run --urls=http://localhost:5000',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 180000, // Blazor WASM can take time to start
    stdout: 'pipe',
    stderr: 'pipe',
  },

  /* Global timeout for tests - longer for Blazor WASM */
  timeout: 60000,
  expect: {
    timeout: 15000,
  },
});
