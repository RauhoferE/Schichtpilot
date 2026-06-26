/**
 * Regression test: TF-UC-04-01-01-01
 * Use Case: UC-04-01-01 – Create Job Role
 *
 * Verifies that a manager can create a new job role via the UI,
 * and that the role subsequently appears in the job role list.
 */
import { test, expect } from '@playwright/test';
import { loginAsManagerViaUi } from '../helpers/auth';
import { JOB_ROLE_NAME, JOB_ROLE_DESCRIPTION } from '../helpers/testData';

test.describe('TF-UC-04-01-01-01 – Create Job Role [UI]', () => {
  test('manager creates a job role and it appears in the list', async ({ page }) => {
    // Precondition: log in as manager and navigate to job role management
    await loginAsManagerViaUi(page);
    await page.goto('/manager/jobrole');
    await page.waitForSelector('h1');

    // Act: open the "Add job role" dialog
    await page.getByRole('button', { name: 'Add job role' }).click();
    await page.waitForSelector('#jobRoleName');

    // Fill in name and description
    await page.fill('#jobRoleName', JOB_ROLE_NAME);
    await page.fill('#jobRoleDescription', JOB_ROLE_DESCRIPTION);

    // Submit the form
    await page.getByRole('button', { name: 'Create' }).click();

    // Assert: the dialog closes and the new role is visible in the table
    await expect(page.getByText(JOB_ROLE_NAME)).toBeVisible({ timeout: 5000 });
  });
});
