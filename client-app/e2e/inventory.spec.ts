import { test, expect } from '@playwright/test';

test.describe('Inventory Page', () => {
  test('should navigate to inventory page and verify inventory list renders', async ({ page }) => {
    await page.goto('/inventory');
    await expect(page.locator('h2')).toContainText('Inventory');
  });

  test('should display inventory data in table', async ({ page }) => {
    await page.goto('/inventory');
    const table = page.locator('table');
    await expect(table).toBeVisible();
    const rows = page.locator('tbody tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });
});
