import { expect, test, type Page } from '@playwright/test';
import { getBaseUrl } from '../support/base-url';

// Regression coverage for the two previously-reported new-tab bugs:
//   1. Creating a tab failed to save (fixed via ApiError handling).
//   2. The in-progress draft was lost when navigating to History and back
//      (fixed via sessionStorage-backed draft persistence).
// Both must stay green.

test.describe('New tab regressions', () => {
  test('regression: creating a tab saves and opens the tab detail', async ({ page }) => {
    const baseUrl = getBaseUrl();
    const tabName = `Save Regression ${Date.now()}`;

    await devLogin(page, baseUrl, 'Save Regression Owner');
    await page.goto(`${baseUrl}/tabs/new`);
    await expect(page.getByRole('heading', { name: 'Start a new tab' })).toBeVisible();

    await page.getByLabel('Tab name').fill(tabName);
    await page.getByLabel('Currency').fill('USD');
    await page.getByRole('button', { name: 'Create tab' }).click();

    // Bug repro condition: save used to fail and never navigate. Now it must
    // land on the detail page for the new tab.
    await expect(page).toHaveURL(/\/tabs\/[a-z0-9]+$/);
    await expect(page.getByRole('heading', { name: tabName })).toBeVisible();
    await expect(page.locator('.total-pill')).toHaveText('$0.00');
    await expect(page.locator('.error-text')).toHaveCount(0);
  });

  test('regression: draft survives navigating to History and back', async ({ page }) => {
    const baseUrl = getBaseUrl();
    const draftName = `Draft ${Date.now()}`;

    await devLogin(page, baseUrl, 'Draft Owner');
    await page.goto(`${baseUrl}/tabs/new`);
    await expect(page.getByRole('heading', { name: 'Start a new tab' })).toBeVisible();

    await page.getByLabel('Tab name').fill(draftName);

    // Navigate away to History, then back to New.
    await page.getByRole('link', { name: 'History' }).click();
    await expect(page.getByRole('heading', { name: 'History' })).toBeVisible();
    await page.getByRole('link', { name: 'New' }).click();
    await expect(page.getByRole('heading', { name: 'Start a new tab' })).toBeVisible();

    // Bug repro condition: the draft name used to be wiped. It must persist.
    await expect(page.getByLabel('Tab name')).toHaveValue(draftName);
  });
});

async function devLogin(page: Page, baseUrl: string, displayName: string) {
  const response = await page.request.post(`${baseUrl}/api/auth/dev-login`, {
    data: { displayName, avatarUrl: null },
  });
  expect(response.ok()).toBeTruthy();
}
