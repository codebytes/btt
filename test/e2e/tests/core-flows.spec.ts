import { expect, test, type Page } from '@playwright/test';
import { getBaseUrl } from '../support/base-url';

test.describe('BarTabTracker core flows', () => {
  test('dev-login, tab lifecycle, item entry, split views, invite, and history', async ({ context, page }) => {
    const baseUrl = getBaseUrl();
    const tabName = `E2E Tab ${Date.now()}`;

    await devLogin(page, baseUrl, 'E2E Owner');

    await page.goto(baseUrl);
    await expect(page.getByRole('heading', { name: "Tonight's tabs" })).toBeVisible();
    await expect(page.getByLabel('Signed in as E2E Owner')).toBeVisible();

    await installGeolocationDenial(page);
    await page.getByRole('link', { name: 'Start a tab' }).click();
    await expect(page.getByRole('heading', { name: 'Start a new tab' })).toBeVisible();
    await page.getByRole('button', { name: 'Use my location' }).click();
    await expect(page.getByText('Location permission was denied or unavailable. You can continue without a map pin.')).toBeVisible();

    await page.getByLabel('Tab name').fill(tabName);
    await page.getByLabel('Currency').fill('USD');
    await page.getByRole('button', { name: 'Create tab' }).click();
    await expect(page.getByRole('heading', { name: tabName })).toBeVisible();
    await expect(page.getByText('No bar tagged')).toBeVisible();

    await page.getByRole('link', { name: 'Tabs' }).click();
    await expect(page.getByRole('link', { name: new RegExp(tabName) })).toBeVisible();
    await page.getByRole('link', { name: new RegExp(tabName) }).click();

    await addItem(page, { name: 'Shared Fries', price: '4.00', quantity: '1', orderedBy: 'Shared' });
    await addItem(page, { name: 'Margarita', price: '6.00', quantity: '1', orderedBy: 'You' });

    await expect(page.locator('.total-pill')).toHaveText('$10.00');
    await expect(page.getByText('Shared Fries')).toBeVisible();
    await expect(page.getByText('Margarita')).toBeVisible();
    await expect(page.getByText('1 × $4.00 · Shared')).toBeVisible();
    await expect(page.getByText('1 × $6.00 · You')).toBeVisible();

    const splitCard = page.locator('.page-card').filter({ has: page.getByText('Split') });
    await splitCard.getByRole('button', { name: 'Equal' }).click();
    await expect(splitCard.locator('.list-row').filter({ hasText: 'You' })).toContainText('$10.00');
    await splitCard.getByRole('button', { name: 'Itemized' }).click();
    await expect(splitCard.locator('.list-row').filter({ hasText: 'You' })).toContainText('$10.00');

    const inviteCard = page.locator('.invite-card');
    await expect(inviteCard.getByText('Invite friends')).toBeVisible();
    const inviteUrl = await inviteCard.locator('p').innerText();
    expect(inviteUrl).toMatch(new RegExp(`^${escapeRegExp(baseUrl)}/join/[A-Za-z0-9_-]+$`));

    await context.grantPermissions(['clipboard-write'], { origin: baseUrl });
    await inviteCard.getByRole('button', { name: 'Copy' }).click();
    await expect(inviteCard.getByRole('button', { name: 'Copied' })).toBeVisible();

    await page.goto(inviteUrl);
    await expect(page.getByRole('heading', { name: tabName })).toBeVisible();

    await page.getByRole('button', { name: 'Close tab' }).click();
    await expect(page.locator('.status-badge').filter({ hasText: 'closed' })).toBeVisible();

    await page.getByRole('link', { name: 'History' }).click();
    await expect(page.getByRole('heading', { name: 'History' })).toBeVisible();
    await expect(page.getByRole('link', { name: new RegExp(tabName) })).toBeVisible();
  });
});

async function devLogin(page: Page, baseUrl: string, displayName: string) {
  const response = await page.request.post(`${baseUrl}/api/auth/dev-login`, {
    data: { displayName, avatarUrl: null },
  });
  expect(response.ok()).toBeTruthy();
}

async function installGeolocationDenial(page: Page) {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'geolocation', {
      configurable: true,
      value: {
        getCurrentPosition: (_success: PositionCallback, error?: PositionErrorCallback) => {
          error?.({ code: 1, message: 'User denied Geolocation', PERMISSION_DENIED: 1, POSITION_UNAVAILABLE: 2, TIMEOUT: 3 } as GeolocationPositionError);
        },
        watchPosition: () => 0,
        clearWatch: () => undefined,
      },
    });
  });
}

async function addItem(page: Page, item: { name: string; price: string; quantity: string; orderedBy: string }) {
  const form = page.locator('form').filter({ hasText: 'Add item' });
  await form.getByLabel('Item name').fill(item.name);
  await form.getByLabel('Price').fill(item.price);
  await form.getByLabel('Qty').fill(item.quantity);
  await form.getByLabel('Who ordered?').selectOption({ label: item.orderedBy });
  await form.getByRole('button', { name: 'Add item' }).click();
  await expect(page.getByText(item.name)).toBeVisible();
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
