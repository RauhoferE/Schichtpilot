// ── Schedule types ────────────────────────────────────────────────────────────

// Shift codes used in the schedule grid
// L = Lunch, E = Evening, M = Mixed, V = Day off/Vacation, null = not assigned
export type ShiftCode = 'L' | 'E' | 'M' | 'V' | 'X' | null;

// A single day entry for one staff member
export interface DaySchedule {
    date: string;   // YYYY-MM-DD
    shift: ShiftCode;
}

// One staff member's full week
export interface StaffSchedule {
    name: string;
    days: DaySchedule[]; // always 7 entries, Mon–Sun
}

// A full week of schedule data for all staff
export interface Week {
    label: string;      // e.g. "03.05.25 – 09.05.25"
    startDate: string;  // YYYY-MM-DD (always a Monday)
    staff: StaffSchedule[];
}

// Metadata for displaying a shift code in the UI
export interface ShiftMeta {
    label: string;  // e.g. "Lunch Shift"
    hours: string;  // e.g. "10:00–17:00"
    bg: string;     // tailwind bg class
    text: string;   // tailwind text class
}