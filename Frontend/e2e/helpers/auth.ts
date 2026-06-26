import { type Page, type APIRequestContext } from '@playwright/test';
import {
  MANAGER_EMAIL, MANAGER_PASSWORD,
  EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD,
} from './testData';

/**
 * Logs in via the browser UI and waits for the redirect to the app.
 */
export async function loginViaUi(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.fill('#email', email);
  await page.fill('#password', password);
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/(manager|employee)\//);
}

export const loginAsManagerViaUi = (page: Page) =>
  loginViaUi(page, MANAGER_EMAIL, MANAGER_PASSWORD);

export const loginAsEmployeeViaUi = (page: Page) =>
  loginViaUi(page, EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD);

/**
 * Logs in via the REST API.
 * Use page.request (not the standalone request fixture) so that the
 * browser context cookie jar — which treats localhost as a secure origin —
 * stores and forwards the Secure JWT cookie on subsequent calls.
 */
export async function loginViaApi(
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<void> {
  const response = await request.post('/api/auth/login', {
    data: { email, password },
  });
  if (!response.ok()) {
    throw new Error(`API login failed: ${response.status()} ${await response.text()}`);
  }
}

export const loginAsManagerViaApi = (request: APIRequestContext) =>
  loginViaApi(request, MANAGER_EMAIL, MANAGER_PASSWORD);

export const loginAsEmployeeViaApi = (request: APIRequestContext) =>
  loginViaApi(request, EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD);
