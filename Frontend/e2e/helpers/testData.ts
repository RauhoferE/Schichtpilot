// Credentials — set via environment variables before running the tests.
// Example: MANAGER_EMAIL=admin@example.com MANAGER_PASSWORD=Secret123! npm run test:e2e
export const MANAGER_EMAIL    = process.env.MANAGER_EMAIL    ?? 'manager@test.at';
export const MANAGER_PASSWORD = process.env.MANAGER_PASSWORD ?? 'Manager123!';
export const EMPLOYEE_EMAIL    = process.env.EMPLOYEE_EMAIL    ?? 'employee@test.at';
export const EMPLOYEE_PASSWORD = process.env.EMPLOYEE_PASSWORD ?? 'Employee123!';

// A unique suffix so repeated test runs do not produce duplicate-name conflicts.
export const RUN_ID = Date.now().toString();

export const SHIFT_NAME = `RS-${RUN_ID.slice(-8)}`; // max 25 chars enforced by validator
export const JOB_ROLE_NAME = `Regression Role ${RUN_ID}`;
export const JOB_ROLE_DESCRIPTION = 'Created by automated regression test';

export const ABSENCE_START_DATE = '2026-09-07';
export const ABSENCE_END_DATE   = '2026-09-13';
export const ABSENCE_TYPE       = 'Vacation';
export const ABSENCE_MESSAGE    = 'Automated regression test – family vacation';

// Monday of the current ISO week (used for the work-schedule query).
export function currentWeekMonday(): string {
  const now = new Date();
  const day = now.getDay() === 0 ? 7 : now.getDay();
  const monday = new Date(now);
  monday.setDate(now.getDate() - day + 1);
  return monday.toISOString().split('T')[0];
}
