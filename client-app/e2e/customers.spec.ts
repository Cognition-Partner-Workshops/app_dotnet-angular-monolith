import { test, expect } from '@playwright/test';

test.describe('Customers Page', () => {
  test('should navigate to customers page and verify customer list renders', async ({ page }) => {
    await page.goto('/customers');
    await expect(page.locator('h2')).toContainText('Customers');
  });

  test('should display customer data in table', async ({ page }) => {
    await page.goto('/customers');
    const table = page.locator('table');
    await expect(table).toBeVisible();
    const rows = page.locator('tbody tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });
});
