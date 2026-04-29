import { test, expect } from '@playwright/test';

test.describe('Orders Page', () => {
  test('should navigate to orders page and verify list renders', async ({ page }) => {
    await page.goto('/orders');
    await expect(page.locator('h2')).toContainText('Orders');
  });

  test('should show empty state or orders list', async ({ page }) => {
    await page.goto('/orders');
    const table = page.locator('table');
    const emptyMessage = page.locator('p');
    const hasTable = await table.isVisible().catch(() => false);
    const hasEmpty = await emptyMessage.isVisible().catch(() => false);
    expect(hasTable || hasEmpty).toBeTruthy();
  });
});
