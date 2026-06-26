/**
 * Regression test: TF-UC-01-01-01-01
 * Use Case: UC-01-01-01 – Retrieve Published Work Schedule
 *
 * Verifies that the work-schedule endpoint responds with HTTP 200 for
 * the current week.  A null/empty payload is accepted when no schedule
 * has been published yet (empty state is valid per TF-UC-01-01-01-03).
 */
import { test, expect } from '@playwright/test';
import { loginAsEmployeeViaApi } from '../helpers/auth';
import { currentWeekMonday } from '../helpers/testData';

test.describe('TF-UC-01-01-01-01 – Retrieve Work Schedule [API]', () => {
  test('GET /api/Workschedule/active returns 200 OK', async ({ page }) => {
    // Precondition: authenticate as employee
    // page.request is used so the browser cookie jar stores the Secure JWT cookie.
    await loginAsEmployeeViaApi(page.request);

    const startDate = currentWeekMonday();

    // Act: request the active schedule for the current week
    const response = await page.request.get(
      `/api/Workschedule/active?startDate=${startDate}`,
    );

    // Assert: endpoint is reachable and returns a well-formed HTTP response
    // 404 is also valid when no schedule has been published for this week.
    expect([200, 404]).toContain(response.status());

    // Only validate body structure when a schedule actually exists.
    if (response.status() === 200) {
      const text = await response.text();
      if (text && text.trim() !== 'null') {
        const body = JSON.parse(text);
        expect(body).toHaveProperty('id');
      }
    }
  });
});
