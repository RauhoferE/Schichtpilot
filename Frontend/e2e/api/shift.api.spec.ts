/**
 * Regression test: TF-UC-04-02-02-01
 * Use Case: UC-04-02-02 – Create Shift
 *
 * Verifies that a manager can create a shift with all mandatory fields
 * via the REST API and receives HTTP 201 Created in response.
 */
import { test, expect } from '@playwright/test';
import { loginAsManagerViaApi } from '../helpers/auth';
import { SHIFT_NAME } from '../helpers/testData';

test.describe('TF-UC-04-02-02-01 – Create Shift [API]', () => {
  test('POST /api/Shift with valid data returns 201 Created', async ({ page }) => {
    // Precondition: authenticate as manager
    // page.request is used so the browser cookie jar (which allows Secure
    // cookies on localhost) stores and forwards the JWT cookie.
    await loginAsManagerViaApi(page.request);

    // Act: submit a new shift with one timeslot per weekday (Mon–Fri)
    const response = await page.request.post('/api/Shift', {
      data: {
        name: SHIFT_NAME,
        description: 'Automated regression test shift',
        colorAsHex: '#f59e0b',
        timeSlots: [1, 2, 3, 4, 5].map((dayOfWeek) => ({
          id: 0,
          dayOfWeek,
          startTime: '06:00',
          endTime: '14:00',
          breaks: [],
        })),
        jobRequirements: [],
      },
    });

    // Assert: backend persists the shift and responds with 201
    expect(response.status()).toBe(201);
  });
});
