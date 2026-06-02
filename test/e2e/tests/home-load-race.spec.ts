import { expect, test } from '@playwright/test';
import { getBaseUrl } from '../support/base-url';

// Regression test for BUG-1 (fixed): HomePage previously fired listTabs() on
// mount concurrently with the SPA's dev-login fallback, received a 401 before
// the auth cookie was set, set a terminal "Could not load open tabs yet."
// error, and never refetched once auth resolved.
//
// The fix gates the initial tabs fetch on auth readiness (useAuth) and refetches
// once authentication resolves. This test asserts the CORRECT behaviour: tabs
// load without error after the SPA finishes authenticating on a fresh session.

test.describe('Home load auth race (regression)', () => {
  test('home shows existing tabs after the SPA dev-login fallback resolves', async ({ page }) => {
    const baseUrl = getBaseUrl();
    const tabName = `Race Tab ${Date.now()}`;

    // Seed a tab for the dev user via the API (authenticated request context).
    const login = await page.request.post(`${baseUrl}/api/auth/dev-login`, {
      data: { displayName: 'Race Owner', avatarUrl: null },
    });
    expect(login.ok()).toBeTruthy();
    const created = await page.request.post(`${baseUrl}/api/tabs`, {
      data: { name: tabName, currency: 'USD' },
    });
    expect(created.ok()).toBeTruthy();

    // Force a fresh, unauthenticated browser session so the SPA must use its
    // dev-login fallback. Delay that fallback so the home tab list deterministically
    // 401s first — exactly the production race condition.
    await page.context().clearCookies();
    await page.route('**/api/auth/dev-login', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1200));
      await route.continue();
    });

    await page.goto(baseUrl);

    // The SPA eventually authenticates (same dev user that owns the seeded tab).
    await expect(page.getByLabel('Signed in as Race Owner')).toBeVisible();

    // DESIRED: the seeded tab is listed and no error banner is shown.
    await expect(page.getByText('Could not load open tabs yet.')).toHaveCount(0);
    await expect(page.getByRole('link', { name: new RegExp(escapeRegExp(tabName)) })).toBeVisible();
  });
});

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
