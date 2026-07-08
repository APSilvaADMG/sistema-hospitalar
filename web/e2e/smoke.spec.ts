import { expect, test } from '@playwright/test';

const apiBase = process.env.PLAYWRIGHT_API_URL ?? 'http://127.0.0.1:8080';

test.describe('Smoke — fluxos críticos', () => {
  test('API health responde', async ({ request }) => {
    const response = await request.get(`${apiBase}/health`);
    expect(response.ok()).toBeTruthy();
  });

  test('login redireciona para o painel', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel('E-mail')).toBeVisible();
    await page.getByLabel('E-mail').fill('admin@hospital.local');
    await page.getByLabel('Senha').fill('Admin123!');
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page).not.toHaveURL(/\/login(?:\?|$)/, { timeout: 30_000 });
  });

  test('sala de espera carrega KPIs', async ({ page }) => {
    await page.goto('/emergencia');
    await expect(page.getByRole('heading', { name: 'Sala de Espera' })).toBeVisible({
      timeout: 30_000,
    });
    await expect(page.locator('.feegow-waiting-kpi-grid')).toBeVisible();
    await expect(page.locator('.feegow-waiting-kpi-label').first()).toBeVisible();
  });

  test('hub financeiro carrega', async ({ page }) => {
    await page.goto('/financeiro');
    await expect(page.getByRole('heading', { name: /Gestão Financeira/i })).toBeVisible({
      timeout: 30_000,
    });
    await expect(page.locator('.guides-kpi-row, .kpi-grid').first()).toBeVisible();
  });

  test('página BI carrega', async ({ page }) => {
    await page.goto('/bi');
    await expect(
      page.getByRole('heading', { name: /Business Intelligence|BI/i }),
    ).toBeVisible({ timeout: 30_000 });
    await expect(page.locator('.kpi-grid').first()).toBeVisible();
  });
});
