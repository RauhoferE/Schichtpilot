/**
 * Regression test: TF-UC-02-01-01-01
 * Use Case: UC-02-01-01 – Submit Absence Request
 *
 * Verifies that an employee can submit a vacation absence request via the
 * UI dialog, and that the system confirms the request with a success message.
 */
import { test, expect } from '@playwright/test';
import { loginAsEmployeeViaUi } from '../helpers/auth';
import {
  ABSENCE_START_DATE,
  ABSENCE_END_DATE,
  ABSENCE_TYPE,
  ABSENCE_MESSAGE,
} from '../helpers/testData';

test.describe('TF-UC-02-01-01-01 – Submit Absence Request [UI]', () => {
  test('employee submits a vacation absence and sees a success confirmation', async ({ page }) => {
    // Precondition: log in as employee and open the absence page
    await loginAsEmployeeViaUi(page);
    await page.goto('/employee/absence');
    await page.waitForSelector('h1');

    // Act: open the "Add new absence" dialog
    await page.getByRole('button', { name: '+ Add new absence' }).click();
    await page.waitForSelector('#absenceType');

    // Select absence type
    await page.selectOption('#absenceType', ABSENCE_TYPE);

    // Set start and end dates (date inputs accept ISO strings directly)
    await page.fill('#startDate', ABSENCE_START_DATE);
    await page.fill('#endDate', ABSENCE_END_DATE);

    // Enter an optional message
    await page.fill('#message', ABSENCE_MESSAGE);

    // Submit the request
    await page.getByRole('button', { name: 'Save request' }).click();

    // Assert: a success toast is shown after submission
    await expect(
      page.locator('.border-green-500, [class*="green"]').first(),
    ).toBeVisible({ timeout: 5000 });
  });
});
