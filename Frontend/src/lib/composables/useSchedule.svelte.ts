import type { ShiftCode, StaffSchedule, Week } from '$lib/types/schedule.types';


export const DAY_HEADERS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
// Shift meta 
export const SHIFT_META: Record<NonNullable<ShiftCode>, { label: string; hours: string; bg: string; text: string }> = {
    L: { label: 'Lunch Shift',   hours: '10:00–17:00', bg: 'bg-yellow-200', text: 'text-yellow-900' },
    E: { label: 'Evening Shift', hours: '16:00–23:00', bg: 'bg-blue-200',   text: 'text-blue-900'   },
    M: { label: 'Mixed Shift',   hours: '13:00–20:00', bg: 'bg-purple-200', text: 'text-purple-900' },
    V: { label: 'Day off',       hours: '',            bg: 'bg-gray-200',   text: 'text-gray-600'   },
    X: {label: "Closed",         hours: '',            bg: 'bg-green-200',  text: ''                },   
};

//  Helpers 
function mondayOf(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day; // adjust so Monday = start
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
}

function fmtShort(date: Date): string {
    return `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}.${String(date.getFullYear()).slice(2)}`;
}

function addDays(date: Date, n: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + n);
    return d;
}

function toIso(date: Date): string {
    return date.toISOString().slice(0, 10);
}

//  Mock schedule builder 
// MOCK ONLY: replace with real API call when backend is connected
// GET /api/schedule?weekStart=YYYY-MM-DD
function buildMockWeek(monday: Date): Week {
    const end = addDays(monday, 6);
    const label = `${fmtShort(monday)} – ${fmtShort(end)}`;

    // Deterministic mock shifts per staff per weekday
    const mockData: Record<string, ShiftCode[]> = {
        //             Mon   Tue   Wed   Thu   Fri   Sat   Sun
        Hannah: ['X', 'V',  'V',  'V',  'V',  'V',  null],
        Paul:   ['X', 'L',  'E',  'E',  'E',  'E',  'M' ],
        Tom:    ['X', null, 'L',  'L',  'M',  'E',  'E' ],
        Hans:   ['X', 'E',  null, 'L',  null, 'E',  'L' ],
        Nina:   ['X', 'E',  'E',  'L',  'M',  'L',  'L' ],
        Sam:    ['X', null, 'L',  'L',  'E',  null, 'M' ],
    };

    const staff: StaffSchedule[] = Object.entries(mockData).map(([name, shifts]) => ({
        name,
        days: shifts.map((shift, i) => ({
            date: toIso(addDays(monday, i)),
            shift,
        })),
    }));

    return { label, startDate: toIso(monday), staff };
}

//State
export function createScheduleState() {
    // Start on current week's Monday
    let currentMonday = $state(mondayOf(new Date()));

    const week = $derived(buildMockWeek(currentMonday));

    function prevWeek() {
        currentMonday = addDays(currentMonday, -7);
    }

    function nextWeek() {
        currentMonday = addDays(currentMonday, 7);
    }

    // PDF download: uses print stylesheet — no backend needed
    // Only the schedule grid is printed, everything else is hidden via print:hidden
    function downloadPdf() {
        window.print();
    }

    return {
        get week()          { return week; },
        get currentMonday() { return currentMonday; },
        prevWeek,
        nextWeek,
        downloadPdf,
    };
}