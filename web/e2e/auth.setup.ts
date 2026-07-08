import { expect, test as setup } from '@playwright/test';

const authFile = 'e2e/.auth/admin.json';

setup('authenticate as admin', async ({ page }) => {
  await page.goto('/login');
  const email = page.locator('input#feegow-email, input#email');
  const password = page.locator('input#feegow-password, input#password');

  await expect(email.first()).toBeVisible();
  await email.first().fill('admin@hospital.local');
  await password.first().fill('Admin123!');
  await page.getByRole('button', { name: 'Entrar' }).click();

  await expect(page).not.toHaveURL(/\/login(?:\?|$)/, { timeout: 30_000 });
  await page.context().storageState({ path: authFile });
});
