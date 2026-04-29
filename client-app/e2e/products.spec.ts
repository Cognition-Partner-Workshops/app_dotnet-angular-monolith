import { test, expect } from '@playwright/test';

test.describe('Products Page', () => {
  test('should navigate to products page and verify product list renders', async ({ page }) => {
    await page.goto('/products');
    await expect(page.locator('h2')).toContainText('Products');
  });

  test('should display product data in table', async ({ page }) => {
    await page.goto('/products');
    const table = page.locator('table');
    await expect(table).toBeVisible();
    const rows = page.locator('tbody tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });
});
